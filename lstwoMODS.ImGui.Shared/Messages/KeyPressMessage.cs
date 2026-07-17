namespace lstwoMODS.ImGui.Shared.Messages
{
    public class KeyPressMessage
    {
        public string WindowId;
        public int ImGuiKey;
        public HotkeyModifiers Modifiers;

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static KeyPressMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<KeyPressMessage>(message);
    }
}
