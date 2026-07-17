namespace lstwoMODS_Overlay;

/// <summary>
/// Implement this interface (or extend <see cref="OverlayPluginBase"/>) to add new element types,
/// server-side UI, custom IPC handlers, or other overlay-side behaviour.
/// Drop the compiled DLL into the <c>plugins/</c> folder next to the overlay exe.
/// </summary>
public interface IOverlayPlugin
{
    /// <summary>Unique reverse-domain identifier, e.g. "com.mymod.MyPlugin".</summary>
    string Id { get; }

    /// <summary>Called once at startup before any window is created. Register renderers and message handlers here.</summary>
    void Initialize(IOverlayContext ctx);

    /// <summary>Called each time a new <see cref="RemoteImGuiWindow"/> is created. Use for per-window frame callbacks.</summary>
    void OnWindowCreated(RemoteImGuiWindow window);

    /// <summary>Called on overlay shutdown. Release any resources here.</summary>
    void Shutdown();
}

/// <summary>
/// Convenience base class with empty no-op implementations of the optional plugin methods.
/// Extend this instead of implementing <see cref="IOverlayPlugin"/> directly when you only need <see cref="Initialize"/>.
/// </summary>
public abstract class OverlayPluginBase : IOverlayPlugin
{
    public abstract string Id { get; }
    public abstract void Initialize(IOverlayContext ctx);
    public virtual void OnWindowCreated(RemoteImGuiWindow window) { }
    public virtual void Shutdown() { }
}
