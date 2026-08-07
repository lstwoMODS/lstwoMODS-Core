namespace lstwoMODS.ImGui.Shared.UI
{
    public class ImageData : BaseUIElementData
    {
        /// <summary>Path to the image file, relative to the overlay's working directory.</summary>
        public string FilePath { get; set; } = "";
        /// <summary>Render width in pixels. 0 = use natural image width.</summary>
        public float DisplayW  { get; set; } = 0f;
        /// <summary>Render height in pixels. 0 = use natural image height.</summary>
        public float DisplayH  { get; set; } = 0f;
        // UV rect (0–1 range)
        public float UV0X { get; set; } = 0f;
        public float UV0Y { get; set; } = 0f;
        public float UV1X { get; set; } = 1f;
        public float UV1Y { get; set; } = 1f;
        // Tint applied over the image
        public float TintR { get; set; } = 1f;
        public float TintG { get; set; } = 1f;
        public float TintB { get; set; } = 1f;
        public float TintA { get; set; } = 1f;
        // Background / border color (only used by ImageButton)
        public float BgR { get; set; } = 0f;
        public float BgG { get; set; } = 0f;
        public float BgB { get; set; } = 0f;
        public float BgA { get; set; } = 0f;
        /// <summary>When true, renders as <c>ImGui.ImageButton</c> and tracks clicks.</summary>
        public bool IsButton { get; set; } = false;
        public bool Pressed  { get; set; } = false;
    }
}
