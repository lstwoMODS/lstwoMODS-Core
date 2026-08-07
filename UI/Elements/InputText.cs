using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;

namespace lstwoMODS_Core.UI.Elements;

public class InputText : BaseUIElement<InputText>
{
    public Action<string>? OnChanged;
    public Action<ImGuiKey>? OnSpecialKey;
    public Action<bool>? OnFocusChanged;
    private Ref<string>? _binding;
    private Ref<bool>? _focusBinding;

    public string Value
    {
        get => ((InputTextData)Data).Value;
        set { ((InputTextData)Data).Value = value; MarkChanged(); }
    }

    public bool IsFocused => ((InputTextData)Data).IsFocused;

    /// <param name="hint">Placeholder text shown when empty. null = no hint.</param>
    /// <param name="multiline">If true, renders as InputTextMultiline with the given size.</param>
    public InputText(string name, string value = "", string hint = "", int maxLength = 256,
                     bool multiline = false, float sizeX = -1f, float sizeY = 100f,
                     ImGuiInputTextFlags flags = ImGuiInputTextFlags.None,
                     Action<string> onChanged = null, bool mainThread = true) : base(name)
    {
        Data = new InputTextData { Name = name, Value = value, Hint = hint, MaxLength = maxLength, Multiline = multiline, SizeX = sizeX, SizeY = sizeY, Flags = flags };
        OnChanged = onChanged;
        RunCallbacksOnMainThread = mainThread;
    }

    public InputText WithValue(Ref<string> binding)
    {
        _binding = binding;
        ((InputTextData)Data).Value = binding.Value;
        binding.Changed += v => Value = v;
        return this;
    }

    /// <summary>
    /// Register ImGuiKey values to watch while this input has keyboard focus. When any of
    /// them is pressed, <see cref="OnSpecialKey"/> fires with the key. Used for chat-style
    /// Tab-complete / Up/Down history navigation.
    /// </summary>
    public InputText WatchKeys(params ImGuiKey[] keys)
    {
        var d = (InputTextData)Data;
        d.WatchKeys = keys.Select(k => (int)k).ToList();
        return this;
    }

    /// <summary>Callback fired when any of the keys registered via <see cref="WatchKeys"/> is pressed.</summary>
    public InputText OnKey(Action<ImGuiKey> callback)
    {
        OnSpecialKey = callback;
        return this;
    }

    /// <summary>Callback fired when the input gains or loses keyboard focus.</summary>
    public InputText OnFocus(Action<bool> callback)
    {
        OnFocusChanged = callback;
        return this;
    }

    /// <summary>Bind the focus state to a <see cref="Ref{T}"/>. Changes from the UI flow into the ref.</summary>
    public InputText WithFocus(Ref<bool> binding)
    {
        _focusBinding = binding;
        ((InputTextData)Data).IsFocused = binding.Value;
        return this;
    }

    /// <summary>Request that the renderer grab keyboard focus on the next frame.</summary>
    public void FocusNextFrame()
    {
        var d = (InputTextData)Data;
        d.RequestFocus = true;
        d.RequestFocusVersion++;
        MarkChanged();
    }

    public override void ApplyReceivedData(BaseUIElementData data)
    {
        var oldData = (InputTextData)Data;
        var oldValue = oldData.Value;
        var oldFocus = oldData.IsFocused;
        var oldKeyVersion = oldData.LastKeyVersion;

        base.ApplyReceivedData(data);

        var newData = (InputTextData)Data;

        // Preserve mod-side fields that the renderer never sends back
        newData.RequestFocus        = oldData.RequestFocus;
        newData.RequestFocusVersion = oldData.RequestFocusVersion;
        newData.WatchKeys           = oldData.WatchKeys;

        if (oldValue != newData.Value)
        {
            if (_binding != null) _binding.Value = newData.Value;
            var v = newData.Value;
            InvokeCallback(() => OnChanged?.Invoke(v));
        }

        if (oldFocus != newData.IsFocused)
        {
            if (_focusBinding != null) _focusBinding.Value = newData.IsFocused;
            var f = newData.IsFocused;
            InvokeCallback(() => OnFocusChanged?.Invoke(f));
        }

        if (newData.LastKeyVersion != oldKeyVersion && newData.LastKeyPressed != 0)
        {
            var key = (ImGuiKey)newData.LastKeyPressed;
            InvokeCallback(() => OnSpecialKey?.Invoke(key));
        }
    }
}
