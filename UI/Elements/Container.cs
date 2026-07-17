using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

/// <summary>
/// A transparent container that renders its children with no ImGui wrapper.
/// Unlike <see cref="Group"/>, it does NOT call BeginGroup/EndGroup, so children
/// retain their independent layout positions (including floating <see cref="GuiWindow"/>s).
///
/// Setting <c>Enabled = false</c> (via <see cref="BaseUIElement.SetVisible"/>) hides
/// all children at once.
/// </summary>
public class Container : BaseUIElement<Container>
{
    public List<BaseUIElement> Children { get; }

    public Container(string name, params BaseUIElement[] children) : base(name)
    {
        Children = new List<BaseUIElement>(children);
        Data = new ContainerData
        {
            Name     = name,
            Children = Children.Select(c => c.Data).ToList()
        };
    }

    public override IEnumerable<BaseUIElement> GetChildren() => Children;
}
