using System;
using System.Collections.Generic;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Overlay.UiRenderers;

namespace lstwoMODS_Overlay;

public class OverlayContext : IOverlayContext
{
    internal readonly Dictionary<string, Action<IpcMessage>> MessageHandlers = new Dictionary<string, Action<IpcMessage>>();

    public void RegisterRenderer(Type dataType, Type rendererType)
    {
        UIRendererRegistry.DataToRenderer[dataType] = rendererType;
    }

    public void RegisterRenderer<TData, TRenderer>()
        where TData    : BaseUIElementData
        where TRenderer : UIRenderer
    {
        UIRendererRegistry.DataToRenderer[typeof(TData)] = typeof(TRenderer);
    }

    public void RegisterMessageHandler(string messageType, Action<IpcMessage> handler)
    {
        MessageHandlers[messageType] = handler;
    }

    public void SendToMod(string json)
    {
        Program.IpcChannel.SendMessage(new IpcMessage { Type = "_plugin", Payload = json });
    }

    public void ShowOpenFileDialog(FileDialogOptions options, Action<string?> onResult)
    {
        FileDialog.ShowOpen(options, onResult);
    }

    public void ShowSaveFileDialog(FileDialogOptions options, Action<string?> onResult)
    {
        FileDialog.ShowSave(options, onResult);
    }
}
