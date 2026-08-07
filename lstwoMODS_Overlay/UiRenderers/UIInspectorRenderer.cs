using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class UIInspectorRenderer : UIRenderer
{
    private int _selectedId = -1;
    private BaseUIElementData? _selectedElement;
    private float _leftWidth = -1f;

    private const float SplitterHitWidth  = 9f;
    private const float SplitterLineWidth = 1f;
    private const float SplitterPadX      = 4f;  // horizontal space either side of the line
    private const float SplitterPadY      = 4f;  // vertical inset at top/bottom
    private const float MinPanelWidth     = 80f;

    public UIInspectorRenderer(BaseUIElementData data) : base(data) { }

    public override void ApplyState(BaseUIElementData data)
    {
        Data = data;
        Name = data.Name;
    }

    public override void Render()
    {
        List<BaseUIElementData> topLevel;
        try { topLevel = new List<BaseUIElementData>(Window.Elements); }
        catch { return; }

        var avail = ImGui.GetContentRegionAvail();

        if (_leftWidth < 0f)
            _leftWidth = MathF.Round(avail.X * 0.35f);

        _leftWidth = Math.Max(MinPanelWidth, Math.Min(_leftWidth, avail.X - SplitterHitWidth - MinPanelWidth));
        var rightWidth = avail.X - _leftWidth - SplitterHitWidth;

        
        if (ImGui.BeginChild("##uiinspector_tree", new Vector2(_leftWidth, avail.Y), ImGuiChildFlags.None, ImGuiWindowFlags.None))
        {
            ImGui.SeparatorText("Element Tree");
            foreach (var el in topLevel)
                RenderTreeNode(el, 0);
        }
        ImGui.EndChild();

        ImGui.SameLine(0f, 0f);

        
        ImGui.InvisibleButton("##uiinspector_splitter", new Vector2(SplitterHitWidth, avail.Y));

        if (ImGui.IsItemActive())
        {
            _leftWidth += ImGui.GetIO().MouseDelta.X;
            _leftWidth = Math.Max(MinPanelWidth, Math.Min(_leftWidth, avail.X - SplitterHitWidth - MinPanelWidth));
            rightWidth  = avail.X - _leftWidth - SplitterHitWidth;
        }

        if (ImGui.IsItemHovered() || ImGui.IsItemActive())
            ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);

        uint splitterCol;
        if (ImGui.IsItemActive())       splitterCol = ImGui.GetColorU32(ImGuiCol.SeparatorActive);
        else if (ImGui.IsItemHovered()) splitterCol = ImGui.GetColorU32(ImGuiCol.SeparatorHovered);
        else                            splitterCol = ImGui.GetColorU32(ImGuiCol.Separator);

        var rMin = ImGui.GetItemRectMin();
        var rMax = ImGui.GetItemRectMax();
        var lineX = MathF.Round(rMin.X + (SplitterHitWidth - SplitterLineWidth) * 0.5f);
        ImGui.GetWindowDrawList().AddRectFilled(
            new Vector2(lineX,                  rMin.Y + SplitterPadY),
            new Vector2(lineX + SplitterLineWidth, rMax.Y - SplitterPadY),
            splitterCol);

        ImGui.SameLine(0f, 0f);

        
        if (ImGui.BeginChild("##uiinspector_detail", new Vector2(rightWidth, avail.Y), ImGuiChildFlags.None, ImGuiWindowFlags.None))
        {
            ImGui.SeparatorText("Inspector");
            if (_selectedElement != null && _selectedId >= 0)
            {
                var liveData = Window.Renderers.TryGetValue(_selectedId, out var r) ? r.Data : _selectedElement;
                RenderInspector(liveData);
            }
            else
            {
                ImGui.TextDisabled("Select an element in the tree");
            }
        }
        ImGui.EndChild();
    }

    private void RenderTreeNode(BaseUIElementData el, int depth)
    {
        var liveData = Window.Renderers.TryGetValue(el.Id, out _) ? Window.Renderers[el.Id].Data : el;
        var children = GetElementChildren(liveData);

        var typeName = el.GetType().Name;
        if (typeName.EndsWith("Data")) typeName = typeName[..^4];

        var label = string.IsNullOrEmpty(liveData.Name)
            ? $"[{typeName}] #{el.Id}"
            : $"{liveData.Name} [{typeName}]";

        var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth | ImGuiTreeNodeFlags.OpenOnDoubleClick;
        if (children.Count == 0) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        if (_selectedId == el.Id) flags |= ImGuiTreeNodeFlags.Selected;
        if (depth == 0) flags |= ImGuiTreeNodeFlags.DefaultOpen;

        if (!liveData.Enabled)
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.5f, 0.5f, 0.5f, 1f));

        ImGui.PushID(el.Id);
        var open = ImGui.TreeNodeEx(label, flags);
        ImGui.PopID();

        if (!liveData.Enabled)
            ImGui.PopStyleColor();

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
        {
            _selectedId = el.Id;
            _selectedElement = el;
        }

        if (open && children.Count > 0)
        {
            foreach (var child in children)
                RenderTreeNode(child, depth + 1);
            ImGui.TreePop();
        }
    }

    private void RenderInspector(BaseUIElementData el)
    {
        var type = el.GetType();
        var typeName = type.Name.EndsWith("Data") ? type.Name[..^4] : type.Name;

        ImGui.Text(typeName);
        ImGui.SameLine();
        ImGui.TextDisabled($"id={el.Id}");
        ImGui.Separator();

        var tableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY;
        if (!ImGui.BeginTable("##uiinspector_props", 2, tableFlags))
            return;

        ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 160f);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();

        var types = new List<Type>();
        for (var t = type; t != null && t != typeof(object); t = t.BaseType)
            types.Insert(0, t);

        foreach (var t in types)
        {
            foreach (var prop in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!prop.CanRead) continue;

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextUnformatted(prop.Name);
                ImGui.TableSetColumnIndex(1);

                object? val;
                try { val = prop.GetValue(el); }
                catch { ImGui.TextDisabled("<error>"); continue; }

                var valStr = val switch
                {
                    null       => "<null>",
                    IList list => $"[{list.Count} items]",
                    _          => val.ToString() ?? ""
                };

                var isDefault = IsDefaultValue(val);
                if (isDefault) ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1f));
                ImGui.TextUnformatted(valStr);
                if (isDefault) ImGui.PopStyleColor();
            }
        }

        ImGui.EndTable();
    }

    private static bool IsDefaultValue(object? val) => val switch
    {
        null   => false,
        bool b => !b,
        int i  => i == 0,
        float f => f == 0f,
        string s => string.IsNullOrEmpty(s),
        _      => false
    };

    private static List<BaseUIElementData> GetElementChildren(BaseUIElementData data)
    {
        try
        {
            var prop = data.GetType().GetProperty("Children");
            if (prop?.GetValue(data) is List<BaseUIElementData> children)
                return children;
        }
        catch { }
        return [];
    }

    public override BaseUIElementData? GetNewState() => null;
}
