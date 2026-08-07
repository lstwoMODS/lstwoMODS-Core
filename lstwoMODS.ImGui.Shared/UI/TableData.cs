using System.Collections.Generic;
namespace lstwoMODS.ImGui.Shared.UI
{
    /// <summary>Column setup passed to ImGui.TableSetupColumn.</summary>
    public class TableColumnSetup
    {
        public string Label  { get; set; } = "";
        public float  Width  { get; set; } = 0f;      // 0 = auto; positive = fixed px or stretch weight
        public ImGuiTableColumnFlags Flags { get; set; } = ImGuiTableColumnFlags.None;
    }

    public class TableData : BaseUIElementData
    {
        public int    Columns    { get; set; } = 1;
        public float  SizeX      { get; set; } = 0f;
        public float  SizeY      { get; set; } = 0f;
        public float  InnerWidth { get; set; } = 0f;
        public ImGuiTableFlags Flags { get; set; } = ImGuiTableFlags.None;
        public System.Collections.Generic.List<TableColumnSetup> ColumnSetups { get; set; } = new System.Collections.Generic.List<TableColumnSetup>();
        public bool HasHeaders  { get; set; } = false;
        public int  FreezeCols  { get; set; } = 0;
        public int  FreezeRows  { get; set; } = 0;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>(); // TableRowData
    }

    public class TableRowData : BaseUIElementData
    {
        public ImGuiTableRowFlags RowFlags  { get; set; } = ImGuiTableRowFlags.None;
        public float MinHeight              { get; set; } = 0f;
        public List<BaseUIElementData> Children { get; set; } = new List<BaseUIElementData>(); // one per column cell
    }
}
