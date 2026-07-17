namespace lstwoMODS.ImGui.Shared.Messages
{
    public class PreloadImageMessage
    {
        public string WindowId { get; set; }
        public string FilePath { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static PreloadImageMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<PreloadImageMessage>(msg);
    }
}
