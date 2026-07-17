namespace lstwoMODS.ImGui.Shared.Messages
{
    public class SetImGuiConfigMessage
    {
        public string       WindowId { get; set; }
        public ImGuiConfig  Config   { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static SetImGuiConfigMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<SetImGuiConfigMessage>(message);
    }
}
