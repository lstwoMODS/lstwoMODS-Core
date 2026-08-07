namespace lstwoMODS.ImGui.Shared.UI
{
    public class ViewManipulatorData : BaseUIElementData
    {
        public float[] ViewMatrix { get; set; } = new float[16];
        public float   Length     { get; set; } = 1f;
        /// <summary>Screen-space position. Use -1 to auto-place in top-right of current window.</summary>
        public float   PosX       { get; set; } = -1f;
        public float   PosY       { get; set; } = -1f;
        public float   SizeX      { get; set; } = 128f;
        public float   SizeY      { get; set; } = 128f;
        public uint    BackgroundColor { get; set; } = 0;
        // Set by overlay when view changed
        public bool Changed { get; set; } = false;
    }
}
