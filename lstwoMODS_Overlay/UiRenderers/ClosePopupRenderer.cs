using System.Collections.Generic;
using System.Numerics;
using Hexa.NET.ImGui;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public class ClosePopupRenderer : UIRenderer
{
    public ClosePopupRenderer(BaseUIElementData data) : base(data) { }
    public override void ApplyState(BaseUIElementData data) { Data = data; Name = data.Name; }
    public override void Render() { ImGui.CloseCurrentPopup(); }
    public override BaseUIElementData? GetNewState() => null;
}
