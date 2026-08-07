using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS.ImGui.Shared.Messages
{
    public class FrameRequestMessage
    {
        public string WindowId;
        public BaseUIElementData[] OutputElements;

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static FrameRequestMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<FrameRequestMessage>(message);
    }
}
