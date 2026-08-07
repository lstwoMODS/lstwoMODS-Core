namespace lstwoMODS.ImGui.Shared.UI
{
    public class RadioButtonData : BaseUIElementData
    {
        public string Label         { get; set; } = "";
        /// <summary>The currently-selected value shared across the group.</summary>
        public int    SelectedValue { get; set; } = 0;
        /// <summary>The value this particular button represents.</summary>
        public int    OptionValue   { get; set; } = 0;
    }
}
