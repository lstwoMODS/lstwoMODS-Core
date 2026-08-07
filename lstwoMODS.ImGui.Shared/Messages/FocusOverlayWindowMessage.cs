namespace lstwoMODS.ImGui.Shared.Messages
{
    /// <summary>
    /// Game → overlay. Asks the overlay to bring its own OS window to the foreground and
    /// grab keyboard focus. Used by auto-focusing UI (e.g. the chat input) so the user can
    /// type immediately without first clicking the overlay.
    /// </summary>
    public class FocusOverlayWindowMessage
    {
        public string WindowId { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static FocusOverlayWindowMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<FocusOverlayWindowMessage>(msg);
    }
}
