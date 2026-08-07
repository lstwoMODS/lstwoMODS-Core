using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI;

/// <summary>
/// Base class for reusable mod-side UI components.
///
/// A component is a <see cref="Group"/> under the hood  it renders as
/// <c>ImGui.BeginGroup / EndGroup</c> on the overlay side. No new overlay code
/// is ever needed; just derive from this class, call <see cref="Add"/> with your
/// child elements, and use the component anywhere an element is accepted.
///
/// <code>
/// public class MyWidget : UIComponent
/// {
///     private readonly SliderFloat _slider;
///
///     public MyWidget(string name) : base(name)
///     {
///         _slider = new SliderFloat(name + "-s", 0f, 0f, 1f);
///         Add(new UIText(name + "-lbl", "My Widget"),
///             _slider);
///     }
///
///     public float Value => _slider.Value;
/// }
/// </code>
/// </summary>
public abstract class UIComponent : Container
{
    protected UIComponent(string name) : base(name) { }

    /// <summary>Append elements to this component. Call from the derived constructor.</summary>
    protected void Add(params BaseUIElement[] elements)
    {
        foreach (var el in elements)
        {
            Children.Add(el);
            ((ContainerData)Data).Children.Add(el.Data);
        }
    }
}


/// <summary>
/// A visual section with a <see cref="SeparatorText"/> header and indented body.
///
/// <code>
/// new Section("audio", "Audio Settings",
///     new SliderFloat("volume", 0.8f, 0f, 1f),
///     new Checkbox("mute", false))
/// </code>
/// </summary>
public class Section : UIComponent
{
    /// <param name="name">Unique element ID.</param>
    /// <param name="title">Title shown in the separator bar.</param>
    /// <param name="content">Child elements indented under the title.</param>
    public Section(string name, string title, params BaseUIElement[] content) : base(name)
    {
        Add(new SeparatorText(name + "-hdr",    title));
        Add(new Indent(       name + "-indent"));
        Add(content);
        Add(new Indent(       name + "-unindent", unindent: true));
    }
}

/// <summary>
/// A row of radio buttons sharing a single selection value.
/// Buttons are placed on the same line automatically.
///
/// <code>
/// var quality = new Ref&lt;int&gt;(1);
/// new OptionGroup("quality-grp", quality,
///     ("Low",    0),
///     ("Medium", 1),
///     ("High",   2))
/// </code>
/// </summary>
public class OptionGroup : UIComponent
{
    private readonly Ref<int> _selection;

    public int SelectedValue => _selection.Value;

    /// <param name="name">Unique element ID.</param>
    /// <param name="selection">Shared <see cref="Ref{T}"/> holding the active option value.</param>
    /// <param name="options">Pairs of (display label, int value) for each radio button.</param>
    public OptionGroup(string name, Ref<int> selection, params (string Label, int Value)[] options) : base(name)
    {
        _selection = selection;

        for (int i = 0; i < options.Length; i++)
        {
            var (label, val) = options[i];
            Add(new RadioButton(name + "-opt-" + val, label, selection, val));
            if (i < options.Length - 1)
                Add(new SameLine(name + "-sl-" + i));
        }
    }
}

/// <summary>
/// A toggle button that switches between two labels and fires a callback on each press.
/// Useful for play/pause, show/hide, lock/unlock, etc.
///
/// <code>
/// new ToggleButton("pause-btn", "Pause Game", "Resume Game",
///     onChanged: paused => Time.timeScale = paused ? 0f : 1f)
/// </code>
/// </summary>
public class ToggleButton : UIComponent
{
    private bool _state;
    private readonly Button _button;
    private readonly string _labelOff;
    private readonly string _labelOn;

    /// <param name="name">Unique element ID.</param>
    /// <param name="labelOff">Button label when state is false.</param>
    /// <param name="labelOn">Button label when state is true.</param>
    /// <param name="initialState">Starting state.</param>
    /// <param name="onChanged">Callback receiving the new state on each click.</param>
    /// <param name="mainThread">Route callback to Unity main thread.</param>
    public ToggleButton(string name, string labelOff, string labelOn,
                        bool initialState = false, Action<bool> onChanged = null,
                        bool mainThread = true) : base(name)
    {
        _state    = initialState;
        _labelOff = labelOff;
        _labelOn  = labelOn;

        _button = new Button(name + "-btn", onPressed: OnPressed, mainThread: mainThread);
        _button.Data.Name = initialState ? labelOn : labelOff;

        Add(_button);

        void OnPressed()
        {
            _state = !_state;
            _button.Data.Name = _state ? _labelOn : _labelOff;
            _button.MarkChanged();
            onChanged?.Invoke(_state);
        }
    }

    public bool State => _state;

    public ToggleButton WithState(Ref<bool> stateRef)
    {
        stateRef.Changed += b =>
        {
            _state = b;
            _button.Data.Name = b ? _labelOn : _labelOff;
            _button.MarkChanged();
        };
        
        return this;
    }

    public ToggleButton WithContentWidth(bool value = true)
    {
        _button.WithContentWidth(value);
        return this;
    }
}

/// <summary>
/// A confirm/cancel modal dialog.
/// Add it to a window once, then call <see cref="Show"/> or <see cref="Show(string)"/> whenever needed.
///
/// <code>
/// // One-liner creation and open
/// var dlg = new ConfirmDialog("del-dlg", "Delete Item",
///     onConfirm: () => DeleteItem(),
///     onCancel:  () => { });
/// AddElement(dlg);
///
/// // Open it from a button
/// new Button("del-btn", "Delete", () =>
///     dlg.Show("Are you sure you want to delete this item?\nThis cannot be undone."))
/// </code>
/// </summary>
public class ConfirmDialog : UIComponent
{
    private readonly Modal  _modal;
    private readonly UIText _messageText;
    public Action OnConfirm;
    public Action OnCancel;

    /// <param name="name">Unique element ID.</param>
    /// <param name="title">Title shown in the modal header bar.</param>
    /// <param name="message">Default body text.</param>
    /// <param name="confirmLabel">Label on the confirm button (default "OK").</param>
    /// <param name="cancelLabel">Label on the cancel button (default "Cancel").</param>
    /// <param name="onConfirm">Callback fired when the user clicks the confirm button.</param>
    /// <param name="onCancel">Callback fired when the user clicks the cancel button or the X.</param>
    /// <param name="mainThread">Route callbacks to Unity main thread.</param>
    public ConfirmDialog(string name,
                         string title          = "Confirm",
                         string message        = "Are you sure?",
                         string confirmLabel   = "OK",
                         string cancelLabel    = "Cancel",
                         Action onConfirm      = null,
                         Action onCancel       = null,
                         bool   mainThread     = true) : base(name)
    {
        OnConfirm = onConfirm;
        OnCancel  = onCancel;

        _messageText = new UIText($"{name}-msg", message);

        _modal = new Modal($"{name}-modal", title,
            _messageText,
            new Spacing($"{name}-sp"),
            new Button(confirmLabel, () => { _modal.Close(); OnConfirm?.Invoke(); }, mainThread)
                .WithItemWidth(80f),
            new SameLine($"{name}-sl"),
            new Button(cancelLabel,  () => { _modal.Close(); OnCancel?.Invoke();  }, mainThread)
                .WithItemWidth(80f)
        ).WithNoClose();

        Add(_modal);
    }

    /// <summary>Open the dialog with the default message.</summary>
    public void Show() => _modal.Open();

    /// <summary>Open the dialog with a runtime-specified message.</summary>
    public void Show(string message)
    {
        ((TextData)_messageText.Data).Text = message;
        _messageText.MarkChanged();
        _modal.Open();
    }

    /// <summary>Close the dialog programmatically (fires neither callback).</summary>
    public void Close() => _modal.Close();

    public bool IsOpen => _modal.IsOpen;

    public ConfirmDialog OnConfirmAction(Action cb) { OnConfirm = cb; return this; }
    public ConfirmDialog OnCancelAction(Action cb)  { OnCancel  = cb; return this; }
}

/// <summary>
/// A simple one-button alert/info dialog with an OK button and no cancel.
///
/// <code>
/// var alert = new AlertDialog("err-dlg", "Error");
/// AddElement(alert);
/// alert.Show("Failed to load config.json");
/// </code>
/// </summary>
public class AlertDialog : UIComponent
{
    private readonly Modal  _modal;
    private readonly UIText _messageText;
    public Action OnDismissed;

    /// <param name="name">Unique element ID.</param>
    /// <param name="title">Title shown in the modal header.</param>
    /// <param name="message">Default body text.</param>
    /// <param name="dismissLabel">Label on the dismiss button (default "OK").</param>
    /// <param name="onDismissed">Optional callback when the user clicks OK.</param>
    public AlertDialog(string name,
                       string title        = "Alert",
                       string message      = "",
                       string dismissLabel = "OK",
                       Action onDismissed  = null,
                       bool   mainThread   = true) : base(name)
    {
        OnDismissed = onDismissed;

        _messageText = new UIText($"{name}-msg", message);

        _modal = new Modal($"{name}-modal", title,
            _messageText,
            new Spacing($"{name}-sp"),
            new Button(dismissLabel, () => { _modal.Close(); OnDismissed?.Invoke(); }, mainThread)
                .WithItemWidth(80f)
        ).WithNoClose();

        Add(_modal);
    }

    public void Show()                { _modal.Open(); }
    public void Show(string message)  { ((TextData)_messageText.Data).Text = message; _messageText.MarkChanged(); _modal.Open(); }
    public void Close()               { _modal.Close(); }
    public bool IsOpen                => _modal.IsOpen;
    public AlertDialog OnDismiss(Action cb) { OnDismissed = cb; return this; }
}
