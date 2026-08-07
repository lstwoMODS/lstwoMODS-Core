using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class TextRenderer : UIRenderer
{
    public TextRenderer(BaseUIElementData data) : base(data) { }

    public override void ApplyState(BaseUIElementData data) { Data = data; Name = data.Name; }

    public override void Render()
    {
        var d = (TextData)Data;
        switch (d.Variant)
        {
            case TextType.Text:            ImGui.Text(d.Text); break;
            case TextType.TextColored:     ImGui.TextColored(new Vector4(d.R, d.G, d.B, d.A), d.Text); break;
            case TextType.TextDisabled:    ImGui.TextDisabled(d.Text); break;
            case TextType.TextWrapped:     ImGui.TextWrapped(d.Text); break;
            case TextType.TextUnformatted: ImGui.TextUnformatted(d.Text); break;
            case TextType.LabelText:       ImGui.LabelText(d.Label, d.Text); break;
            case TextType.BulletText:      ImGui.BulletText(d.Text); break;
            case TextType.SeparatorText:   ImGui.SeparatorText(d.Text); break;
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
