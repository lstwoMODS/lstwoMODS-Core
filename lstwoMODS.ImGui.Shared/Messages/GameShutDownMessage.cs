using System;
using Newtonsoft.Json;

namespace lstwoMODS.ImGui.Shared.Messages
{
    public class GameShutDownMessage
    {
        public IpcMessage Serialize()
        {
            return new IpcMessage
            {
                Type = nameof(GameShutDownMessage),
                Payload = ""
            };
        }
    }
}