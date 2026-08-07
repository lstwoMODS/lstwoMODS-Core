using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Overlay.UiRenderers;

namespace lstwoMODS_Overlay;

/// <summary>
/// API surface given to plugins during <see cref="IOverlayPlugin.Initialize"/>.
/// Provides global registration hooks that persist for the lifetime of the overlay.
/// </summary>
public interface IOverlayContext
{
    /// <summary>Register a renderer type for a shared data type (non-generic overload).</summary>
    void RegisterRenderer(Type dataType, Type rendererType);

    /// <summary>Register a renderer type for a shared data type.</summary>
    void RegisterRenderer<TData, TRenderer>()
        where TData  : BaseUIElementData
        where TRenderer : UIRenderer;

    /// <summary>
    /// Register a handler for a custom IPC message type sent from the mod side.
    /// <paramref name="handler"/> receives the raw <see cref="IpcMessage"/>; use
    /// <c>message.Payload</c> for the JSON body.
    /// </summary>
    void RegisterMessageHandler(string messageType, Action<IpcMessage> handler);

    /// <summary>Send a raw JSON string to the mod over IPC. Thread-safe.</summary>
    void SendToMod(string json);
}
