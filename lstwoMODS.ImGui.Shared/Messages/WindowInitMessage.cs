using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared.UI;
using Newtonsoft.Json;

namespace lstwoMODS.ImGui.Shared.Messages
{
    public class WindowInitMessage
    {
        public string Title;
        public int Width;
        public int Height;
        public WindowType WindowType;
        public BaseUiElement[] Elements;
        public long FollowWindowHandle;

        public IpcMessage Serialize()
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            };

            var serialized = JsonConvert.SerializeObject(this, settings);
            Console.WriteLine(serialized);
            
            return new IpcMessage
            {
                Type = nameof(WindowInitMessage),
                Payload = serialized
            };
        }

        public static WindowInitMessage Deserialize(IpcMessage message)
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            };
            
            var deserialized = JsonConvert.DeserializeObject<WindowInitMessage>(message.Payload, settings);
            Console.WriteLine(deserialized);
            
            return deserialized;
        }
    }
}