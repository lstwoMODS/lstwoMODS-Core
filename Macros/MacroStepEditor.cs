using System;
using System.Collections.Generic;
using lstwoMODS_Core.UI.Elements;
using Newtonsoft.Json.Linq;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// Supplies a custom editor for a macro step whose shape does not fit the auto-built
/// one-row-per-parameter layout (a variable number of sub-widgets, a bespoke arrangement, ...).
/// Attach an instance to <see cref="MacroMethodDescriptor.CustomEditor"/> and pair it with a
/// <see cref="MacroMethodDescriptor.ExecuteCustom"/> that reads the same data at run time. The
/// built-in Switch step (<see cref="MacroSwitchStep"/>) is written entirely against this public
/// API  anything it does, a plugin's custom step can do too.
///
/// One instance is shared across every step that uses the method, so it must hold <b>no</b>
/// per-step state in fields: keep transient UI handles in <see cref="MacroStepEditorContext.Tag"/>
/// and the persisted data in the step (via <see cref="MacroStepEditorContext.GetData{T}"/> /
/// <see cref="MacroStepEditorContext.SetData{T}"/>).
/// </summary>
public abstract class MacroStepEditor
{
    /// <summary>
    /// Build the editor's widgets for one step and return the root elements to place in the step
    /// body. Called once when the step row is created. Stash any handles you need for
    /// <see cref="Refresh"/> in <paramref name="ctx"/>'s <see cref="MacroStepEditorContext.Tag"/>.
    /// Elements you return are owned by the step row and torn down with it; only reach for
    /// <see cref="MacroStepEditorContext.AddElement"/> for widgets added <i>later</i> in response
    /// to user actions (e.g. a "+ add case" button), since the parents must already be live.
    /// </summary>
    public abstract IEnumerable<BaseUIElement> Build(MacroStepEditorContext ctx);

    /// <summary>Push the step's stored data into the widgets. Called every editor refresh; keep it
    /// cheap and skip sends when a widget already shows the value (see the helpers the Switch
    /// editor uses). Optional.</summary>
    public virtual void Refresh(MacroStepEditorContext ctx) { }

    /// <summary>Remove any elements this editor added at runtime via
    /// <see cref="MacroStepEditorContext.AddElement"/>. Called right before the step row itself is
    /// removed. The elements returned from <see cref="Build"/> are cleaned up automatically with
    /// the row, so many editors need nothing here. Optional.</summary>
    public virtual void Teardown(MacroStepEditorContext ctx) { }

    /// <summary>Optional short one-line hint appended to the collapsed step header (e.g. the value
    /// being switched on). Null for none.</summary>
    public virtual string Summary(MacroStep step) => null;
}

/// <summary>
/// Everything a <see cref="MacroStepEditor"/> needs for one step: the step and its macro, a unique
/// element-id prefix, a "persist my edits" callback, runtime element add/remove, and typed access
/// to the step's <see cref="MacroStep.Custom"/> data bag. One is created per step row and reused
/// across refreshes.
/// </summary>
public sealed class MacroStepEditorContext
{
    public Macro Macro { get; }
    public MacroStep Step { get; }

    /// <summary>Unique, stable prefix for this step's element ids (e.g. <c>Mc-{macroId}-s{stepId}</c>).
    /// Suffix your own fragment onto it so ids never collide with another step or another editor.</summary>
    public string IdScope { get; }

    /// <summary>Call after mutating the step's data so the change is saved and broadcast. Wraps
    /// <see cref="MacroManager.NotifyEdited"/>.</summary>
    public Action NotifyEdited { get; }

    /// <summary>Scratch slot for the editor's per-step UI handles (parallel to the widgets built
    /// in <see cref="MacroStepEditor.Build"/>). The editor owns the shape.</summary>
    public object Tag { get; set; }

    public MacroStepEditorContext(Macro macro, MacroStep step, string idScope, Action notifyEdited)
    {
        Macro = macro;
        Step = step;
        IdScope = idScope;
        NotifyEdited = notifyEdited;
    }

    /// <summary>Add an element into a live parent that is already part of the editor window (a
    /// container you returned from <see cref="MacroStepEditor.Build"/>). Use for variable-length
    /// lists. No-op when the window is gone.</summary>
    public void AddElement(BaseUIElement child, BaseUIElement parent, int index = -1)
        => Plugin.Window?.AddElement(child, parent, index);

    /// <summary>Remove a previously added element (and its subtree). No-op when the window is gone.</summary>
    public void RemoveElement(BaseUIElement element)
    {
        if (element != null) Plugin.Window?.RemoveElement(element);
    }

    /// <summary>Deserialize the step's data bag to <typeparamref name="T"/>, or a fresh instance
    /// when the step has none yet. Mutate the result and pass it back through
    /// <see cref="SetData{T}"/> to persist.</summary>
    public T GetData<T>() where T : new()
    {
        if (Step.Custom == null) return new T();
        try { return Step.Custom.ToObject<T>() ?? new T(); }
        catch { return new T(); }
    }

    /// <summary>Store <paramref name="value"/> into the step's data bag. Does not itself save;
    /// call <see cref="NotifyEdited"/> after.</summary>
    public void SetData<T>(T value) => Step.Custom = value == null ? null : JObject.FromObject(value);
}
