namespace lstwoMODS.ImGui.Shared.UI
{
    public class FontDescriptor
    {
        /// <summary>Name used to reference this font in PushFontCommand.</summary>
        public string Name     { get; set; }
        /// <summary>Path relative to the overlay's working directory.</summary>
        public string FilePath { get; set; }
        public float  Size     { get; set; }
        /// <summary>Merge this font's glyphs into the previously added font (ImFontConfig.MergeMode)
        /// instead of registering a standalone font. Used for icon fonts so their glyphs render
        /// inline in any label. A merged font cannot be pushed by name.</summary>
        public bool   Merge    { get; set; }
        /// <summary>Vertical glyph offset in pixels  icon fonts often need a small nudge to
        /// baseline-align with text. Only meaningful with <see cref="Merge"/>.</summary>
        public float  GlyphOffsetY { get; set; }
    }
}
