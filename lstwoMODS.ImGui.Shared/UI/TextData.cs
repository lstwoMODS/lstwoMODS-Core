namespace lstwoMODS.ImGui.Shared.UI
{
    public enum TextType { Text, TextColored, TextDisabled, TextWrapped, TextUnformatted, LabelText, BulletText, SeparatorText }

    public class TextData : BaseUIElementData
    {
        public string   Text    { get; set; } = "";
        public string   Label   { get; set; } = ""; // used by LabelText (left column)
        public TextType Variant { get; set; } = TextType.Text;
        // RGBA used by TextColored
        public float R { get; set; } = 1f;
        public float G { get; set; } = 1f;
        public float B { get; set; } = 1f;
        public float A { get; set; } = 1f;
    }
}
