namespace lstwoMODS.ImGui.Shared.Messages
{
    public class StyleDataMessage
    {
        /// <summary>Matches the <see cref="RequestStyleDataMessage.RequestId"/> that triggered this response.</summary>
        public string  RequestId      { get; set; }
        /// <summary>RGBA values for all 60 ImGuiCol entries. Index as [col * 4 + channel].</summary>
        public float[] Colors         { get; set; }
        /// <summary>Float value or Vec2.X for each of the 39 ImGuiStyleVar entries.</summary>
        public float[] StyleVarValues  { get; set; }
        /// <summary>Vec2.Y for Vec2 style vars; zero for float vars.</summary>
        public float[] StyleVarValuesY { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static StyleDataMessage Deserialize(IpcMessage message)
            => IpcSerializer.Unwrap<StyleDataMessage>(message);
    }
}
