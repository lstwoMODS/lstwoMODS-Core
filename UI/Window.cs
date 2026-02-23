using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.Messages;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI;

public abstract class Window(string title, int width, int height, WindowType windowType, IntPtr followWindowHandle) : IDisposable
{
    public string Title = title;
    public int Width = width, Height = height;
    public WindowType WindowType = windowType;
    public IntPtr FollowWindowHandle = followWindowHandle;

    public async Task Initialize()
    {
        var demoWindow = new DemoWindow();
        
        var message = new WindowInitMessage
        {
            Title = Title,
            WindowType = WindowType,
            FollowWindowHandle = FollowWindowHandle.ToInt64(),
            Width = Width,
            Height = Height,
            Elements = [demoWindow]
        };
        
        UIManager.IpcChannel.SendMessage(message.Serialize());
    }

    public void Dispose()
    {
    }
}