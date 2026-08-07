using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace lstwoMODS.ImGui.Shared
{
    public class IpcChannel : IDisposable
    {
        /// <summary>
        /// Hard cap on a single framed message. The length prefix arrives from an untrusted peer,
        /// so we refuse to allocate a buffer larger than this. Sized generously for the largest
        /// legitimate WindowInitMessage (full element tree + fonts).
        /// </summary>
        private const int MaxFrameBytes = 32 * 1024 * 1024;

        /// <summary>Cap on the initial auth handshake frame (a short base64 token).</summary>
        private const int MaxHandshakeBytes = 1024;

        /// <summary>Max nesting of <see cref="BatchMessage"/> to keep a crafted deep batch from overflowing the stack.</summary>
        private const int MaxBatchDepth = 8;

        public enum LogType
        {
            Debug,
            Info,
            Warning,
            Error
        }
        
        public class OutgoingItem
        {
            public IpcMessage Message;
            public TaskCompletionSource<bool> SentTcs;
        }

        public delegate Task IpcMessageHandler(IpcMessage message);
        public delegate void LogHandler(string message, LogType logType);

        public event IpcMessageHandler MessageReceived;
        public event LogHandler Log;
        public event Action Disconnected;

        public ConcurrentQueue<OutgoingItem> OutgoingMessages { get; } = new ConcurrentQueue<OutgoingItem>();

        private readonly ConcurrentDictionary<string, TaskCompletionSource<IpcMessage>> _pendingRequests = new ConcurrentDictionary<string, TaskCompletionSource<IpcMessage>>();
        private readonly SemaphoreSlim _writeSignal = new SemaphoreSlim(0);
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        private TcpListener _listener;
        private TcpClient _client;
        private NetworkStream _stream;

        public readonly bool IsServer;
        public readonly int Port;
        public readonly Action OnInitialized;

        private readonly string _authToken;

        private bool IsDisposed;

        public IpcChannel(bool isServer, int port, Action onInitialized, string authToken = null)
        {
            IsServer = isServer;
            Port = port;
            OnInitialized = onInitialized;
            _authToken = authToken;
        }

        public async Task Main()
        {
            try
            {
                Log?.Invoke("Initializing", LogType.Debug);

                if (IsServer)
                {
                    _listener = new TcpListener(IPAddress.Loopback, Port);
                    _listener.Start();
                    Log?.Invoke("TCP Server Created", LogType.Debug);

                    _client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                    Log?.Invoke("Client Connected", LogType.Debug);
                }
                else
                {
                    _client = new TcpClient();
                    Log?.Invoke("TCP Client Created", LogType.Debug);

                    await _client.ConnectAsync(IPAddress.Loopback, Port).ConfigureAwait(false);
                    Log?.Invoke("Connected to Server", LogType.Debug);
                }

                _stream = _client.GetStream();

                if (_authToken != null)
                {
                    if (IsServer)
                    {
                        // Require the client to prove it knows the token before any message is dispatched.
                        var received = await ReadHandshake(_stream, _cts.Token).ConfigureAwait(false);
                        if (!FixedTimeEquals(received, _authToken))
                        {
                            Log?.Invoke("Handshake failed: invalid auth token. Closing connection.", LogType.Error);
                            Dispose();
                            return;
                        }

                        Log?.Invoke("Handshake OK", LogType.Debug);
                    }
                    else
                    {
                        // First thing on the wire: send our token so the server will accept us.
                        await WriteHandshake(_stream, _authToken).ConfigureAwait(false);
                    }
                }

                var readTask = ReadLoop(_stream);
                var writeTask = WriteLoop(_stream);

                OnInitialized?.Invoke();
                Log?.Invoke("Initialized", LogType.Debug);

                await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log?.Invoke("ERROR IN IPC CHANNEL MAIN: " + e, LogType.Error);
                Dispose();
            }
        }

        public void SendMessage(IpcMessage msg)
        {
            OutgoingMessages.Enqueue(new OutgoingItem { Message = msg });
            _writeSignal.Release();
        }

        public async Task<IpcMessage> SendRequestAsync(IpcMessage message, TimeSpan? timeout = null)
        {
            var requestId = Guid.NewGuid().ToString();

            message.RequestId = requestId;
            message.IsResponse = false;

            var tcs = new TaskCompletionSource<IpcMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            _pendingRequests[requestId] = tcs;

            OutgoingMessages.Enqueue(new OutgoingItem { Message = message });
            _writeSignal.Release();

            if (timeout == null)
                return await tcs.Task.ConfigureAwait(false);

            var delayTask = Task.Delay(timeout.Value);
            var completed = await Task.WhenAny(tcs.Task, delayTask).ConfigureAwait(false);

            if (completed == delayTask)
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw new TimeoutException();
            }

            return await tcs.Task.ConfigureAwait(false);
        }

        public void SendResponse(IpcMessage request, string payload)
        {
            var response = new IpcMessage
            {
                Type = request.Type,
                Payload = payload,
                RequestId = request.RequestId,
                IsResponse = true
            };

            OutgoingMessages.Enqueue(new OutgoingItem { Message = response });
            _writeSignal.Release();
        }
        
        public async Task SendAndWaitAsync(IpcMessage msg, CancellationToken token = default)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            OutgoingMessages.Enqueue(new OutgoingItem
            {
                Message = msg,
                SentTcs = tcs
            });

            _writeSignal.Release();

            // Link both the caller's token and _cts (fires when Dispose() is called).
            // If the channel was already disposed, _cts.Token is already cancelled and
            // the registration fires synchronously, unblocking the await immediately.
            using (token.Register(() => tcs.TrySetCanceled()))
            using (_cts.Token.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }

        private async Task ReadLoop(NetworkStream stream)
        {
            try
            {
                while (!IsDisposed)
                {
                    var msg = await ReadMessage(stream, _cts.Token).ConfigureAwait(false);
                    await DispatchMessage(msg).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (IsDisposed || e is ObjectDisposedException || e is OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e) when (e is EndOfStreamException ||
                                      (e is IOException && e.InnerException is SocketException))
            {
                Log?.Invoke("Connection closed by remote host.", LogType.Info);
                Disconnected?.Invoke();
                Dispose();
            }
            catch (Exception e)
            {
                Log?.Invoke("ERROR IN READ LOOP: " + e, LogType.Error);
                Disconnected?.Invoke();
                Dispose();
            }
        }

        private Task DispatchMessage(IpcMessage msg) => DispatchMessage(msg, 0);

        private async Task DispatchMessage(IpcMessage msg, int depth)
        {
            if (msg.Type == nameof(BatchMessage))
            {
                if (depth >= MaxBatchDepth)
                {
                    Log?.Invoke($"Dropped batch nested deeper than {MaxBatchDepth} levels.", LogType.Warning);
                    return;
                }

                var batch = BatchMessage.Deserialize(msg);
                if (batch?.Messages != null)
                    foreach (var child in batch.Messages)
                        await DispatchMessage(child, depth + 1).ConfigureAwait(false);
                return;
            }

            if (msg.IsResponse &&
                msg.RequestId != null &&
                _pendingRequests.TryRemove(msg.RequestId, out var tcs))
            {
                tcs.TrySetResult(msg);
            }
            else if (MessageReceived != null)
            {
                try
                {
                    await MessageReceived.Invoke(msg).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log?.Invoke("Message handler exception: " + e, LogType.Error);
                }
            }
        }

        private async Task WriteLoop(NetworkStream stream)
        {
            try
            {
                while (!IsDisposed)
                {
                    await _writeSignal.WaitAsync().ConfigureAwait(false);

                    if (IsDisposed)
                        break;

                    var batch = new List<OutgoingItem>();
                    while (OutgoingMessages.TryDequeue(out var item))
                        batch.Add(item);

                    if (batch.Count == 0)
                        continue;

                    if (batch.Count == 1)
                    {
                        await WriteMessage(stream, batch[0].Message).ConfigureAwait(false);
                        batch[0].SentTcs?.TrySetResult(true);
                    }
                    else
                    {
                        var batchMsg = new BatchMessage { Messages = batch.Select(i => i.Message).ToList() };
                        await WriteMessage(stream, batchMsg.Serialize()).ConfigureAwait(false);
                        foreach (var item in batch)
                            item.SentTcs?.TrySetResult(true);
                    }
                }
            }
            catch (Exception e) when (IsDisposed || e is ObjectDisposedException || e is OperationCanceledException)
            {
                // ignored
            }
            catch (Exception e)
            {
                Log?.Invoke("ERROR IN WRITE LOOP: " + e, LogType.Error);
                Disconnected?.Invoke();
                Dispose();
            }
        }

        private static async Task WriteMessage(NetworkStream stream, IpcMessage msg)
        {
            var json = JsonConvert.SerializeObject(msg);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(length, 0, 4).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task<IpcMessage> ReadMessage(NetworkStream stream, CancellationToken token)
        {
            var lengthBuffer = new byte[4];
            await ReadExactlyAsync(stream, lengthBuffer, 4, token).ConfigureAwait(false);

            var length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length < 0 || length > MaxFrameBytes)
                throw new InvalidDataException(
                    $"Frame length {length} is out of range (0..{MaxFrameBytes}).");

            var dataBuffer = new byte[length];

            await ReadExactlyAsync(stream, dataBuffer, length, token).ConfigureAwait(false);

            var json = Encoding.UTF8.GetString(dataBuffer);
            return JsonConvert.DeserializeObject<IpcMessage>(json);
        }

        private static async Task WriteHandshake(NetworkStream stream, string token)
        {
            var data = Encoding.UTF8.GetBytes(token);
            var length = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(length, 0, 4).ConfigureAwait(false);
            await stream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task<string> ReadHandshake(NetworkStream stream, CancellationToken token)
        {
            var lengthBuffer = new byte[4];
            await ReadExactlyAsync(stream, lengthBuffer, 4, token).ConfigureAwait(false);

            var length = BitConverter.ToInt32(lengthBuffer, 0);
            if (length < 0 || length > MaxHandshakeBytes)
                throw new InvalidDataException(
                    $"Handshake length {length} is out of range (0..{MaxHandshakeBytes}).");

            var dataBuffer = new byte[length];
            await ReadExactlyAsync(stream, dataBuffer, length, token).ConfigureAwait(false);

            return Encoding.UTF8.GetString(dataBuffer);
        }

        // Length-independent-ish constant-time comparison so the handshake check doesn't leak the
        // token via timing. (net472 has no CryptographicOperations.FixedTimeEquals.)
        private static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null)
                return false;

            var ab = Encoding.UTF8.GetBytes(a);
            var bb = Encoding.UTF8.GetBytes(b);

            if (ab.Length != bb.Length)
                return false;

            var diff = 0;
            for (var i = 0; i < ab.Length; i++)
                diff |= ab[i] ^ bb[i];

            return diff == 0;
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int length, CancellationToken token)
        {
            var offset = 0;

            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, offset, length - offset, token).ConfigureAwait(false);

                if (read == 0)
                    throw new EndOfStreamException();

                offset += read;
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            _cts.Cancel();
            _writeSignal.Release();

            _stream?.Close();
            _client?.Close();
            _listener?.Stop();
        }
    }
}