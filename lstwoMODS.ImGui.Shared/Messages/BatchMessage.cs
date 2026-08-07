using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared
{
    public class BatchMessage
    {
        public List<IpcMessage> Messages { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static BatchMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<BatchMessage>(msg);
    }
}
