namespace lstwoMODS.ImGui.Shared.Messages
{
    public class RegisterFontMessage
    {
        public string WindowId { get; set; }
        public string Name     { get; set; }
        public string FilePath { get; set; }
        public float  Size     { get; set; }
        /// <summary>See <see cref="UI.FontDescriptor.Merge"/>.</summary>
        public bool   Merge    { get; set; }
        /// <summary>See <see cref="UI.FontDescriptor.GlyphOffsetY"/>.</summary>
        public float  GlyphOffsetY { get; set; }

        public IpcMessage Serialize() => IpcSerializer.Wrap(this);

        public static RegisterFontMessage Deserialize(IpcMessage msg)
            => IpcSerializer.Unwrap<RegisterFontMessage>(msg);
    }
}
