using System.Reflection;
using lstwoMODS.ImGui.Shared.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace lstwoMODS.ImGui.Shared.Messages
{
    /// <summary>
    /// One runtime-created element (with its full subtree) and where to attach it.
    /// </summary>
    public class CreatedElementEntry
    {
        /// <summary>Id of the element to attach under, or -1 for the window's top level.
        /// The parent must be a container-like element (one that has a Children list).</summary>
        public int ParentId = -1;

        /// <summary>Insert position within the parent's children. -1 or out of range = append.</summary>
        public int Index = -1;

        public BaseUIElementData Data;
    }

    public class FrameStateMessage
    {
        public string WindowId;
        public CreatedElementEntry[] CreatedElements;
        public BaseUIElementData[] UpdatedElements;
        public int[] RemovedElementIds;

        private static readonly JsonSerializerSettings _fullSettings = IpcSerializer.Settings;

        // Strips Children lists so UpdatedElements payloads don't re-transmit the entire UI tree.
        // Renderers preserve their existing _children when the incoming list is empty.
        // Shares the hardened security settings (allow-list binder, max depth) via IpcSerializer.
        private static readonly JsonSerializerSettings _shallowSettings =
            IpcSerializer.CreateSettings(new ChildFreeContractResolver());

        private class ChildFreeContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var prop = base.CreateProperty(member, memberSerialization);
                if (prop.PropertyName == "Children" || prop.PropertyName == "LineChildren")
                    prop.ShouldSerialize = _ => false;
                return prop;
            }
        }

        public IpcMessage Serialize()
        {
            // Serialize the whole message without children (UpdatedElements is shallow by design).
            // CreatedElements are patched back in with full children because they're new to the overlay.
            var obj = JObject.FromObject(this, JsonSerializer.Create(_shallowSettings));

            if (CreatedElements?.Length > 0)
                obj["CreatedElements"] = JToken.FromObject(CreatedElements, JsonSerializer.Create(_fullSettings));

            return new IpcMessage
            {
                Type    = nameof(FrameStateMessage),
                Payload = obj.ToString(Formatting.None)
            };
        }

        public static FrameStateMessage Deserialize(IpcMessage message)
        {
            return JsonConvert.DeserializeObject<FrameStateMessage>(message.Payload, _fullSettings);
        }
    }
}
