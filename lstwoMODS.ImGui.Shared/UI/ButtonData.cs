namespace lstwoMODS.ImGui.Shared.UI
{
    public class ButtonData : BaseUIElementData
    {
        /// <summary>True for one frame when the button is clicked. Always false when sent mod→overlay.</summary>
        public bool Pressed          { get; set; } = false;
        /// <summary>When true, the button width matches ImGui.CalcItemWidth(), aligns with input widgets in the same layout.</summary>
        public bool UseContentWidth  { get; set; } = false;
    }
}
