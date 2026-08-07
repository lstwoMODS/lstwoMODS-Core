using System.Linq;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class HStackRenderer : UIRenderer
{
    private HStackData _data;

    public HStackRenderer(BaseUIElementData data) : base(data)
    {
        _data = (HStackData)data;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (HStackData)data;
        var prev = _data.Children;
        _data = d;
        Data  = data;
        Name  = data.Name;
        if (!(d.Children?.Count > 0)) _data.Children = prev;
    }

    public override void Render()
    {
        var visible = _data.Children.Where(c => c.Enabled).ToList();
        if (visible.Count == 0) return;

        // Resolve the spacing value  -1 means use ImGui default.
        var spacing = _data.Spacing >= 0f
            ? _data.Spacing
            : ImGui.GetStyle().ItemInnerSpacing.X;

        var availableWidth = _data.WidthMode switch
        {
            HStackWidthMode.Content  => ImGui.CalcItemWidth(),
            HStackWidthMode.Explicit => _data.ExplicitWidth,
            _                        => ImGui.GetContentRegionAvail().X,
        };

        var totalSpacing = (visible.Count - 1) * spacing;
        var usableWidth  = availableWidth - totalSpacing;

        // Build per-slot widths from proportions.
        var props = _data.Proportions;

        var propSum = 0f;
        for (var i = 0; i < visible.Count; i++)
            propSum += (props != null && i < props.Count) ? props[i] : 1f;

        var widths = new float[visible.Count];
        for (var i = 0; i < visible.Count; i++)
        {
            var p   = (props != null && i < props.Count) ? props[i] : 1f;
            widths[i] = usableWidth * (p / propSum);
        }

        for (var i = 0; i < visible.Count; i++)
        {
            if (i > 0)
                ImGui.SameLine(0f, spacing);

            ImGui.BeginGroup();
            ImGui.PushItemWidth(widths[i]);
            RenderContext.PushSlotWidth(widths[i]);
            Window.RenderSingleElement(visible[i]);
            RenderContext.PopSlotWidth();
            ImGui.PopItemWidth();
            ImGui.EndGroup();
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
