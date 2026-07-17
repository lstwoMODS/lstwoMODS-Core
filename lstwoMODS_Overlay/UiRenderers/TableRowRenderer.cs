using System.Collections.Generic;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;
namespace lstwoMODS_Overlay.UiRenderers;
public class TableRowRenderer : UIRenderer
{
    private ImGuiTableRowFlags _rowFlags;
    private float _minHeight;
    private List<BaseUIElementData> _children;

    public TableRowRenderer(BaseUIElementData data) : base(data) { CopyFrom((TableRowData)data); }
    private void CopyFrom(TableRowData d) { _rowFlags=(ImGuiTableRowFlags)(int)d.RowFlags; _minHeight=d.MinHeight; _children=d.Children; }
    public override void ApplyState(BaseUIElementData data) { var d=(TableRowData)data; var prev=_children; Data=d; Name=d.Name; CopyFrom(d); if (!(d.Children?.Count > 0)) _children=prev; }

    public override void Render()
    {
        ImGui.TableNextRow(_rowFlags, _minHeight);
        foreach (var cell in _children)
        {
            ImGui.TableNextColumn();
            if (cell.Enabled) Window.RenderSingleElement(cell);
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
