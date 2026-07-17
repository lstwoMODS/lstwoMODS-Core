using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// ImGui Tables API  feature-rich sortable/resizable table.
/// Build with WithColumn() then WithRows().
/// </summary>
public class Table : BaseUIElement<Table>
{
    public List<BaseUIElement> Children; // TableRow elements

    private static int _rowCounter = 0;

    public Table(string name, int columns = 1, ImGuiTableFlags flags = ImGuiTableFlags.None) : base(name)
    {
        Children = new List<BaseUIElement>();
        Data = new TableData { Name = name, Columns = columns, Flags = flags };
    }

    public Table WithSize(float sizeX, float sizeY = 0f) { var d=(TableData)Data; d.SizeX=sizeX; d.SizeY=sizeY; return this; }
    public Table WithInnerWidth(float w) { ((TableData)Data).InnerWidth = w; return this; }
    public Table WithHeadersRow() { ((TableData)Data).HasHeaders = true; return this; }
    public Table WithScrollFreeze(int cols, int rows) { var d=(TableData)Data; d.FreezeCols=cols; d.FreezeRows=rows; return this; }
    public Table WithFlags(ImGuiTableFlags flags) { ((TableData)Data).Flags = flags; return this; }

    /// <summary>Add a column definition. Call once per column, in order.</summary>
    public Table WithColumn(string label, float width = 0f, ImGuiTableColumnFlags colFlags = ImGuiTableColumnFlags.None)
    {
        var d = (TableData)Data;
        d.ColumnSetups.Add(new TableColumnSetup { Label = label, Width = width, Flags = colFlags });
        if (d.ColumnSetups.Count > d.Columns) d.Columns = d.ColumnSetups.Count;
        return this;
    }

    /// <summary>Add rows. Each TableRow's children map one-to-one to columns.</summary>
    public Table WithRows(params TableRow[] rows)
    {
        foreach (var r in rows) AddRow(r);
        return this;
    }

    public Table AddRow(TableRow row)
    {
        Children.Add(row);
        ((TableData)Data).Children.Add(row.Data);
        return this;
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}

/// <summary>
/// A single table row. Each child element maps to one column cell (left to right).
/// Wrap multiple elements in a Container or Group for cells with more than one widget.
/// </summary>
public class TableRow : BaseUIElement<TableRow>
{
    public List<BaseUIElement> Children;

    public TableRow(params BaseUIElement[] cells) : base($"__row_{System.Threading.Interlocked.Increment(ref _counter)}")
    {
        Children = new List<BaseUIElement>(cells);
        Data = new TableRowData { Name = Name, Children = Children.Select(c => c.Data).ToList() };
    }

    private static int _counter = 0;

    public TableRow WithHeight(float h)           { ((TableRowData)Data).MinHeight = h;   return this; }
    public TableRow WithFlags(ImGuiTableRowFlags f){ ((TableRowData)Data).RowFlags = f;   return this; }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
