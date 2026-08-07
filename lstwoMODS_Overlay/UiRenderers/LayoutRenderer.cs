using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class LayoutRenderer : UIRenderer
{
    public LayoutRenderer(BaseUIElementData data) : base(data) { }

    public override void ApplyState(BaseUIElementData data) { Data = data; Name = data.Name; }

    public override void Render()
    {
        switch (Data)
        {
            case SeparatorData:
                ImGui.Separator();
                break;
            case SeparatorTextData std:
                ImGui.SeparatorText(std.Label);
                break;
            case SpacingData:
                ImGui.Spacing();
                break;
            case NewLineData:
                ImGui.NewLine();
                break;
            case SameLineData sld:
                ImGui.SameLine(sld.OffsetX, sld.Spacing);
                break;
            case DummyData dd:
                ImGui.Dummy(new Vector2(dd.SizeX, dd.SizeY));
                break;
            case IndentData id:
                if (id.Unindent) ImGui.Unindent(id.Amount);
                else             ImGui.Indent(id.Amount);
                break;
            case AlignTextData:
                ImGui.AlignTextToFramePadding();
                break;
            case SetCursorPosData cp:
                if (cp.ScreenSpace)
                    ImGui.SetCursorScreenPos(new Vector2(cp.X, cp.Y));
                else
                    ImGui.SetCursorPos(new Vector2(cp.X, cp.Y));
                break;
            case SetNextItemWidthData nw:
                ImGui.SetNextItemWidth(nw.Width);
                break;
            case ColumnsData cd:
                if (cd.ColId != null) ImGui.Columns(cd.Count, cd.ColId, cd.Borders);
                else ImGui.Columns(cd.Count, cd.Borders);
                break;
            case NextColumnData:
                ImGui.NextColumn();
                break;
            case FocusNextData fd:
                ImGui.SetKeyboardFocusHere(fd.Offset);
                break;
            case FocusDefaultData:
                ImGui.SetItemDefaultFocus();
                break;
            case SetNextItemShortcutData sd:
                ImGui.SetNextItemShortcut(sd.KeyChord, (ImGuiInputFlags)(int)sd.InputFlags);
                break;
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
