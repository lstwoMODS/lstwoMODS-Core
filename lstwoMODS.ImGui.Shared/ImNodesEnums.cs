namespace lstwoMODS.ImGui.Shared
{
    // All values verified against Hexa.NET.ImNodes 2.2.9 via reflection.

    public enum ImNodesPinShape
    {
        Circle         = 0,
        CircleFilled   = 1,
        Triangle       = 2,
        TriangleFilled = 3,
        Quad           = 4,
        QuadFilled     = 5,
    }

    [System.Flags]
    public enum ImNodesAttributeFlags
    {
        None                         = 0,
        EnableLinkDetachWithDragClick = 1,
        EnableLinkCreationOnSnap     = 2,
    }

    [System.Flags]
    public enum ImNodesStyleFlags
    {
        None             = 0,
        NodeOutline      = 1,
        GridLines        = 4,
        GridLinesPrimary = 8,
        GridSnapping     = 16,
    }

    public enum ImNodesMiniMapLocation
    {
        BottomLeft  = 0,
        BottomRight = 1,
        TopLeft     = 2,
        TopRight    = 3,
    }
}
