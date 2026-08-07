namespace lstwoMODS.ImGui.Shared
{
    // All values verified against Hexa.NET.ImPlot 2.2.9 via reflection.

    public enum ImAxis
    {
        X1 = 0, X2 = 1, X3 = 2,
        Y1 = 3, Y2 = 4, Y3 = 5,
        Count = 6,
    }

    [System.Flags]
    public enum ImPlotFlags
    {
        None        = 0,
        NoTitle     = 1,
        NoLegend    = 2,
        NoMouseText = 4,
        NoInputs    = 8,
        NoMenus     = 16,
        NoBoxSelect = 32,
        CanvasOnly  = 55,
        NoFrame     = 64,
        Equal       = 128,
        Crosshairs  = 256,
    }

    [System.Flags]
    public enum ImPlotAxisFlags
    {
        None          = 0,
        NoLabel       = 1,
        NoGridLines   = 2,
        NoTickMarks   = 4,
        NoTickLabels  = 8,
        NoDecorations = 15,
        NoInitialFit  = 16,
        NoMenus       = 32,
        NoSideSwitch  = 64,
        NoHighlight   = 128,
        Opposite      = 256,
        AuxDefault    = 258,
        Foreground    = 512,
        Invert        = 1024,
        AutoFit       = 2048,
        RangeFit      = 4096,
        PanStretch    = 8192,
        LockMin       = 16384,
        LockMax       = 32768,
        Lock          = 49152,
    }

    [System.Flags]
    public enum ImPlotLineFlags
    {
        None     = 0,
        Segments = 1024,
        Loop     = 2048,
        SkipNaN  = 4096,
        NoClip   = 8192,
        Shaded   = 16384,
    }

    [System.Flags]
    public enum ImPlotScatterFlags
    {
        None   = 0,
        NoClip = 1024,
    }

    [System.Flags]
    public enum ImPlotBarsFlags
    {
        None       = 0,
        Horizontal = 1024,
    }

    [System.Flags]
    public enum ImPlotShadedFlags
    {
        None = 0,
    }

    [System.Flags]
    public enum ImPlotStairsFlags
    {
        None    = 0,
        PreStep = 1024,
        Shaded  = 2048,
    }

    [System.Flags]
    public enum ImPlotStemsFlags
    {
        None       = 0,
        Horizontal = 1024,
    }

    [System.Flags]
    public enum ImPlotPieChartFlags
    {
        None         = 0,
        Normalize    = 1024,
        IgnoreHidden = 2048,
        Exploding    = 4096,
    }

    [System.Flags]
    public enum ImPlotHeatmapFlags
    {
        None      = 0,
        ColMajor  = 1024,
    }

    [System.Flags]
    public enum ImPlotHistogramFlags
    {
        None       = 0,
        Horizontal = 1024,
        Cumulative = 2048,
        Density    = 4096,
        NoOutliers = 8192,
        ColMajor   = 16384,
    }

    [System.Flags]
    public enum ImPlotDragToolFlags
    {
        None       = 0,
        NoCursors  = 1,
        NoFit      = 2,
        NoInputs   = 4,
        Delayed    = 8,
    }

    [System.Flags]
    public enum ImPlotLegendFlags
    {
        None            = 0,
        NoButtons       = 1,
        NoHighlightItem = 2,
        NoHighlightAxis = 4,
        NoMenus         = 8,
        Outside         = 16,
        Horizontal      = 32,
        Sort            = 64,
    }

    public enum ImPlotLocation
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

    public enum ImPlotCond
    {
        None   = 0,
        Always = 1,
        Once   = 2,
    }

    public enum ImPlotMarker
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

    public enum ImPlotScale
    {
        Linear = 0,
        Time   = 1,
        Log10  = 2,
        SymLog = 3,
    }

    public enum ImPlotBin
    {
        Sqrt    = -1,
        Sturges = -2,
        Rice    = -3,
        Scott   = -4,
    }
}
