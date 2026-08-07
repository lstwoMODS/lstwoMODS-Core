using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// The built-in <c>core.runMacro</c> step: run another macro, optionally passing it arguments and/or
/// waiting for its return value. The target is either a fixed pick or a run-time expression (an
/// if/loop primitive: an expression resolving to a macro id, or <c>""</c> to run nothing). When a
/// fixed target declares parameters (via a <see cref="MacroTriggerBuiltins.CalledId"/> "Called by
/// Macro" trigger) the editor shows one expression field per parameter and passes the evaluated
/// values in as the sub-run's trigger values, so the callee reads each as a bare variable (or
/// <c>trigger("name")</c>). Its shape varies with the picked macro, so like Switch it is a
/// custom-editor step built on the public <see cref="MacroStepEditor"/> API.
/// </summary>
public static class MacroRunStep
{
    public const string Id = "core.runMacro";

    /// <summary>Persisted configuration, stored in <see cref="MacroStep.Custom"/>.</summary>
    private sealed class RunData
    {
        public bool ExprMode;                                                       // pick a macro vs. run-time expression
        public string Macro = "";                                                   // target macro ref/slug (pick mode)
        public string Expr = "";                                                    // expression resolving to a macro ref (expr mode)
        public bool Wait;                                                           // wait and take its return value
        public Dictionary<string, string> Args = new(StringComparer.OrdinalIgnoreCase); // param name -> expression (pick mode)
    }

    internal static void Register()
    {
        MacroRegistry.Register(new MacroMethodDescriptor
        {
            Id = Id,
            Label = "Run Macro",
            Category = "Flow",
            PickerLabel = "Run Macro",
            ReturnType = typeof(IEnumerator),
            OutputType = typeof(object), // a waited call surfaces the callee's Return value
            CustomEditor = new RunEditor(),
            ExecuteCustom = ExecuteRun,
        });
    }

    private static object ExecuteRun(CustomStepRunContext c)
    {
        var data = ReadData(c.Step);

        Macro target;
        if (data.ExprMode)
        {
            if (string.IsNullOrWhiteSpace(data.Expr)) return null; // intentionally run nothing
            var val = c.Eval(data.Expr);
            target = val as Macro ?? MacroManager.FindByRef(MacroValues.ToDisplay(val));
        }
        else
        {
            target = MacroManager.FindByRef(data.Macro); // "" / null resolves to null = run nothing
        }
        if (target == null) return null;

        // Arguments only apply to a fixed target (its parameters are known at edit time).
        var values = data.ExprMode ? null : BuildArgs(target, data, c);

        var chain = MacroRunner.CurrentChain;
        var off = MacroManager.AdvanceToggle(target);
        if (data.Wait) return MacroRunner.RunNested(target, chain, off, values);
        MacroRunner.RunDetached(target, chain, off, values);
        return null;
    }

    /// <summary>Evaluate one argument per declared parameter into the trigger-value map, or null when
    /// the target declares no parameters (a plain run).</summary>
    private static IReadOnlyDictionary<string, object> BuildArgs(Macro target, RunData data, CustomStepRunContext c)
    {
        var prms = Params(target);
        if (prms.Length == 0) return null;

        var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in prms)
        {
            if (string.IsNullOrEmpty(p.Key)) continue;
            var expr = data.Args != null && data.Args.TryGetValue(p.Key, out var e) ? e : null;
            dict[p.Key] = string.IsNullOrWhiteSpace(expr)
                ? MacroValues.DefaultFor(p.Type)
                : MacroValues.Coerce(c.Eval(expr), p.Type);
        }
        return dict;
    }

    /// <summary>The parameters a macro accepts from a caller: its trigger's resolved outputs.</summary>
    private static MacroTriggerOutput[] Params(Macro target)
        => target == null ? Array.Empty<MacroTriggerOutput>()
         : MacroTriggerRegistry.For(target.Trigger)?.ResolveOutputs(target.Trigger) ?? Array.Empty<MacroTriggerOutput>();

    private static RunData ReadData(MacroStep step)
    {
        if (step.Custom != null)
        {
            try { return step.Custom.ToObject<RunData>() ?? Migrate(step); }
            catch { return Migrate(step); }
        }
        return Migrate(step);
    }

    /// <summary>Best-effort read of a legacy (pre-custom-editor) Run Macro step's <c>macro</c>/<c>wait</c>
    /// value sources, so old steps keep working. New steps store everything in <see cref="MacroStep.Custom"/>.</summary>
    private static RunData Migrate(MacroStep step)
    {
        var d = new RunData();
        switch (step.GetArg("macro"))
        {
            case TypedModeValueSource t:                                            d.Macro = t.Arg ?? ""; break;
            case ExpressionValueSource e when !string.IsNullOrWhiteSpace(e.Text):   d.ExprMode = true; d.Expr = e.Text; break;
            case ConstantValueSource cst:                                           d.Macro = MacroValues.ToDisplay(cst.Value); break;
        }
        if (step.GetArg("wait") is ConstantValueSource w)
            d.Wait = string.Equals(MacroValues.ToDisplay(w.Value)?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        return d;
    }

    // ── Editor ────────────────────────────────────────────────────────────

    private sealed class RunUI
    {
        public Combo ModeCombo;
        public Container PickWrap;
        public Combo MacroCombo;
        public Container ExprWrap;
        public InputText ExprInput;
        public Checkbox WaitBox;
        public Container ArgsWrap;   // label + host; hidden in expression mode or when no params
        public Container ArgsHost;
        public readonly List<ArgRow> Rows = new();
        public string[] MacroItems = Array.Empty<string>();     // "(none)" + slugs, parallel to combo items
        public string BoundMacro = "";                          // slug the arg rows were built for
        public string[] BoundParamKeys = Array.Empty<string>(); // keys the arg rows were built for
        public int NextRowId;                                   // never reused, so ids never collide
    }

    private sealed class ArgRow
    {
        public BaseUIElement Row;
        public string Key;
        public InputText Input;
    }

    private sealed class RunEditor : MacroStepEditor
    {
        public override string Summary(MacroStep step)
        {
            var d = ReadData(step);
            var s = d.ExprMode ? d.Expr : d.Macro;
            return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        }

        public override IEnumerable<BaseUIElement> Build(MacroStepEditorContext ctx)
        {
            var data = ctx.GetData<RunData>();
            var ui = new RunUI { MacroItems = MacroItems() };
            ctx.Tag = ui;
            var scope = ctx.IdScope;

            ui.ModeCombo = new Combo($"##{scope}-run-mode", new[] { "Pick a macro", "Expression (choose at run time)" },
                    data.ExprMode ? 1 : 0, i => OnModeChanged(ctx, ui, i == 1))
                .WithItemWidth(-1f);

            ui.MacroCombo = new Combo($"##{scope}-run-macro", ui.MacroItems, IndexOfSlug(ui, data.Macro),
                    i => OnMacroChanged(ctx, ui, SlugAt(ui, i)))
                .WithItemWidth(-1f);
            ui.PickWrap = new Container($"{scope}-run-pick", ui.MacroCombo);

            ui.ExprInput = new InputText($"##{scope}-run-expr", value: data.Expr ?? "",
                    hint: "macro id / slug, or \"\" for none", maxLength: 256,
                    onChanged: v => Mutate(ctx, d => d.Expr = v ?? ""))
                .WithItemWidth(-1f)
                .WithTooltip("C# expression resolving to a macro id/slug to run at run time. "
                           + "Empty (or \"\") runs nothing. e.g. var(\"next\") or prev.");
            ui.ExprWrap = new Container($"{scope}-run-expr-wrap", ui.ExprInput);

            ui.WaitBox = new Checkbox($"Wait for the macro to finish##{scope}-run-wait", data.Wait,
                    v => Mutate(ctx, d => d.Wait = v))
                .WithTooltip("Wait for the called macro to finish, and take its Return value as this step's output.");

            // A row per declared parameter of the picked macro (Build runs with the host not yet live,
            // so seed them as the container's children; macro changes below use Add/RemoveElement).
            var prms = Params(MacroManager.FindByRef(data.Macro));
            foreach (var p in prms)
                ui.Rows.Add(BuildArgRow(ctx, ui, p, ArgValue(data, p.Key)));
            ui.BoundMacro = data.Macro ?? "";
            ui.BoundParamKeys = prms.Select(p => p.Key).ToArray();
            ui.ArgsHost = new Container($"{scope}-run-args", ui.Rows.Select(r => r.Row).ToArray());
            ui.ArgsWrap = new Container($"{scope}-run-args-wrap",
                new UIText($"{scope}-run-args-lbl", "Arguments"),
                ui.ArgsHost);

            ApplyVisibility(ui, data);

            return new BaseUIElement[]
            {
                new UIText($"{scope}-run-lbl", "Run macro"),
                ui.ModeCombo,
                ui.PickWrap,
                ui.ExprWrap,
                ui.WaitBox,
                new Spacing($"{scope}-run-sp"),
                ui.ArgsWrap,
            };
        }

        public override void Refresh(MacroStepEditorContext ctx)
        {
            if (ctx.Tag is not RunUI ui) return;
            var data = ctx.GetData<RunData>();

            SetCombo(ui.ModeCombo, data.ExprMode ? 1 : 0);

            var items = MacroItems();
            if (!SameItems(items, ui.MacroItems))
            {
                ui.MacroItems = items;
                SetItems(ui.MacroCombo, items);
            }
            SetCombo(ui.MacroCombo, IndexOfSlug(ui, data.Macro));
            if (!ui.ExprInput.IsFocused && ui.ExprInput.Value != (data.Expr ?? "")) ui.ExprInput.Value = data.Expr ?? "";
            if (ui.WaitBox.Value != data.Wait) ui.WaitBox.Value = data.Wait;

            // Rebuild the argument rows when the chosen macro (or its declared parameters) changed.
            var keys = Params(MacroManager.FindByRef(data.Macro)).Select(p => p.Key).ToArray();
            if (!string.Equals(ui.BoundMacro, data.Macro ?? "", StringComparison.Ordinal) || !SameItems(keys, ui.BoundParamKeys))
            {
                RebuildRows(ctx, ui);
            }
            else
            {
                foreach (var row in ui.Rows)
                {
                    var v = ArgValue(data, row.Key);
                    if (!row.Input.IsFocused && row.Input.Value != v) row.Input.Value = v;
                }
            }

            ApplyVisibility(ui, data);
        }

        private void OnModeChanged(MacroStepEditorContext ctx, RunUI ui, bool exprMode)
        {
            Mutate(ctx, d => d.ExprMode = exprMode);
            ApplyVisibility(ui, ctx.GetData<RunData>());
        }

        private void OnMacroChanged(MacroStepEditorContext ctx, RunUI ui, string slug)
        {
            Mutate(ctx, d => d.Macro = slug ?? "");
            RebuildRows(ctx, ui);
            ApplyVisibility(ui, ctx.GetData<RunData>());
        }

        /// <summary>Show the pick or expression target editor per mode, and the arguments block only
        /// for a fixed target that declares parameters.</summary>
        private static void ApplyVisibility(RunUI ui, RunData data)
        {
            ui.PickWrap.SetVisible(!data.ExprMode);
            ui.ExprWrap.SetVisible(data.ExprMode);
            ui.ArgsWrap.SetVisible(!data.ExprMode && ui.Rows.Count > 0);
        }

        private void RebuildRows(MacroStepEditorContext ctx, RunUI ui)
        {
            var data = ctx.GetData<RunData>();
            var prms = Params(MacroManager.FindByRef(data.Macro));

            foreach (var row in ui.Rows) ctx.RemoveElement(row.Row);
            ui.Rows.Clear();

            foreach (var p in prms)
            {
                var row = BuildArgRow(ctx, ui, p, ArgValue(data, p.Key));
                ui.Rows.Add(row);
                ctx.AddElement(row.Row, ui.ArgsHost);
            }
            ui.BoundMacro = data.Macro ?? "";
            ui.BoundParamKeys = prms.Select(p => p.Key).ToArray();
        }

        private ArgRow BuildArgRow(MacroStepEditorContext ctx, RunUI ui, MacroTriggerOutput p, string value)
        {
            var scope = ctx.IdScope;
            var rowId = ui.NextRowId++;
            var key = p.Key;
            var typeName = TypeLabel(p.Type);
            var row = new ArgRow { Key = key };

            row.Input = new InputText($"##{scope}-run-a{rowId}", value: value ?? "",
                    hint: (typeName != null ? typeName + " " : "") + "expression...", maxLength: 256,
                    onChanged: v => Mutate(ctx, d => d.Args[key] = v ?? ""))
                .WithItemWidth(-1f)
                .WithTooltip($"Value passed as '{key}'{(typeName != null ? $" ({typeName})" : "")}. "
                           + "C# expression, e.g. prev, var(\"x\"), 3 * count.");

            row.Row = new Container($"{scope}-run-a{rowId}-row",
                new UIText($"{scope}-run-a{rowId}-lbl", key),
                row.Input);
            return row;
        }

        private static void Mutate(MacroStepEditorContext ctx, Action<RunData> edit)
        {
            var data = ctx.GetData<RunData>();
            edit(data);
            ctx.SetData(data);
            ctx.NotifyEdited();
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string ArgValue(RunData data, string key)
        => data.Args != null && key != null && data.Args.TryGetValue(key, out var v) ? v ?? "" : "";

    /// <summary>Friendly type name for an argument's hint/tooltip, or null for an untyped parameter.</summary>
    private static string TypeLabel(Type t)
    {
        if (t == null || t == typeof(object)) return null;
        var d = MacroTypes.For(t);
        if (d != null) return d.DisplayName;
        if (t == typeof(int)) return "int";
        if (t == typeof(float)) return "number";
        if (t == typeof(bool)) return "bool";
        if (t == typeof(string)) return "text";
        return t.Name;
    }

    private static string[] MacroItems()
        => new[] { "(none)" }.Concat(MacroManager.Macros.Select(m => m.Slug)).ToArray();

    private static string SlugAt(RunUI ui, int index)
        => index <= 0 || index >= ui.MacroItems.Length ? "" : ui.MacroItems[index];

    private static int IndexOfSlug(RunUI ui, string slug)
    {
        if (string.IsNullOrEmpty(slug)) return 0;
        var i = Array.IndexOf(ui.MacroItems, slug);
        return i < 0 ? 0 : i;
    }

    private static bool SameItems(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (a[i] != b[i]) return false;
        return true;
    }

    private static void SetItems(Combo combo, string[] items)
    {
        ((ComboData)combo.Data).Items = items;
        combo.MarkChanged();
    }

    private static void SetCombo(Combo combo, int index)
    {
        if (combo.SelectedIndex != index) combo.SelectedIndex = index;
    }
}
