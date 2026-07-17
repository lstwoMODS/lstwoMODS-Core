namespace lstwoMODS.ImGui.Shared.Messages
{
    public class SetHotkeysMessage
    {
        /// <summary>The OSWindow this hotkey set belongs to.</summary>
        public string WindowId;
        /// <summary>ImGui key codes the overlay should watch for this window.</summary>
        public ImGuiKey[] ImGuiKeys;

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static SetHotkeysMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<SetHotkeysMessage>(message);
    }
}
