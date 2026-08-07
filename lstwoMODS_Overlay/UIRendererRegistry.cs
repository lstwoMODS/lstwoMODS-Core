using lstwoMODS_Overlay.UiRenderers;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay;

public static class UIRendererRegistry
{
    public static Dictionary<Type, Type> DataToRenderer = new() {
        // ── Core ────────────────────────────────────────────────────────────
        [typeof(UIInspectorData)]   = typeof(UIInspectorRenderer),
        [typeof(DemoWindowData)]    = typeof(DemoWindowRenderer),
        [typeof(WindowData)]        = typeof(WindowRenderer),
        [typeof(DockSpaceData)]     = typeof(DockSpaceRenderer),
        [typeof(GroupData)]         = typeof(GroupRenderer),
        [typeof(ContainerData)]     = typeof(ContainerRenderer),
        [typeof(FlowGridData)]      = typeof(FlowGridRenderer),
        [typeof(HStackData)]        = typeof(HStackRenderer),
        [typeof(PinRowData)]        = typeof(PinRowRenderer),

        // ── Buttons ──────────────────────────────────────────────────────────
        [typeof(ButtonData)]        = typeof(ButtonRenderer),
        [typeof(KeyCaptureData)]    = typeof(KeyCaptureRenderer),
        [typeof(SmallButtonData)]   = typeof(SmallButtonRenderer),
        [typeof(ArrowButtonData)]   = typeof(ArrowButtonRenderer),
        [typeof(InvisibleButtonData)] = typeof(InvisibleButtonRenderer),

        // ── Text / display ───────────────────────────────────────────────────
        [typeof(TextData)]          = typeof(TextRenderer),
        [typeof(ProgressBarData)]   = typeof(ProgressBarRenderer),

        // ── Layout ───────────────────────────────────────────────────────────
        [typeof(SeparatorData)]     = typeof(LayoutRenderer),
        [typeof(SeparatorTextData)] = typeof(LayoutRenderer),
        [typeof(SpacingData)]       = typeof(LayoutRenderer),
        [typeof(NewLineData)]       = typeof(LayoutRenderer),
        [typeof(SameLineData)]      = typeof(LayoutRenderer),
        [typeof(DummyData)]              = typeof(LayoutRenderer),
        [typeof(IndentData)]             = typeof(LayoutRenderer),
        [typeof(AlignTextData)]          = typeof(LayoutRenderer),
        [typeof(SetCursorPosData)]       = typeof(LayoutRenderer),
        [typeof(SetNextItemWidthData)]   = typeof(LayoutRenderer),

        // ── Tables ────────────────────────────────────────────────────────────
        [typeof(TableData)]          = typeof(TableRenderer),
        [typeof(TableRowData)]       = typeof(TableRowRenderer),

        // ── Inputs & Focus (layout elements) ─────────────────────────────────
        [typeof(ColumnsData)]            = typeof(LayoutRenderer),
        [typeof(NextColumnData)]         = typeof(LayoutRenderer),
        [typeof(FocusNextData)]          = typeof(LayoutRenderer),
        [typeof(FocusDefaultData)]       = typeof(LayoutRenderer),
        [typeof(SetNextItemShortcutData)] = typeof(LayoutRenderer),

        // ── Popups & modals ───────────────────────────────────────────────────
        [typeof(PopupData)]          = typeof(PopupRenderer),
        [typeof(ModalData)]          = typeof(ModalRenderer),
        [typeof(ContextMenuData)]    = typeof(ContextMenuRenderer),
        [typeof(MenuItemData)]       = typeof(MenuItemRenderer),
        [typeof(MenuData)]           = typeof(MenuRenderer),
        [typeof(MenuBarData)]        = typeof(MenuBarRenderer),
        [typeof(MainMenuBarData)]    = typeof(MainMenuBarRenderer),
        [typeof(ClosePopupData)]     = typeof(ClosePopupRenderer),

        // ── Value widgets ────────────────────────────────────────────────────
        [typeof(CheckboxData)]          = typeof(CheckboxRenderer),
        [typeof(RadioButtonData)]       = typeof(RadioButtonRenderer),
        [typeof(ComboData)]             = typeof(ComboRenderer),
        [typeof(SearchableComboData)]   = typeof(SearchableComboRenderer),
        [typeof(SelectableData)]        = typeof(SelectableRenderer),

        // ── Drag ─────────────────────────────────────────────────────────────
        [typeof(DragFloatData)]     = typeof(DragFloatRenderer),
        [typeof(DragFloat2Data)]    = typeof(DragFloat2Renderer),
        [typeof(DragFloat3Data)]    = typeof(DragFloat3Renderer),
        [typeof(DragFloat4Data)]    = typeof(DragFloat4Renderer),
        [typeof(DragIntData)]       = typeof(DragIntRenderer),
        [typeof(DragInt2Data)]      = typeof(DragInt2Renderer),
        [typeof(DragInt3Data)]      = typeof(DragInt3Renderer),
        [typeof(DragInt4Data)]      = typeof(DragInt4Renderer),

        // ── Slider ───────────────────────────────────────────────────────────
        [typeof(SliderFloatData)]   = typeof(SliderFloatRenderer),
        [typeof(SliderFloat2Data)]  = typeof(SliderFloat2Renderer),
        [typeof(SliderFloat3Data)]  = typeof(SliderFloat3Renderer),
        [typeof(SliderFloat4Data)]  = typeof(SliderFloat4Renderer),
        [typeof(SliderIntData)]     = typeof(SliderIntRenderer),
        [typeof(SliderInt2Data)]    = typeof(SliderInt2Renderer),
        [typeof(SliderInt3Data)]    = typeof(SliderInt3Renderer),
        [typeof(SliderInt4Data)]    = typeof(SliderInt4Renderer),
        [typeof(SliderAngleData)]   = typeof(SliderAngleRenderer),

        // ── Input ────────────────────────────────────────────────────────────
        [typeof(InputTextData)]     = typeof(InputTextRenderer),
        [typeof(InputFloatData)]    = typeof(InputFloatRenderer),
        [typeof(InputFloat2Data)]   = typeof(InputFloat2Renderer),
        [typeof(InputFloat3Data)]   = typeof(InputFloat3Renderer),
        [typeof(InputFloat4Data)]   = typeof(InputFloat4Renderer),
        [typeof(InputIntData)]      = typeof(InputIntRenderer),
        [typeof(InputInt2Data)]     = typeof(InputInt2Renderer),
        [typeof(InputInt3Data)]     = typeof(InputInt3Renderer),
        [typeof(InputInt4Data)]     = typeof(InputInt4Renderer),

        // ── Color ────────────────────────────────────────────────────────────
        [typeof(ColorEdit3Data)]    = typeof(ColorEdit3Renderer),
        [typeof(ColorEdit4Data)]    = typeof(ColorEdit4Renderer),

        // ── Containers ───────────────────────────────────────────────────────
        [typeof(CollapsingHeaderData)] = typeof(CollapsingHeaderRenderer),
        [typeof(TreeNodeData)]         = typeof(TreeNodeRenderer),
        [typeof(TabBarData)]           = typeof(TabBarRenderer),
        [typeof(TabItemData)]          = typeof(TabItemRenderer),
        [typeof(ChildWindowData)]      = typeof(ChildWindowRenderer),

        // ── Plotting (ImGui built-in) ────────────────────────────────────────
        [typeof(PlotLinesData)]     = typeof(PlotLinesRenderer),
        [typeof(PlotHistogramData)] = typeof(PlotHistogramRenderer),

        // ── Drag/drop ────────────────────────────────────────────────────────
        [typeof(DragSourceData)]    = typeof(DragSourceRenderer),
        [typeof(DragTargetData)]    = typeof(DragTargetRenderer),

        // ── Image ────────────────────────────────────────────────────────────
        [typeof(ImageData)]         = typeof(ImageRenderer),

        // ── ImPlot ───────────────────────────────────────────────────────────
        [typeof(PlotPanelData)]         = typeof(PlotPanelRenderer),
        [typeof(PlotLineSeriesData)]    = typeof(PlotLineSeriesRenderer),
        [typeof(PlotScatterSeriesData)] = typeof(PlotScatterSeriesRenderer),
        [typeof(PlotBarsSeriesData)]    = typeof(PlotBarsSeriesRenderer),
        [typeof(PlotShadedSeriesData)]  = typeof(PlotShadedSeriesRenderer),
        [typeof(PlotStairsSeriesData)]  = typeof(PlotStairsSeriesRenderer),
        [typeof(PlotStemsSeriesData)]   = typeof(PlotStemsSeriesRenderer),
        [typeof(PlotPieChartData)]      = typeof(PlotPieChartRenderer),
        [typeof(PlotHeatmapData)]       = typeof(PlotHeatmapRenderer),
        [typeof(ImPlotHistogramData)]   = typeof(ImPlotHistogramRenderer),
        [typeof(PlotAnnotationData)]    = typeof(PlotAnnotationRenderer),
        [typeof(PlotDragLineData)]      = typeof(PlotDragLineRenderer),

        // ── ImGuizmo ─────────────────────────────────────────────────────────
        [typeof(GizmoData)]           = typeof(GizmoRenderer),
        [typeof(ViewManipulatorData)] = typeof(ViewManipulatorRenderer),

        // ── ImNodes ──────────────────────────────────────────────────────────
        [typeof(NodeEditorData)]      = typeof(NodeEditorRenderer),
        [typeof(GraphNodeData)]       = typeof(GraphNodeRenderer),
        [typeof(InputAttributeData)]  = typeof(InputAttributeRenderer),
        [typeof(OutputAttributeData)] = typeof(OutputAttributeRenderer),
        [typeof(StaticAttributeData)] = typeof(StaticAttributeRenderer),

        // ── ImPlot3D ─────────────────────────────────────────────────────────
        [typeof(Plot3DPanelData)]         = typeof(Plot3DPanelRenderer),
        [typeof(Plot3DLineSeriesData)]    = typeof(Plot3DLineSeriesRenderer),
        [typeof(Plot3DScatterSeriesData)] = typeof(Plot3DScatterSeriesRenderer),
        [typeof(Plot3DSurfaceData)]       = typeof(Plot3DSurfaceRenderer),
    };
}
