using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using lstwoMODS.ImGui.Shared;

namespace lstwoMODS_Overlay;

public class PluginManager
{
    private readonly List<IOverlayPlugin> _plugins = new List<IOverlayPlugin>();
    private readonly OverlayContext _ctx;

    public PluginManager(OverlayContext ctx)
    {
        _ctx = ctx;
    }

    /// <summary>Register and initialize a built-in plugin.</summary>
    public void Register(IOverlayPlugin plugin)
    {
        try
        {
            plugin.Initialize(_ctx);
            _plugins.Add(plugin);
            Logger.Log($"[PluginManager] Loaded plugin: {plugin.Id}");
        }
        catch (Exception ex)
        {
            Logger.Log($"[PluginManager] Failed to initialize plugin {plugin.Id}: {ex.Message}");
        }
    }

    /// <summary>Scan a directory for plugin DLLs and load any <see cref="IOverlayPlugin"/> implementations found.</summary>
    public void LoadFromDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return;

        foreach (var dll in Directory.GetFiles(dir, "*.dll"))
        {
            try
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var type in asm.GetTypes())
                {
                    if (!typeof(IOverlayPlugin).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface)
                        continue;

                    var plugin = (IOverlayPlugin)Activator.CreateInstance(type);
                    Register(plugin);
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"[PluginManager] Failed to load plugin DLL '{Path.GetFileName(dll)}': {ex.Message}");
            }
        }
    }

    /// <summary>Notify all plugins that a new window has been created.</summary>
    public void NotifyWindowCreated(RemoteImGuiWindow window)
    {
        foreach (var plugin in _plugins)
        {
            try { plugin.OnWindowCreated(window); }
            catch (Exception ex) { Logger.Log($"[PluginManager] Plugin {plugin.Id} OnWindowCreated error: {ex.Message}"); }
        }
    }

    /// <summary>
    /// Try to route an IPC message to a registered plugin handler.
    /// Returns true if a handler was found and invoked.
    /// </summary>
    public bool TryHandleMessage(IpcMessage message)
    {
        if (_ctx.MessageHandlers.TryGetValue(message.Type, out var handler))
        {
            handler(message);
            return true;
        }
        return false;
    }

    /// <summary>Shut down all plugins in reverse registration order.</summary>
    public void Shutdown()
    {
        for (var i = _plugins.Count - 1; i >= 0; i--)
        {
            try { _plugins[i].Shutdown(); }
            catch (Exception ex) { Logger.Log($"[PluginManager] Plugin {_plugins[i].Id} Shutdown error: {ex.Message}"); }
        }
    }
}
