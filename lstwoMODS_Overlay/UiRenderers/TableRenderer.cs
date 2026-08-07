using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;
namespace lstwoMODS_Overlay.UiRenderers;
public class TableRenderer : UIRenderer
{
    private int    _columns;
    private float  _sizeX, _sizeY, _innerWidth;
    private ImGuiTableFlags _flags;
    private List<TableColumnSetup> _columnSetups;
    private bool _hasHeaders;
    private int  _freezeCols, _freezeRows;
    private List<BaseUIElementData> _children;

    public TableRenderer(BaseUIElementData data) : base(data) { CopyFrom((TableData)data); }
    private void CopyFrom(TableData d)
    {
        _columns = d.Columns; _sizeX = d.SizeX; _sizeY = d.SizeY; _innerWidth = d.InnerWidth;
        _flags = (ImGuiTableFlags)(int)d.Flags; _columnSetups = d.ColumnSetups;
        _hasHeaders = d.HasHeaders; _freezeCols = d.FreezeCols; _freezeRows = d.FreezeRows;
        _children = d.Children;
    }
    public override void ApplyState(BaseUIElementData data) { var d=(TableData)data; var prev=_children; Data=d; Name=d.Name; CopyFrom(d); if (!(d.Children?.Count > 0)) _children=prev; }

    public override void Render()
    {
        if (!ImGui.BeginTable(Data.Name, _columns, _flags, new Vector2(_sizeX, _sizeY), _innerWidth))
            return;

        foreach (var col in _columnSetups)
            ImGui.TableSetupColumn(col.Label, (ImGuiTableColumnFlags)(int)col.Flags, col.Width);
        if (_freezeCols > 0 || _freezeRows > 0)
            ImGui.TableSetupScrollFreeze(_freezeCols, _freezeRows);
        if (_hasHeaders)
            ImGui.TableHeadersRow();

        foreach (var child in _children)
            Window.RenderSingleElement(child);

        ImGui.EndTable();
    }

    public override BaseUIElementData? GetNewState() => null;
}
