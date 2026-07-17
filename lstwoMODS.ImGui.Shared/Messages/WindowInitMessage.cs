using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS.ImGui.Shared.Messages
{
    public class WindowInitMessage
    {
        public string            WindowId            { get; set; }
        public string            Title               { get; set; }
        public int               Width               { get; set; }
        public int               Height              { get; set; }
        public WindowType        WindowType          { get; set; }
        public BaseUIElementData[] Elements          { get; set; }
        public long              FollowWindowHandle  { get; set; }
        public ImGuiConfig       Config              { get; set; } = new ImGuiConfig();
        public FontDescriptor[]  Fonts               { get; set; } = new FontDescriptor[0];
        /// <summary>Render backend override. Null means the overlay uses its own overlay.config value.</summary>
        public string            Backend             { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static WindowInitMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<WindowInitMessage>(message);
    }
}