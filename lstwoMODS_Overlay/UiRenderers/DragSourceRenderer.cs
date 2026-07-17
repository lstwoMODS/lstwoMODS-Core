using System.Collections.Generic;
using System.Text;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class DragSourceRenderer : UIRenderer
{
    private string _payloadType;
    private string _payloadData;
    private string _displayLabel;
    private List<BaseUIElementData> _children;

    public DragSourceRenderer(BaseUIElementData data) : base(data)
    {
        var d = (DragSourceData)data;
        _payloadType  = d.PayloadType;
        _payloadData  = d.PayloadData;
        _displayLabel = d.DisplayLabel;
        _children     = d.Children;
    }

    public override void ApplyState(BaseUIElementData data)
    {
        var d = (DragSourceData)data;
        Data          = d;
        Name          = d.Name;
        _payloadType  = d.PayloadType;
        _payloadData  = d.PayloadData;
        _displayLabel = d.DisplayLabel;
        if (d.Children?.Count > 0) _children = d.Children;
    }

    public override unsafe void Render()
    {
        ImGui.BeginGroup();
        foreach (var child in _children)
            Window.RenderSingleElement(child);
        ImGui.EndGroup();

        if (ImGui.BeginDragDropSource(ImGuiDragDropFlags.SourceAllowNullId))
        {
            var bytes = Encoding.UTF8.GetBytes(_payloadData ?? "");
            fixed (byte* ptr = bytes)
                ImGui.SetDragDropPayload(_payloadType, ptr, (uint)bytes.Length);

            ImGui.Text(_displayLabel ?? _payloadType);
            ImGui.EndDragDropSource();
        }
    }

    public override BaseUIElementData? GetNewState() => null;
}
