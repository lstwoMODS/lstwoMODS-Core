namespace lstwoMODS.ImGui.Shared.Messages
{
    public class FocusGameWindowMessage
    {
        public string WindowId { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static FocusGameWindowMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<FocusGameWindowMessage>(msg);
    }
}
