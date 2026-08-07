using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// The built-in <c>core.switch</c> step: evaluate an expression, then run the macro whose case
/// value equals the result (or a default macro when none match). Its number of cases varies, so it
/// does not fit the one-row-per-parameter step layout  instead it is the reference consumer of the
/// public custom-step API (<see cref="MacroStepEditor"/> + <see cref="MacroMethodDescriptor.ExecuteCustom"/>),
/// written with nothing a plugin's own custom step couldn't use.
/// </summary>
public static class MacroSwitchStep
{
    public const string Id = "core.switch";

    /// <summary>Persisted switch configuration, stored in <see cref="MacroStep.Custom"/>.</summary>
    private sealed class SwitchData
    {
        public string Test = "";                 // expression producing the value to match
        public bool Wait;                        // wait for the chosen macro (and take its return value)
        public string DefaultMacro = "";         // macro ref run when no case matches ("" = nothing)
        public List<SwitchCase> Cases = new();
    }

    private sealed class SwitchCase
    {
        public string Value = "";   // compared (numeric-aware) against the test result
        public string Macro = "";   // macro ref to run ("" = nothing)
    }

    internal static void Register()
    {
        MacroRegistry.Register(new MacroMethodDescriptor
        {
            Id = Id,
            Label = "Switch",
            Category = "Flow",
            ReturnType = typeof(IEnumerator),
            OutputType = typeof(object), // a waited switch surfaces the chosen macro's Return value
            CustomEditor = new SwitchEditor(),
            ExecuteCustom = ExecuteSwitch,
        });
    }

    private static object ExecuteSwitch(CustomStepRunContext c)
    {
        var data = ReadData(c.Step);

        var value = c.Eval(string.IsNullOrWhiteSpace(data.Test) ? "null" : data.Test);

        var chosen = data.DefaultMacro;
        foreach (var kase in data.Cases)
            if (CaseMatches(value, kase.Value)) { chosen = kase.Macro; break; }

        var target = MacroManager.FindByRef(chosen); // "" / null resolves to null = run nothing
        return MacroRegistry.RunPicked(target, data.Wait);
    }

    /// <summary>Match a case's stored string against the evaluated value: numeric comparison when
    /// both look numeric, bool comparison for booleans, else a trimmed case-insensitive string
    /// compare on the value's display form (so enums and names work).</summary>
    private static bool CaseMatches(object value, string caseValue)
    {
        caseValue = caseValue?.Trim() ?? "";
        if (value == null) return caseValue.Length == 0;

        var display = MacroValues.ToDisplay(value);

        if (double.TryParse(display, NumberStyles.Any, CultureInfo.InvariantCulture, out var a)
            && double.TryParse(caseValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var b))
            return Math.Abs(a - b) < 1e-9;

        if (value is bool vb && bool.TryParse(caseValue, out var cb)) return vb == cb;

        return string.Equals(display, caseValue, StringComparison.OrdinalIgnoreCase);
    }

    private static SwitchData ReadData(MacroStep step)
    {
        if (step.Custom == null) return new SwitchData();
        try { return step.Custom.ToObject<SwitchData>() ?? new SwitchData(); }
        catch { return new SwitchData(); }
    }

    // ── Editor ────────────────────────────────────────────────────────────

    /// <summary>Per-step UI handles, kept in the context's Tag (the editor instance is shared, so
    /// it holds no per-step state itself).</summary>
    private sealed class SwitchUI
    {
        public InputText TestInput;
        public Checkbox WaitBox;
        public Combo DefaultCombo;
        public Container CasesHost;
        public readonly List<CaseRow> Rows = new();
        public string[] MacroItems = Array.Empty<string>(); // "(none)" + slugs, parallel to combo items
        public int NextRowId;
    }

    private sealed class CaseRow
    {
        public BaseUIElement Row;
        public InputText ValueInput;
        public Combo MacroCombo;
    }

    private sealed class SwitchEditor : MacroStepEditor
    {
        public override string Summary(MacroStep step)
        {
            var test = ReadData(step).Test;
            return string.IsNullOrWhiteSpace(test) ? null : test.Trim();
        }

        public override IEnumerable<BaseUIElement> Build(MacroStepEditorContext ctx)
        {
            var data = ctx.GetData<SwitchData>();
            var ui = new SwitchUI { MacroItems = MacroItems() };
            ctx.Tag = ui;
            var scope = ctx.IdScope;

            ui.TestInput = new InputText($"##{scope}-sw-test", hint: "expression...", maxLength: 256,
                    onChanged: v => Mutate(ctx, d => d.Test = v ?? ""))
                .WithItemWidth(-1f)
                .WithTooltip("The value to switch on. C# expression, e.g. var(\"state\") or prev.");

            ui.WaitBox = new Checkbox($"Wait for the chosen macro##{scope}-sw-wait",
                    onChanged: v => Mutate(ctx, d => d.Wait = v))
                .WithTooltip("Wait for the chosen macro to finish (and take its return value as this step's output).");

            ui.DefaultCombo = new Combo($"##{scope}-sw-default", ui.MacroItems, 0,
                    i => Mutate(ctx, d => d.DefaultMacro = SlugAt(ui, i)))
                .WithItemWidth(-1f);

            ui.CasesHost = new Container($"{scope}-sw-cases");

            // A row per already-stored case (Build runs with the host not yet live, so seed
            // statically; the +/x buttons below use AddElement/RemoveElement once it is).
            foreach (var kase in data.Cases)
                ui.Rows.Add(BuildCaseRow(ctx, ui, kase));

            var addButton = new Button($"{Lucide.Plus} Add case##{scope}-sw-add", () => AddCase(ctx))
                .WithTooltip("Add a value -> macro case.");

            return new BaseUIElement[]
            {
                new UIText($"{scope}-sw-test-lbl", "Switch on"),
                ui.TestInput,
                ui.WaitBox,
                new Spacing($"{scope}-sw-sp0"),
                new UIText($"{scope}-sw-cases-lbl", "Cases (first match wins)"),
                ui.CasesHost,
                addButton,
                new Spacing($"{scope}-sw-sp1"),
                new UIText($"{scope}-sw-default-lbl", "Default (no match)"),
                ui.DefaultCombo,
            };
        }

        public override void Refresh(MacroStepEditorContext ctx)
        {
            if (ctx.Tag is not SwitchUI ui) return;
            var data = ctx.GetData<SwitchData>();

            if (!ui.TestInput.IsFocused && ui.TestInput.Value != (data.Test ?? ""))
                ui.TestInput.Value = data.Test ?? "";
            if (ui.WaitBox.Value != data.Wait) ui.WaitBox.Value = data.Wait;

            // Rebuild combo items if the set of macros changed (added / removed / renamed slug).
            var items = MacroItems();
            if (!SameItems(items, ui.MacroItems))
            {
                ui.MacroItems = items;
                SetItems(ui.DefaultCombo, items);
                foreach (var row in ui.Rows) SetItems(row.MacroCombo, items);
            }

            SetCombo(ui.DefaultCombo, IndexOfSlug(ui, data.DefaultMacro));

            // Rows stay parallel to data.Cases (only this editor mutates either), so sync by index.
            for (var i = 0; i < ui.Rows.Count && i < data.Cases.Count; i++)
            {
                var row = ui.Rows[i];
                var kase = data.Cases[i];
                if (!row.ValueInput.IsFocused && row.ValueInput.Value != (kase.Value ?? ""))
                    row.ValueInput.Value = kase.Value ?? "";
                SetCombo(row.MacroCombo, IndexOfSlug(ui, kase.Macro));
            }
        }

        private CaseRow BuildCaseRow(MacroStepEditorContext ctx, SwitchUI ui, SwitchCase kase)
        {
            var scope = ctx.IdScope;
            var rowId = ui.NextRowId++; // never reused, so a removed-then-added row never collides
            var row = new CaseRow();

            row.ValueInput = new InputText($"##{scope}-sw-c{rowId}-val", hint: "equals...", maxLength: 128,
                    onChanged: v => MutateCase(ctx, ui, row, (d, idx) => d.Cases[idx].Value = v ?? ""))
                .WithItemWidth(120f);

            row.MacroCombo = new Combo($"##{scope}-sw-c{rowId}-macro", ui.MacroItems, IndexOfSlug(ui, kase.Macro),
                    i => MutateCase(ctx, ui, row, (d, idx) => d.Cases[idx].Macro = SlugAt(ui, i)))
                .WithItemWidth(-1f);

            row.Row = new Container($"{scope}-sw-c{rowId}-row",
                new UIText($"{scope}-sw-c{rowId}-lbl", "="),
                new SameLine($"{scope}-sw-c{rowId}-sl0"), row.ValueInput,
                new SameLine($"{scope}-sw-c{rowId}-sl1"),
                new SmallButton($"{Lucide.X}##{scope}-sw-c{rowId}-del", () => RemoveCase(ctx, ui, row))
                    .WithTooltip("Remove this case"),
                row.MacroCombo);
            return row;
        }

        private void AddCase(MacroStepEditorContext ctx)
        {
            if (ctx.Tag is not SwitchUI ui) return;
            Mutate(ctx, d => d.Cases.Add(new SwitchCase()));
            var row = BuildCaseRow(ctx, ui, new SwitchCase());
            ui.Rows.Add(row);
            ctx.AddElement(row.Row, ui.CasesHost);
        }

        private void RemoveCase(MacroStepEditorContext ctx, SwitchUI ui, CaseRow row)
        {
            var idx = ui.Rows.IndexOf(row);
            if (idx < 0) return;
            ui.Rows.RemoveAt(idx);
            ctx.RemoveElement(row.Row);
            Mutate(ctx, d => { if (idx < d.Cases.Count) d.Cases.RemoveAt(idx); });
        }

        /// <summary>Apply an edit to a case identified by its row's current position (rows stay
        /// parallel to the stored cases).</summary>
        private void MutateCase(MacroStepEditorContext ctx, SwitchUI ui, CaseRow row, Action<SwitchData, int> edit)
        {
            var idx = ui.Rows.IndexOf(row);
            if (idx < 0) return;
            Mutate(ctx, d => { if (idx < d.Cases.Count) edit(d, idx); });
        }

        private static void Mutate(MacroStepEditorContext ctx, Action<SwitchData> edit)
        {
            var data = ctx.GetData<SwitchData>();
            edit(data);
            ctx.SetData(data);
            ctx.NotifyEdited();
        }
    }

    // ── Macro-list combo helpers ────────────────────────────────────────────

    private static string[] MacroItems()
    {
        var slugs = MacroManager.Macros.Select(m => m.Slug);
        return new[] { "(none)" }.Concat(slugs).ToArray();
    }

    private static string SlugAt(SwitchUI ui, int index)
        => index <= 0 || index >= ui.MacroItems.Length ? "" : ui.MacroItems[index];

    private static int IndexOfSlug(SwitchUI ui, string slug)
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
