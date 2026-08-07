using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Overlay.UiRenderers;

public abstract class UIRenderer
{
    public BaseUIElementData Data { get; set; }
    public string Name { get; set; }

    public RemoteImGuiWindow Window;

    /// <summary>
    /// Whether this element is currently rendered on the main viewport.
    /// False when an ImGui window has been torn off into its own OS window via multi-viewport.
    /// Defaults to true so non-window renderers always participate in input passthrough checks.
    /// </summary>
    public bool IsOnMainViewport { get; set; } = true;

    /// <summary>
    /// Whether this element actually rendered interactive content this frame. Renderers that gate
    /// their whole subtree on an open state (windows, modals, popups) override this to report that
    /// state. <see cref="RemoteImGuiWindow.ElementTreeRequiresInput"/> treats a false result as
    /// "no input needed" for the entire subtree, so a mounted-but-closed container that draws
    /// nothing can't hold the overlay in the foreground, its Inherit descendants would otherwise
    /// resolve RequireInput to true (the root default) even while invisible.
    /// Defaults to true so ordinary widgets and always-rendered containers participate normally.
    /// </summary>
    public virtual bool ParticipatesInInput => true;

    public abstract void ApplyState(BaseUIElementData data);
    public abstract void Render();
    public abstract BaseUIElementData? GetNewState();

    /// <summary>
    /// Renders only the widget itself (button, header label, etc.)  NOT children.
    /// The tooltip is applied immediately after this returns.
    /// Returns true if children should be rendered (always true for leaf elements).
    /// Default: calls <see cref="Render"/> and returns true (correct for all leaf renderers).
    /// Container renderers should override this + <see cref="RenderChildren"/>.
    /// </summary>
    public virtual bool RenderWidget() { Render(); return true; }

    /// <summary>
    /// Renders this element's children. Called after the tooltip has been applied.
    /// Default: no-op (correct for leaf renderers).
    /// </summary>
    public virtual void RenderChildren() { }

    public UIRenderer(BaseUIElementData data)
    {
        Data = data;
    }
}