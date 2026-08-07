namespace lstwoMODS.ImGui.Shared.UI
{
    public class Plot3DSurfaceData : BaseUIElementData
    {
        /// <summary>Flattened row-major grid: values[row * cols + col]</summary>
        public float[] XValues { get; set; } = System.Array.Empty<float>();
        public float[] YValues { get; set; } = System.Array.Empty<float>();
        public float[] ZValues { get; set; } = System.Array.Empty<float>();
        public int     Rows    { get; set; } = 1;
        public int     Cols    { get; set; } = 1;
        public ImPlot3DSurfaceFlags Flags { get; set; } = ImPlot3DSurfaceFlags.None;
    }
}
