namespace lstwoMODS.ImGui.Shared.Messages
{
    public class RequestStyleDataMessage
    {
        public string WindowId   { get; set; }
        /// <summary>0 = Dark, 1 = Light, 2 = Classic.</summary>
        public int    ThemeIndex { get; set; }
        /// <summary>Caller-generated GUID matched in the <see cref="StyleDataMessage"/> response.</summary>
        public string RequestId  { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static RequestStyleDataMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<RequestStyleDataMessage>(message);
    }
}
