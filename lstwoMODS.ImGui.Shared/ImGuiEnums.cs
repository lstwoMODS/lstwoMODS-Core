namespace lstwoMODS.ImGui.Shared
{
    [System.Flags]
    public enum ImGuiWindowFlags
    {
        None                    = 0,
        NoTitleBar              = 1,
        NoResize                = 2,
        NoMove                  = 4,
        NoScrollbar             = 8,
        NoScrollWithMouse       = 16,
        NoCollapse              = 32,
        NoDecoration            = 43,
        AlwaysAutoResize        = 64,
        NoBackground            = 128,
        NoSavedSettings         = 256,
        NoMouseInputs           = 512,
        MenuBar                 = 1024,
        HorizontalScrollbar     = 2048,
        NoFocusOnAppearing      = 4096,
        NoBringToFrontOnFocus   = 8192,
        AlwaysVerticalScrollbar = 16384,
        AlwaysHorizontalScrollbar = 32768,
        NoNavInputs             = 65536,
        NoNavFocus              = 131072,
        NoNav                   = 196608,
        NoInputs                = 197120,
        UnsavedDocument         = 262144,
        NoDocking               = 524288,
    }

    
    public enum ImGuiStyleVar
    {
        Alpha                       = 0,
        DisabledAlpha               = 1,
        WindowPadding               = 2,
        WindowRounding              = 3,
        WindowBorderSize            = 4,
        WindowMinSize               = 5,
        WindowTitleAlign            = 6,
        ChildRounding               = 7,
        ChildBorderSize             = 8,
        PopupRounding               = 9,
        PopupBorderSize             = 10,
        FramePadding                = 11,
        FrameRounding               = 12,
        FrameBorderSize             = 13,
        ItemSpacing                 = 14,
        ItemInnerSpacing            = 15,
        IndentSpacing               = 16,
        CellPadding                 = 17,
        ScrollbarSize               = 18,
        ScrollbarRounding           = 19,
        GrabMinSize                 = 20,
        GrabRounding                = 21,
        ImageBorderSize             = 22,
        TabRounding                 = 23,
        TabBorderSize               = 24,
        TabMinWidthBase             = 25,
        TabMinWidthShrink           = 26,
        TabBarBorderSize            = 27,
        TabBarOverlineSize          = 28,
        TableAngledHeadersAngle     = 29,
        TableAngledHeadersTextAlign = 30,
        TreeLinesSize               = 31,
        TreeLinesRounding           = 32,
        ButtonTextAlign             = 33,
        SelectableTextAlign         = 34,
        SeparatorTextBorderSize     = 35,
        SeparatorTextAlign          = 36,
        SeparatorTextPadding        = 37,
        DockingSeparatorSize        = 38,
        Count                       = 39,
    }

    [System.Flags]
    public enum ImGuiItemFlags
    {
        None              = 0,
        NoTabStop         = 1,
        NoNav             = 2,
        NoNavDefaultFocus = 4,
        ButtonRepeat      = 8,
        AutoClosePopups   = 16,
        AllowDuplicateId  = 32,
    }

    [System.Flags]
    public enum ImGuiDockNodeFlags
    {
        None                    = 0,
        KeepAliveOnly           = 1,
        NoDockingOverCentralNode = 4,
        PassthruCentralNode     = 8,
        NoDockingSplit          = 16,
        NoResize                = 32,
        AutoHideTabBar          = 64,
        NoUndocking             = 128,
    }

    public enum ImGuiCol
    {
        Text                      = 0,
        TextDisabled              = 1,
        WindowBg                  = 2,
        ChildBg                   = 3,
        PopupBg                   = 4,
        Border                    = 5,
        BorderShadow              = 6,
        FrameBg                   = 7,
        FrameBgHovered            = 8,
        FrameBgActive             = 9,
        TitleBg                   = 10,
        TitleBgActive             = 11,
        TitleBgCollapsed          = 12,
        MenuBarBg                 = 13,
        ScrollbarBg               = 14,
        ScrollbarGrab             = 15,
        ScrollbarGrabHovered      = 16,
        ScrollbarGrabActive       = 17,
        CheckMark                 = 18,
        SliderGrab                = 19,
        SliderGrabActive          = 20,
        Button                    = 21,
        ButtonHovered             = 22,
        ButtonActive              = 23,
        Header                    = 24,
        HeaderHovered             = 25,
        HeaderActive              = 26,
        Separator                 = 27,
        SeparatorHovered          = 28,
        SeparatorActive           = 29,
        ResizeGrip                = 30,
        ResizeGripHovered         = 31,
        ResizeGripActive          = 32,
        InputTextCursor           = 33,
        TabHovered                = 34,
        Tab                       = 35,
        TabSelected               = 36,
        TabSelectedOverline       = 37,
        TabDimmed                 = 38,
        TabDimmedSelected         = 39,
        TabDimmedSelectedOverline = 40,
        DockingPreview            = 41,
        DockingEmptyBg            = 42,
        PlotLines                 = 43,
        PlotLinesHovered          = 44,
        PlotHistogram             = 45,
        PlotHistogramHovered      = 46,
        TableHeaderBg             = 47,
        TableBorderStrong         = 48,
        TableBorderLight          = 49,
        TableRowBg                = 50,
        TableRowBgAlt             = 51,
        TextLink                  = 52,
        TextSelectedBg            = 53,
        TreeLines                 = 54,
        DragDropTarget            = 55,
        NavCursor                 = 56,
        NavWindowingHighlight     = 57,
        NavWindowingDimBg         = 58,
        ModalWindowDimBg          = 59,
        Count                     = 60,
    }

    [System.Flags]
    public enum ImGuiSliderFlags
    {
        None            = 0,
        Logarithmic     = 32,
        NoRoundToFormat = 64,
        NoInput         = 128,
        WrapAround      = 256,
        ClampOnInput    = 512,
        ClampZeroRange  = 1024,
        AlwaysClamp     = 1536,
        NoSpeedTweaks   = 2048,
    }

    [System.Flags]
    public enum ImGuiInputTextFlags
    {
        None                = 0,
        CharsDecimal        = 1,
        CharsHexadecimal    = 2,
        CharsScientific     = 4,
        CharsUppercase      = 8,
        CharsNoBlank        = 16,
        AllowTabInput       = 32,
        EnterReturnsTrue    = 64,
        EscapeClearsAll     = 128,
        CtrlEnterForNewLine = 256,
        ReadOnly            = 512,
        Password            = 1024,
        AlwaysOverwrite     = 2048,
        AutoSelectAll       = 4096,
        ParseEmptyRefVal    = 8192,
        DisplayEmptyRefVal  = 16384,
        NoHorizontalScroll  = 32768,
        NoUndoRedo          = 65536,
        ElideLeft           = 131072,
        CallbackCompletion  = 262144,
        CallbackHistory     = 524288,
        CallbackAlways      = 1048576,
        CallbackCharFilter  = 2097152,
        CallbackResize      = 4194304,
        CallbackEdit        = 8388608,
    }

    [System.Flags]
    public enum ImGuiComboFlags
    {
        None            = 0,
        PopupAlignLeft  = 1,
        HeightSmall     = 2,
        HeightRegular   = 4,
        HeightLarge     = 8,
        HeightLargest   = 16,
        NoArrowButton   = 32,
        NoPreview       = 64,
        WidthFitPreview = 128,
    }

    [System.Flags]
    public enum ImGuiSelectableFlags
    {
        None              = 0,
        NoAutoClosePopups = 1,
        SpanAllColumns    = 2,
        AllowDoubleClick  = 4,
        Disabled          = 8,
        AllowOverlap      = 16,
        Highlight         = 32,
    }

    [System.Flags]
    public enum ImGuiTreeNodeFlags
    {
        None                 = 0,
        Selected             = 1,
        Framed               = 2,
        AllowOverlap         = 4,
        NoTreePushOnOpen     = 8,
        NoAutoOpenOnLog      = 16,
        CollapsingHeader     = 26,
        DefaultOpen          = 32,
        OpenOnDoubleClick    = 64,
        OpenOnArrow          = 128,
        Leaf                 = 256,
        Bullet               = 512,
        FramePadding         = 1024,
        SpanAvailWidth       = 2048,
        SpanFullWidth        = 4096,
        SpanLabelWidth       = 8192,
        SpanAllColumns       = 16384,
        LabelSpanAllColumns  = 32768,
        NavLeftJumpsToParent = 131072,
        DrawLinesNone        = 262144,
        DrawLinesFull        = 524288,
        DrawLinesToNodes     = 1048576,
    }

    [System.Flags]
    public enum ImGuiTabBarFlags
    {
        None                         = 0,
        Reorderable                  = 1,
        AutoSelectNewTabs            = 2,
        ListPopupButton              = 4,
        NoCloseWithMiddleMouseButton = 8,
        NoTabListScrollingButtons    = 16,
        NoTooltip                    = 32,
        DrawSelectedOverline         = 64,
        FittingPolicyDefault         = 128,
        FittingPolicyShrink          = 256,
        FittingPolicyScroll          = 512,
    }

    [System.Flags]
    public enum ImGuiTabItemFlags
    {
        None                         = 0,
        UnsavedDocument              = 1,
        SetSelected                  = 2,
        NoCloseWithMiddleMouseButton = 4,
        NoPushId                     = 8,
        NoTooltip                    = 16,
        NoReorder                    = 32,
        Leading                      = 64,
        Trailing                     = 128,
        NoAssumedClosure             = 256,
    }

    [System.Flags]
    public enum ImGuiChildFlags
    {
        None                   = 0,
        Borders                = 1,
        AlwaysUseWindowPadding = 2,
        ResizeX                = 4,
        ResizeY                = 8,
        AutoResizeX            = 16,
        AutoResizeY            = 32,
        AlwaysAutoResize       = 64,
        FrameStyle             = 128,
        NavFlattened           = 256,
    }

    [System.Flags]
    public enum ImGuiColorEditFlags
    {
        None             = 0,
        NoAlpha          = 2,
        NoPicker         = 4,
        NoOptions        = 8,
        NoSmallPreview   = 16,
        NoInputs         = 32,
        NoTooltip        = 64,
        NoLabel          = 128,
        NoSidePreview    = 256,
        NoDragDrop       = 512,
        NoBorder         = 1024,
        AlphaOpaque      = 2048,
        AlphaNoBg        = 4096,
        AlphaPreviewHalf = 8192,
        AlphaBar         = 65536,
        Hdr              = 524288,
        DisplayRgb       = 1048576,
        DisplayHsv       = 2097152,
        DisplayHex       = 4194304,
        Uint8            = 8388608,
        Float            = 16777216,
        PickerHueBar     = 33554432,
        PickerHueWheel   = 67108864,
        InputRgb         = 134217728,
        InputHsv         = 268435456,
    }

    public enum ImGuiDir
    {
        None  = -1,
        Left  = 0,
        Right = 1,
        Up    = 2,
        Down  = 3,
    }

    [System.Flags]
    public enum ImGuiPopupFlags
    {
        None                      = 0,
        MouseButtonLeft           = 0,
        MouseButtonRight          = 1,
        MouseButtonMiddle         = 2,
        NoReopen                  = 32,
        NoOpenOverExistingPopup   = 128,
        NoOpenOverItems           = 256,
        AnyPopupId                = 1024,
        AnyPopupLevel             = 2048,
        AnyPopup                  = 3072,
    }

    public enum ImGuiHoveredFlags
    {
        None                        = 0,
        ChildWindows                = 1,
        RootWindow                  = 2,
        AnyWindow                   = 4,
        NoPopupHierarchy            = 8,
        AllowWhenBlockedByPopup     = 32,
        AllowWhenBlockedByActiveItem = 128,
        AllowWhenOverlappedByItem   = 256,
        AllowWhenOverlappedByWindow = 512,
        AllowWhenOverlapped         = 768,
        AllowWhenDisabled           = 1024,
        NoNavOverride               = 2048,
        /// <summary>Use the hover-delay from ImGui style (recommended for tooltips).</summary>
        ForTooltip                  = 4096,
        /// <summary>Only hover when mouse hasn't moved for a moment.</summary>
        Stationary                  = 8192,
        DelayNone                   = 16384,
        DelayShort                  = 32768,
        DelayNormal                 = 65536,
        NoSharedDelay               = 131072,
    }

    public enum ImGuiCond
    {
        None         = 0,
        Always       = 1,
        Once         = 2,
        FirstUseEver = 4,
        Appearing    = 8,
    }

    /// <summary>
    /// Controls whether an overlay element (or container) requires mouse/keyboard focus.
    /// Inherit resolves to the nearest ancestor's effective value (default true at root).
    /// </summary>
    public enum RequireInputMode
    {
        Inherit = 0,
        True    = 1,
        False   = 2,
    }

    [System.Flags]
    public enum ImGuiTableFlags
    {
        None                        = 0,
        Resizable                   = 1,
        Reorderable                 = 2,
        Hideable                    = 4,
        Sortable                    = 8,
        NoSavedSettings             = 16,
        ContextMenuInBody           = 32,
        RowBg                       = 64,
        BordersInnerH               = 128,
        BordersOuterH               = 256,
        BordersH                    = 384,
        BordersInnerV               = 512,
        BordersInner                = 640,
        BordersOuterV               = 1024,
        BordersOuter                = 1280,
        BordersV                    = 1536,
        Borders                     = 1920,
        NoBordersInBody             = 2048,
        NoBordersInBodyUntilResize  = 4096,
        SizingFixedFit              = 8192,
        SizingFixedSame             = 16384,
        SizingStretchProp           = 24576,
        SizingStretchSame           = 32768,
        NoHostExtendX               = 65536,
        NoHostExtendY               = 131072,
        NoKeepColumnsVisible        = 262144,
        PreciseWidths               = 524288,
        NoClip                      = 1048576,
        PadOuterX                   = 2097152,
        NoPadOuterX                 = 4194304,
        NoPadInnerX                 = 8388608,
        ScrollX                     = 16777216,
        ScrollY                     = 33554432,
        SortMulti                   = 67108864,
        SortTristate                = 134217728,
        HighlightHoveredColumn      = 268435456,
    }

    [System.Flags]
    public enum ImGuiTableColumnFlags
    {
        None                = 0,
        Disabled            = 1,
        DefaultHide         = 2,
        DefaultSort         = 4,
        WidthStretch        = 8,
        WidthFixed          = 16,
        NoResize            = 32,
        NoReorder           = 64,
        NoHide              = 128,
        NoClip              = 256,
        NoSort              = 512,
        NoSortAscending     = 1024,
        NoSortDescending    = 2048,
        NoHeaderLabel       = 4096,
        NoHeaderWidth       = 8192,
        PreferSortAscending = 16384,
        PreferSortDescending = 32768,
        IndentEnable        = 65536,
        IndentDisable       = 131072,
        AngledHeader        = 262144,
    }

    [System.Flags]
    public enum ImGuiTableRowFlags
    {
        None    = 0,
        Headers = 1,
    }

    public enum ImGuiTableBgTarget
    {
        None   = 0,
        RowBg0 = 1,
        RowBg1 = 2,
        CellBg = 3,
    }

    [System.Flags]
    public enum ImGuiInputFlags
    {
        None                  = 0,
        Repeat                = 1,
        RouteActive           = 1024,
        RouteFocused          = 2048,
        RouteGlobal           = 4096,
        RouteAlways           = 8192,
        RouteOverFocused      = 16384,
        RouteOverActive       = 32768,
        RouteUnlessBgFocused  = 65536,
        RouteFromRootWindow   = 131072,
        Tooltip               = 262144,
    }

    /// <summary>
    /// Keyboard/mouse key identifiers. Use ModCtrl/ModShift/ModAlt as bitmask modifiers
    /// for SetNextItemShortcut: e.g. (int)ImGuiKey.S | (int)ImGuiKey.ModCtrl
    /// </summary>
    public enum ImGuiKey
    {
        /// <summary>To be documented.</summary>
        None = 0,
        /// <summary>
        /// First valid key value (other than 0)<br />
        /// </summary>
        NamedKeyBegin = 512, // 0x00000200
        /// <summary>
        /// == ImGuiKey_NamedKey_BEGIN<br />
        /// </summary>
        Tab = NamedKeyBegin, // 0x00000200
        /// <summary>To be documented.</summary>
        LeftArrow = 513, // 0x00000201
        /// <summary>To be documented.</summary>
        RightArrow = 514, // 0x00000202
        /// <summary>To be documented.</summary>
        UpArrow = 515, // 0x00000203
        /// <summary>To be documented.</summary>
        DownArrow = 516, // 0x00000204
        /// <summary>To be documented.</summary>
        PageUp = 517, // 0x00000205
        /// <summary>To be documented.</summary>
        PageDown = 518, // 0x00000206
        /// <summary>To be documented.</summary>
        Home = 519, // 0x00000207
        /// <summary>To be documented.</summary>
        End = 520, // 0x00000208
        /// <summary>To be documented.</summary>
        Insert = 521, // 0x00000209
        /// <summary>To be documented.</summary>
        Delete = 522, // 0x0000020A
        /// <summary>To be documented.</summary>
        Backspace = 523, // 0x0000020B
        /// <summary>To be documented.</summary>
        Space = 524, // 0x0000020C
        /// <summary>To be documented.</summary>
        Enter = 525, // 0x0000020D
        /// <summary>To be documented.</summary>
        Escape = 526, // 0x0000020E
        /// <summary>To be documented.</summary>
        LeftCtrl = 527, // 0x0000020F
        /// <summary>To be documented.</summary>
        LeftShift = 528, // 0x00000210
        /// <summary>To be documented.</summary>
        LeftAlt = 529, // 0x00000211
        /// <summary>
        /// Also see ImGuiMod_Ctrl, ImGuiMod_Shift, ImGuiMod_Alt, ImGuiMod_Super below!<br />
        /// </summary>
        LeftSuper = 530, // 0x00000212
        /// <summary>To be documented.</summary>
        RightCtrl = 531, // 0x00000213
        /// <summary>To be documented.</summary>
        RightShift = 532, // 0x00000214
        /// <summary>To be documented.</summary>
        RightAlt = 533, // 0x00000215
        /// <summary>To be documented.</summary>
        RightSuper = 534, // 0x00000216
        /// <summary>To be documented.</summary>
        Menu = 535, // 0x00000217
        /// <summary>To be documented.</summary>
        Key0 = 536, // 0x00000218
        /// <summary>To be documented.</summary>
        Key1 = 537, // 0x00000219
        /// <summary>To be documented.</summary>
        Key2 = 538, // 0x0000021A
        /// <summary>To be documented.</summary>
        Key3 = 539, // 0x0000021B
        /// <summary>To be documented.</summary>
        Key4 = 540, // 0x0000021C
        /// <summary>To be documented.</summary>
        Key5 = 541, // 0x0000021D
        /// <summary>To be documented.</summary>
        Key6 = 542, // 0x0000021E
        /// <summary>To be documented.</summary>
        Key7 = 543, // 0x0000021F
        /// <summary>To be documented.</summary>
        Key8 = 544, // 0x00000220
        /// <summary>To be documented.</summary>
        Key9 = 545, // 0x00000221
        /// <summary>To be documented.</summary>
        A = 546, // 0x00000222
        /// <summary>To be documented.</summary>
        B = 547, // 0x00000223
        /// <summary>To be documented.</summary>
        C = 548, // 0x00000224
        /// <summary>To be documented.</summary>
        D = 549, // 0x00000225
        /// <summary>To be documented.</summary>
        E = 550, // 0x00000226
        /// <summary>To be documented.</summary>
        F = 551, // 0x00000227
        /// <summary>To be documented.</summary>
        G = 552, // 0x00000228
        /// <summary>To be documented.</summary>
        H = 553, // 0x00000229
        /// <summary>To be documented.</summary>
        I = 554, // 0x0000022A
        /// <summary>To be documented.</summary>
        J = 555, // 0x0000022B
        /// <summary>To be documented.</summary>
        K = 556, // 0x0000022C
        /// <summary>To be documented.</summary>
        L = 557, // 0x0000022D
        /// <summary>To be documented.</summary>
        M = 558, // 0x0000022E
        /// <summary>To be documented.</summary>
        N = 559, // 0x0000022F
        /// <summary>To be documented.</summary>
        O = 560, // 0x00000230
        /// <summary>To be documented.</summary>
        P = 561, // 0x00000231
        /// <summary>To be documented.</summary>
        Q = 562, // 0x00000232
        /// <summary>To be documented.</summary>
        R = 563, // 0x00000233
        /// <summary>To be documented.</summary>
        S = 564, // 0x00000234
        /// <summary>To be documented.</summary>
        T = 565, // 0x00000235
        /// <summary>To be documented.</summary>
        U = 566, // 0x00000236
        /// <summary>To be documented.</summary>
        V = 567, // 0x00000237
        /// <summary>To be documented.</summary>
        W = 568, // 0x00000238
        /// <summary>To be documented.</summary>
        X = 569, // 0x00000239
        /// <summary>To be documented.</summary>
        Y = 570, // 0x0000023A
        /// <summary>To be documented.</summary>
        Z = 571, // 0x0000023B
        /// <summary>To be documented.</summary>
        F1 = 572, // 0x0000023C
        /// <summary>To be documented.</summary>
        F2 = 573, // 0x0000023D
        /// <summary>To be documented.</summary>
        F3 = 574, // 0x0000023E
        /// <summary>To be documented.</summary>
        F4 = 575, // 0x0000023F
        /// <summary>To be documented.</summary>
        F5 = 576, // 0x00000240
        /// <summary>To be documented.</summary>
        F6 = 577, // 0x00000241
        /// <summary>To be documented.</summary>
        F7 = 578, // 0x00000242
        /// <summary>To be documented.</summary>
        F8 = 579, // 0x00000243
        /// <summary>To be documented.</summary>
        F9 = 580, // 0x00000244
        /// <summary>To be documented.</summary>
        F10 = 581, // 0x00000245
        /// <summary>To be documented.</summary>
        F11 = 582, // 0x00000246
        /// <summary>To be documented.</summary>
        F12 = 583, // 0x00000247
        /// <summary>To be documented.</summary>
        F13 = 584, // 0x00000248
        /// <summary>To be documented.</summary>
        F14 = 585, // 0x00000249
        /// <summary>To be documented.</summary>
        F15 = 586, // 0x0000024A
        /// <summary>To be documented.</summary>
        F16 = 587, // 0x0000024B
        /// <summary>To be documented.</summary>
        F17 = 588, // 0x0000024C
        /// <summary>To be documented.</summary>
        F18 = 589, // 0x0000024D
        /// <summary>To be documented.</summary>
        F19 = 590, // 0x0000024E
        /// <summary>To be documented.</summary>
        F20 = 591, // 0x0000024F
        /// <summary>To be documented.</summary>
        F21 = 592, // 0x00000250
        /// <summary>To be documented.</summary>
        F22 = 593, // 0x00000251
        /// <summary>To be documented.</summary>
        F23 = 594, // 0x00000252
        /// <summary>To be documented.</summary>
        F24 = 595, // 0x00000253
        /// <summary>
        /// '<br />
        /// </summary>
        Apostrophe = 596, // 0x00000254
        /// <summary>
        /// ,<br />
        /// </summary>
        Comma = 597, // 0x00000255
        /// <summary>
        /// -<br />
        /// </summary>
        Minus = 598, // 0x00000256
        /// <summary>
        /// .<br />
        /// </summary>
        Period = 599, // 0x00000257
        /// <summary>
        /// </summary>
        Slash = 600, // 0x00000258
        /// <summary>
        /// ;<br />
        /// </summary>
        Semicolon = 601, // 0x00000259
        /// <summary>
        /// =<br />
        /// </summary>
        Equal = 602, // 0x0000025A
        /// <summary>
        /// [<br />
        /// </summary>
        LeftBracket = 603, // 0x0000025B
        /// <summary>
        /// \ (this text inhibit multiline comment caused by backslash)<br />
        /// </summary>
        Backslash = 604, // 0x0000025C
        /// <summary>
        /// ]<br />
        /// </summary>
        RightBracket = 605, // 0x0000025D
        /// <summary>
        /// `<br />
        /// </summary>
        GraveAccent = 606, // 0x0000025E
        /// <summary>To be documented.</summary>
        CapsLock = 607, // 0x0000025F
        /// <summary>To be documented.</summary>
        ScrollLock = 608, // 0x00000260
        /// <summary>To be documented.</summary>
        NumLock = 609, // 0x00000261
        /// <summary>To be documented.</summary>
        PrintScreen = 610, // 0x00000262
        /// <summary>To be documented.</summary>
        Pause = 611, // 0x00000263
        /// <summary>To be documented.</summary>
        Keypad0 = 612, // 0x00000264
        /// <summary>To be documented.</summary>
        Keypad1 = 613, // 0x00000265
        /// <summary>To be documented.</summary>
        Keypad2 = 614, // 0x00000266
        /// <summary>To be documented.</summary>
        Keypad3 = 615, // 0x00000267
        /// <summary>To be documented.</summary>
        Keypad4 = 616, // 0x00000268
        /// <summary>To be documented.</summary>
        Keypad5 = 617, // 0x00000269
        /// <summary>To be documented.</summary>
        Keypad6 = 618, // 0x0000026A
        /// <summary>To be documented.</summary>
        Keypad7 = 619, // 0x0000026B
        /// <summary>To be documented.</summary>
        Keypad8 = 620, // 0x0000026C
        /// <summary>To be documented.</summary>
        Keypad9 = 621, // 0x0000026D
        /// <summary>To be documented.</summary>
        KeypadDecimal = 622, // 0x0000026E
        /// <summary>To be documented.</summary>
        KeypadDivide = 623, // 0x0000026F
        /// <summary>To be documented.</summary>
        KeypadMultiply = 624, // 0x00000270
        /// <summary>To be documented.</summary>
        KeypadSubtract = 625, // 0x00000271
        /// <summary>To be documented.</summary>
        KeypadAdd = 626, // 0x00000272
        /// <summary>To be documented.</summary>
        KeypadEnter = 627, // 0x00000273
        /// <summary>To be documented.</summary>
        KeypadEqual = 628, // 0x00000274
        /// <summary>
        /// Available on some keyboardmouses. Often referred as "Browser Back"<br />
        /// </summary>
        AppBack = 629, // 0x00000275
        /// <summary>To be documented.</summary>
        AppForward = 630, // 0x00000276
        /// <summary>
        /// Non-US backslash.<br />
        /// </summary>
        Oem102 = 631, // 0x00000277
        /// <summary>
        /// Menu        | +       | Options  |<br />
        /// </summary>
        GamepadStart = 632, // 0x00000278
        /// <summary>
        /// View        | -       | Share    |<br />
        /// </summary>
        GamepadBack = 633, // 0x00000279
        /// <summary>
        /// X           | Y       | Square   | Tap: Toggle Menu. Hold: Windowing mode (FocusMoveResize windows)<br />
        /// </summary>
        GamepadFaceLeft = 634, // 0x0000027A
        /// <summary>
        /// B           | A       | Circle   | Cancel  Close  Exit<br />
        /// </summary>
        GamepadFaceRight = 635, // 0x0000027B
        /// <summary>
        /// Y           | X       | Triangle | Text Input  On-screen Keyboard<br />
        /// </summary>
        GamepadFaceUp = 636, // 0x0000027C
        /// <summary>
        /// A           | B       | Cross    | Activate  Open  Toggle  Tweak<br />
        /// </summary>
        GamepadFaceDown = 637, // 0x0000027D
        /// <summary>
        /// D-pad Left  | "       | "        | Move  Tweak  Resize Window (in Windowing mode)<br />
        /// </summary>
        GamepadDpadLeft = 638, // 0x0000027E
        /// <summary>
        /// D-pad Right | "       | "        | Move  Tweak  Resize Window (in Windowing mode)<br />
        /// </summary>
        GamepadDpadRight = 639, // 0x0000027F
        /// <summary>
        /// D-pad Up    | "       | "        | Move  Tweak  Resize Window (in Windowing mode)<br />
        /// </summary>
        GamepadDpadUp = 640, // 0x00000280
        /// <summary>
        /// D-pad Down  | "       | "        | Move  Tweak  Resize Window (in Windowing mode)<br />
        /// </summary>
        GamepadDpadDown = 641, // 0x00000281
        /// <summary>
        /// L Bumper    | L       | L1       | Tweak Slower  Focus Previous (in Windowing mode)<br />
        /// </summary>
        GamepadL1 = 642, // 0x00000282
        /// <summary>
        /// R Bumper    | R       | R1       | Tweak Faster  Focus Next (in Windowing mode)<br />
        /// </summary>
        GamepadR1 = 643, // 0x00000283
        /// <summary>
        /// L Trigger   | ZL      | L2       | [Analog]<br />
        /// </summary>
        GamepadL2 = 644, // 0x00000284
        /// <summary>
        /// R Trigger   | ZR      | R2       | [Analog]<br />
        /// </summary>
        GamepadR2 = 645, // 0x00000285
        /// <summary>
        /// L Stick     | L3      | L3       |<br />
        /// </summary>
        GamepadL3 = 646, // 0x00000286
        /// <summary>
        /// R Stick     | R3      | R3       |<br />
        /// </summary>
        GamepadR3 = 647, // 0x00000287
        /// <summary>
        /// |         |          | [Analog] Move Window (in Windowing mode)<br />
        /// </summary>
        GamepadLStickLeft = 648, // 0x00000288
        /// <summary>
        /// |         |          | [Analog] Move Window (in Windowing mode)<br />
        /// </summary>
        GamepadLStickRight = 649, // 0x00000289
        /// <summary>
        /// |         |          | [Analog] Move Window (in Windowing mode)<br />
        /// </summary>
        GamepadLStickUp = 650, // 0x0000028A
        /// <summary>
        /// |         |          | [Analog] Move Window (in Windowing mode)<br />
        /// </summary>
        GamepadLStickDown = 651, // 0x0000028B
        /// <summary>
        /// |         |          | [Analog]<br />
        /// </summary>
        GamepadRStickLeft = 652, // 0x0000028C
        /// <summary>
        /// |         |          | [Analog]<br />
        /// </summary>
        GamepadRStickRight = 653, // 0x0000028D
        /// <summary>
        /// |         |          | [Analog]<br />
        /// </summary>
        GamepadRStickUp = 654, // 0x0000028E
        /// <summary>
        /// |         |          | [Analog]<br />
        /// </summary>
        GamepadRStickDown = 655, // 0x0000028F
        /// <summary>To be documented.</summary>
        MouseLeft = 656, // 0x00000290
        /// <summary>To be documented.</summary>
        MouseRight = 657, // 0x00000291
        /// <summary>To be documented.</summary>
        MouseMiddle = 658, // 0x00000292
        /// <summary>To be documented.</summary>
        MouseX1 = 659, // 0x00000293
        /// <summary>To be documented.</summary>
        MouseX2 = 660, // 0x00000294
        /// <summary>To be documented.</summary>
        MouseWheelX = 661, // 0x00000295
        /// <summary>To be documented.</summary>
        MouseWheelY = 662, // 0x00000296
        /// <summary>To be documented.</summary>
        ReservedForModCtrl = 663, // 0x00000297
        /// <summary>To be documented.</summary>
        ReservedForModShift = 664, // 0x00000298
        /// <summary>To be documented.</summary>
        ReservedForModAlt = 665, // 0x00000299
        /// <summary>To be documented.</summary>
        ReservedForModSuper = 666, // 0x0000029A
        /// <summary>To be documented.</summary>
        NamedKeyEnd = 667, // 0x0000029B
        /// <summary>To be documented.</summary>
        NamedKeyCount = 155, // 0x0000009B
        /// <summary>To be documented.</summary>
        ModNone = 0,
        /// <summary>
        /// Ctrl (non-macOS), Cmd (macOS)<br />
        /// </summary>
        ModCtrl = 4096, // 0x00001000
        /// <summary>
        /// Shift<br />
        /// </summary>
        ModShift = 8192, // 0x00002000
        /// <summary>
        /// OptionMenu<br />
        /// </summary>
        ModAlt = 16384, // 0x00004000
        /// <summary>
        /// WindowsSuper (non-macOS), Ctrl (macOS)<br />
        /// </summary>
        ModSuper = 32768, // 0x00008000
        /// <summary>
        /// 4-bits<br />
        /// </summary>
        ModMask = ModSuper | ModAlt | ModShift | ModCtrl, // 0x0000F000
    }
}
