namespace lstwoMODS.ImGui.Shared
{
    // All values verified against Hexa.NET.ImPlot3D 2.2.9 via reflection.

    public enum ImAxis3D
    {
        X     = 0,
        Y     = 1,
        Z     = 2,
        Count = 3,
    }

    [System.Flags]
    public enum ImPlot3DFlags
    {
        None        = 0,
        NoTitle     = 1,
        NoLegend    = 2,
        NoMouseText = 4,
        CanvasOnly  = 7,
        NoClip      = 8,
        NoMenus     = 16,
    }

    [System.Flags]
    public enum ImPlot3DAxisFlags
    {
        None          = 0,
        NoLabel       = 1,
        NoGridLines   = 2,
        NoTickMarks   = 4,
        NoTickLabels  = 8,
        NoDecorations = 11,
        LockMin       = 16,
        LockMax       = 32,
        Lock          = 48,
        AutoFit       = 64,
        Invert        = 128,
        PanStretch    = 256,
    }

    [System.Flags]
    public enum ImPlot3DLineFlags
    {
        None     = 0,
        NoLegend = 1,
        NoFit    = 2,
        Segments = 1024,
        Loop     = 2048,
        SkipNaN  = 4096,
    }

    [System.Flags]
    public enum ImPlot3DScatterFlags
    {
        None     = 0,
        NoLegend = 1,
        NoFit    = 2,
    }

    [System.Flags]
    public enum ImPlot3DSurfaceFlags
    {
        None      = 0,
        NoLegend  = 1,
        NoFit     = 2,
        NoLines   = 1024,
        NoFill    = 2048,
        NoMarkers = 4096,
    }

    [System.Flags]
    public enum ImPlot3DMeshFlags
    {
        None      = 0,
        NoLegend  = 1,
        NoFit     = 2,
        NoLines   = 1024,
        NoFill    = 2048,
        NoMarkers = 4096,
    }

    [System.Flags]
    public enum ImPlot3DLegendFlags
    {
        None            = 0,
        NoButtons       = 1,
        NoHighlightItem = 2,
        Horizontal      = 4,
    }

    public enum ImPlot3DCond
    {
        None   = 0,
        Always = 1,
        Once   = 2,
    }

    public enum ImPlot3DMarker
    {
        None     = -1,
        Circle   = 0,
        Square   = 1,
        Diamond  = 2,
        Up       = 3,
        Down     = 4,
        Left     = 5,
        Right    = 6,
        Cross    = 7,
        Plus     = 8,
        Asterisk = 9,
        Count    = 10,
    }

    public enum ImPlot3DLocation
    {
        Center    = 0,
        North     = 1,
        South     = 2,
        West      = 4,
        NorthWest = 5,
        SouthWest = 6,
        East      = 8,
        NorthEast = 9,
        SouthEast = 10,
    }
}
