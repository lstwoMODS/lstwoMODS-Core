using System;
using System.Collections.Concurrent;
using System.IO;
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

        private bool IsDisposed;

        public IpcChannel(bool isServer, int port, Action onInitialized)
        {
            IsServer = isServer;
            Port = port;
            OnInitialized = onInitialized;
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

                    _client = await _listener.AcceptTcpClientAsync();
                    Log?.Invoke("Client Connected", LogType.Debug);
                }
                else
                {
                    _client = new TcpClient();
                    Log?.Invoke("TCP Client Created", LogType.Debug);

                    await _client.ConnectAsync(IPAddress.Loopback, Port);
                    Log?.Invoke("Connected to Server", LogType.Debug);
                }

                _stream = _client.GetStream();

                var readTask = ReadLoop(_stream);
                var writeTask = WriteLoop(_stream);

                OnInitialized?.Invoke();
                Log?.Invoke("Initialized", LogType.Debug);

                await Task.WhenAll(readTask, writeTask);
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
                return await tcs.Task;

            var delayTask = Task.Delay(timeout.Value);
            var completed = await Task.WhenAny(tcs.Task, delayTask);

            if (completed == delayTask)
            {
                _pendingRequests.TryRemove(requestId, out _);
                throw new TimeoutException();
            }

            return await tcs.Task;
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

            using (token.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }
        }

        private async Task ReadLoop(NetworkStream stream)
        {
            try
            {
                while (!IsDisposed)
                {
                    var msg = await ReadMessage(stream, _cts.Token);

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
                            await MessageReceived.Invoke(msg);
                        }
                        catch (Exception e)
                        {
                            Log?.Invoke("Message handler exception: " + e, LogType.Error);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log?.Invoke("ERROR IN READ LOOP: " + e, LogType.Error);
                Dispose();
            }
        }

        private async Task WriteLoop(NetworkStream stream)
        {
            try
            {
                while (!IsDisposed)
                {
                    await _writeSignal.WaitAsync();

                    if (IsDisposed)
                        break;

                    while (OutgoingMessages.TryDequeue(out var item))
                    {
                        await WriteMessage(stream, item.Message);
                        item.SentTcs?.TrySetResult(true);
                    }
                }
            }
            catch (Exception e)
            {
                Log?.Invoke("ERROR IN WRITE LOOP: " + e, LogType.Error);
                Dispose();
            }
        }

        private static async Task WriteMessage(NetworkStream stream, IpcMessage msg)
        {
            var json = JsonConvert.SerializeObject(msg);
            var data = Encoding.UTF8.GetBytes(json);
            var length = BitConverter.GetBytes(data.Length);

            await stream.WriteAsync(length, 0, 4);
            await stream.WriteAsync(data, 0, data.Length);
            await stream.FlushAsync();
        }

        private static async Task<IpcMessage> ReadMessage(NetworkStream stream, CancellationToken token)
        {
            var lengthBuffer = new byte[4];
            await ReadExactlyAsync(stream, lengthBuffer, 4, token);

            var length = BitConverter.ToInt32(lengthBuffer, 0);
            var dataBuffer = new byte[length];

            await ReadExactlyAsync(stream, dataBuffer, length, token);

            var json = Encoding.UTF8.GetString(dataBuffer);
            return JsonConvert.DeserializeObject<IpcMessage>(json);
        }

        private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, int length, CancellationToken token)
        {
            var offset = 0;

            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer, offset, length - offset, token);

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