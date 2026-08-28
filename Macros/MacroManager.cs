using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS_Core.Hacks;
using Newtonsoft.Json;
using UnityEngine;

namespace lstwoMODS_Core.Macros;

/// <summary>
/// Owns the macro groups, their persistence and trigger arming. Macros live inside
/// <see cref="MacroGroup"/>s, each of which is saved to its own file (so a group can be shared
/// as a single file); one group is the always-present, undeletable <see cref="MacroGroup.IsDefault"/>
/// group that new macros land in. Every mutation saves to disk automatically; groups load lazily
/// on first access and are armed at startup via <see cref="Initialize"/>. Subscribe to
/// <see cref="Changed"/> to react to edits.
/// </summary>
public static class MacroManager
{
    private const string StorageId    = "lstwoMODS_Core";
    private const string GroupsFolder = "macros/groups";
    private const string LegacyKey    = "macros/macros"; // pre-groups single-file store
    private const string DefaultGroupName = "Default";

    /// <summary>Fired on the caller's thread after any mutation of the groups or macros.</summary>
    public static event Action Changed;

    private static List<MacroGroup> _groups;
    private static readonly List<IDisposable> _armedHandles = new();
    private static readonly Dictionary<string, bool> _toggleState = new();

    /// <summary>All macros across every group, flattened. Triggers, slugs and Run Macro
    /// references are global, so most lookups go through here.</summary>
    public static IReadOnlyList<Macro> Macros
    {
        get
        {
            EnsureLoaded();
            return AllMacros().ToList();
        }
    }

    /// <summary>The groups, default first. Never empty  the default group always exists.</summary>
    public static IReadOnlyList<MacroGroup> Groups
    {
        get
        {
            EnsureLoaded();
            return _groups;
        }
    }

    /// <summary>The undeletable catch-all group new macros land in when no group is chosen.</summary>
    public static MacroGroup DefaultGroup
    {
        get
        {
            EnsureLoaded();
            return _groups.First(g => g.IsDefault);
        }
    }

    private static IEnumerable<Macro> AllMacros() => _groups.SelectMany(g => g.Macros);

    /// <summary>The group that currently holds <paramref name="macro"/>, or null.</summary>
    public static MacroGroup GroupOf(Macro macro)
    {
        EnsureLoaded();
        return _groups.FirstOrDefault(g => g.Macros.Contains(macro));
    }

    /// <summary>Absolute path of the file backing a group  what to copy to share it.</summary>
    public static string GroupFilePath(MacroGroup group)
        => group == null ? null : DataStorage.GetFilePath(StorageId, GroupKey(group.Id));

    /// <summary>
    /// Load groups from disk and arm their triggers. Called once at startup after the
    /// overlay window (and thus the hotkey manager) exists; safe to call again.
    /// </summary>
    public static void Initialize()
    {
        EnsureLoaded();
        ArmTriggers();
    }

    /// <summary>
    /// Re-reads the groups folder and re-arms. For anything that writes a group file from outside
    /// this class — installing a shared file, removing one — since <see cref="EnsureLoaded"/> runs
    /// once and would otherwise not notice until the next launch.
    ///
    /// The in-memory groups are dropped, so callers holding a <see cref="MacroGroup"/> or a
    /// <see cref="Macro"/> across this call are holding a stale object: look it up again by id.
    /// Pending writes are flushed first, or a group saved moments ago would be re-read from the
    /// version before it.
    /// </summary>
    public static void Reload()
    {
        DataStorage.FlushAll();

        foreach (var macro in AllMacrosOrEmpty()) MacroRunner.Stop(macro);

        _groups = null;
        _toggleState.Clear();

        EnsureLoaded();
        ArmTriggers();
        Changed?.Invoke();
    }

    private static IEnumerable<Macro> AllMacrosOrEmpty()
        => _groups == null ? Enumerable.Empty<Macro>() : AllMacros();

    /// <summary>
    /// Adds a group that came from somewhere else — an installed bundle, a shared file — replacing
    /// any group with the same id. The group is written to its own file and everything is then
    /// re-read, because <see cref="EnsureLoaded"/> is where the normalisation an imported file needs
    /// lives: exactly one default group, globally unique slugs, and trigger migration. Returns the
    /// group as it ended up after that, which is not the object passed in.
    /// </summary>
    public static MacroGroup ImportGroup(MacroGroup group)
    {
        if (group == null || string.IsNullOrEmpty(group.Id)) return null;

        EnsureLoaded();

        // A shared file always arrives as a normal group; the receiving side keeps its own default.
        group.IsDefault = false;
        group.Macros ??= new List<Macro>();

        DataStorage.Save(StorageId, GroupKey(group.Id), group);
        Reload();

        return _groups.FirstOrDefault(g => g.Id == group.Id);
    }

    /// <summary>Create a macro in <paramref name="group"/> (the default group when null).</summary>
    public static Macro Add(string name, MacroGroup group = null)
    {
        EnsureLoaded();
        if (group == null || !_groups.Contains(group)) group = DefaultGroup;

        var macro = new Macro { Name = string.IsNullOrWhiteSpace(name) ? "New Macro" : name.Trim(), Simple = true };
        macro.Slug = UniqueSlug(macro.Name);
        group.Macros.Add(macro);

        SaveAndNotify(rearm: true);
        return macro;
    }

    // ── Step authoring (public API) ───────────────────────────────────────
    // The macro editor and any programmatic caller (e.g. the right-click "Add to
    // macro" / "Create hotkey" context menu) share these so a step built from code
    // is seeded exactly like one added through the UI's Add Step picker.

    /// <summary>The <see cref="MacroRegistry"/> method id that invokes this mod action.</summary>
    public static string MethodIdFor(ModActionDescriptor action)
        => $"{action.InvokeTarget.GetType().FullName}.{action.MethodName}";

    /// <summary>The <see cref="MacroRegistry"/> method id that writes this mod setting (the ".set" projection).</summary>
    public static string MethodIdFor(ModSettingDescriptor setting)
        => $"{setting.ValueTarget.GetType().FullName}.{setting.MemberName}.set";

    /// <summary>
    /// Build a step for <paramref name="methodId"/> with every parameter seeded to a
    /// sensible default source (typed default mode, expression, or the parameter's
    /// current value). Returns null when the id resolves to no registered method.
    /// </summary>
    public static MacroStep CreateStep(string methodId)
    {
        var desc = MacroRegistry.Find(methodId);
        if (desc == null) return null;

        var step = new MacroStep { MethodId = methodId };
        foreach (var p in desc.Parameters)
            step.SetArg(p.Name, DefaultSourceFor(p));
        return step;
    }

    /// <summary>
    /// Build a default step for <paramref name="methodId"/> and append it to the macro's
    /// on-steps (or off-steps when <paramref name="off"/>), persisting the edit. Returns the
    /// appended step, or null when the id is unknown.
    /// </summary>
    public static MacroStep AddStep(Macro macro, string methodId, bool off = false)
    {
        if (macro == null) return null;
        var step = CreateStep(methodId);
        if (step == null) return null;

        var steps = off ? (macro.OffSteps ??= new List<MacroStep>()) : (macro.Steps ??= new List<MacroStep>());
        steps.Add(step);
        NotifyEdited();
        return step;
    }

    /// <summary>Default value source for a macro parameter: expression, typed default mode, or its current/typed default value.</summary>
    public static ValueSource DefaultSourceFor(MacroParam param)
    {
        if (param.PrefersExpression)
            return new ExpressionValueSource { Text = "" };
        var macroType = MacroTypes.For(param.Type);
        if (macroType?.DefaultMode != null)
            return MakeTypedSource(macroType, macroType.DefaultMode.Id);
        return new ConstantValueSource
        {
            Value = param.CurrentValueGetter != null
                ? MacroValues.ToDisplay(param.CurrentValueGetter())
                : MacroValues.ToDisplay(MacroValues.DefaultFor(param.Type)),
        };
    }

    /// <summary>New typed-mode source; the mode's argument is seeded with the first live
    /// choice so what the dropdown shows is what actually runs.</summary>
    public static TypedModeValueSource MakeTypedSource(MacroTypeDescriptor macroType, string modeId)
    {
        var mode = macroType?.FindMode(modeId);
        string arg = null;
        if (mode?.Param != null && mode.Choices != null)
        {
            try { arg = mode.Choices()?.FirstOrDefault(); }
            catch { /* no game instance yet, leave empty */ }
        }
        return new TypedModeValueSource
        {
            TypeId = macroType?.Type.FullName ?? "",
            ModeId = modeId,
            Arg = arg,
        };
    }

    /// <summary>Insert a deep copy right after <paramref name="macro"/> in the same group, with
    /// fresh macro/step ids so hotkeys and future step references never collide.</summary>
    public static Macro Duplicate(Macro macro)
    {
        EnsureLoaded();
        var group = GroupOf(macro);
        if (group == null) return null;
        var index = group.Macros.IndexOf(macro);

        var clone = JsonConvert.DeserializeObject<Macro>(JsonConvert.SerializeObject(macro));
        clone.Id = Guid.NewGuid().ToString();
        clone.Name = macro.Name + " (copy)";
        clone.Slug = UniqueSlug(clone.Name);
        // Fresh step ids, with step-output references inside the clone remapped to
        // follow them (otherwise they'd keep pointing at the original macro's steps).
        var idMap = new Dictionary<string, string>();
        foreach (var step in clone.Steps.Concat(clone.OffSteps))
        {
            var oldId = step.Id;
            step.Id = Guid.NewGuid().ToString();
            idMap[oldId] = step.Id;
        }
        foreach (var step in clone.Steps.Concat(clone.OffSteps))
        {
            RemapStepIds(step.NamedArgs, idMap);
            RemapStepIds(step.ArgStash, idMap);
        }

        group.Macros.Insert(index + 1, clone);
        SaveAndNotify(rearm: true);
        return clone;
    }

    /// <summary>Point step-output sources at the new ids from <paramref name="idMap"/>;
    /// ids not in the map (e.g. a reference to a since-deleted step) stay untouched.</summary>
    private static void RemapStepIds(Dictionary<string, ValueSource> sources, Dictionary<string, string> idMap)
    {
        if (sources == null) return;
        foreach (var source in sources.Values)
            if (source is StepOutputValueSource so && so.StepId != null && idMap.TryGetValue(so.StepId, out var newId))
                so.StepId = newId;
    }

    public static void Remove(Macro macro)
    {
        EnsureLoaded();
        var group = GroupOf(macro);
        if (group == null || !group.Macros.Remove(macro)) return;
        MacroRunner.Stop(macro); // a deleted macro must not keep running
        _toggleState.Remove(macro.Id);
        SaveAndNotify(rearm: true);
    }

    public static void Rename(Macro macro, string name)
    {
        if (macro == null || string.IsNullOrWhiteSpace(name)) return;
        macro.Name = name.Trim();
        // No rearm: called per keystroke from the editor, and re-registering hotkeys
        // rewrites config entries. The hotkey display name catches up on the next rearm.
        SaveAndNotify(rearm: false);
    }

    // ── Group operations ──────────────────────────────────────────────────

    /// <summary>Create a new (non-default) group and persist it.</summary>
    public static MacroGroup AddGroup(string name)
    {
        EnsureLoaded();
        var order = _groups.Count == 0 ? 0 : _groups.Max(g => g.Order) + 1;
        var group = new MacroGroup
        {
            Name  = string.IsNullOrWhiteSpace(name) ? "New Group" : name.Trim(),
            Order = order,
        };
        _groups.Add(group);
        SortGroups();
        SaveAndNotify(rearm: false); // a fresh group has no macros/triggers yet
        return group;
    }

    /// <summary>Delete a non-default group and every macro in it (its file is removed too, so
    /// the shared copy is what survives). The default group is never removed.</summary>
    public static void RemoveGroup(MacroGroup group)
    {
        EnsureLoaded();
        if (group == null || group.IsDefault || !_groups.Contains(group)) return;

        foreach (var macro in group.Macros)
        {
            MacroRunner.Stop(macro);
            _toggleState.Remove(macro.Id);
        }
        _groups.Remove(group);
        DataStorage.Delete(StorageId, GroupKey(group.Id));
        SaveAndNotify(rearm: true);
    }

    public static void RenameGroup(MacroGroup group, string name)
    {
        if (group == null || string.IsNullOrWhiteSpace(name)) return;
        group.Name = name.Trim();
        SaveAndNotify(rearm: false);
    }

    /// <summary>Move a macro into another group (appended at the end). Triggers stay armed
    /// (they key on the macro id), so no rearm.</summary>
    public static void MoveMacro(Macro macro, MacroGroup target)
    {
        EnsureLoaded();
        if (macro == null || target == null || !_groups.Contains(target)) return;
        var from = GroupOf(macro);
        if (from == null || from == target) return;
        from.Macros.Remove(macro);
        target.Macros.Add(macro);
        SaveAndNotify(rearm: false);
    }

    /// <summary>Place <paramref name="source"/> into the gap just before (or after, when
    /// <paramref name="after"/>) <paramref name="target"/> in target's group. Powers drag-to-reorder
    /// within a group and drag-to-move across groups with the same gesture; triggers key on the
    /// macro id so no rearm is needed.</summary>
    public static void PlaceMacro(Macro source, Macro target, bool after)
    {
        EnsureLoaded();
        if (source == null || target == null || source == target) return;
        var to   = GroupOf(target);
        var from = GroupOf(source);
        if (to == null || from == null) return;

        from.Macros.Remove(source);
        var idx = to.Macros.IndexOf(target); // recomputed after the removal shifted indices
        if (idx < 0) to.Macros.Add(source);
        else         to.Macros.Insert(after ? idx + 1 : idx, source);
        SaveAndNotify(rearm: false);
    }

    /// <summary>Persist and broadcast an edit to steps or arguments. Does not touch triggers.</summary>
    public static void NotifyEdited() => SaveAndNotify(rearm: false);

    /// <summary>Persist and broadcast a change to a macro's trigger or enabled state; re-arms hotkeys.</summary>
    public static void NotifyTriggerChanged() => SaveAndNotify(rearm: true);

    private static void SaveAndNotify(bool rearm)
    {
        Save();
        if (rearm) ArmTriggers();
        Changed?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (_groups != null) return;
        _groups = new List<MacroGroup>();

        // One file per group; the file name is the group's identity.
        foreach (var id in DataStorage.ListKeys(StorageId, GroupsFolder))
        {
            var group = DataStorage.Load<MacroGroup>(StorageId, GroupKey(id));
            if (group == null) continue;
            group.Id = id;
            group.Macros ??= new List<Macro>();
            _groups.Add(group);
        }

        var changed = false;

        // First run, or an upgrade from the pre-groups single-file store: fold the legacy
        // macro list into a fresh default group.
        if (_groups.Count == 0)
        {
            var legacy = DataStorage.Load<List<Macro>>(StorageId, LegacyKey);
            var def = new MacroGroup { Name = DefaultGroupName, IsDefault = true, Order = 0 };
            if (legacy != null) def.Macros = legacy;
            _groups.Add(def);
            changed = true;
        }

        // Guarantee exactly one default group even across hand-edited / imported files.
        var defaults = _groups.Where(g => g.IsDefault).ToList();
        if (defaults.Count == 0)
        {
            (_groups.OrderBy(g => g.Order).First()).IsDefault = true;
            changed = true;
        }
        else if (defaults.Count > 1)
        {
            foreach (var extra in defaults.Skip(1)) extra.IsDefault = false;
            changed = true;
        }

        // Fold any pre-registry triggers (Type/Key/Mode fields) into the TypeId/Config shape.
        foreach (var macro in AllMacros())
            if (macro.Trigger.Migrate()) changed = true;

        // Global slug uniqueness across every group (files from before slugs, or two shared
        // files that happen to collide).
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var macro in AllMacros())
        {
            if (string.IsNullOrEmpty(macro.Slug) || !seen.Add(macro.Slug))
            {
                macro.Slug = UniqueSlug(macro.Name, seen);
                seen.Add(macro.Slug);
                changed = true;
            }
        }

        SortGroups();
        if (changed) Save();
    }

    /// <summary>Default group first, then by explicit order, then by name.</summary>
    private static void SortGroups()
        => _groups.Sort((a, b) =>
        {
            if (a.IsDefault != b.IsDefault) return a.IsDefault ? -1 : 1;
            var byOrder = a.Order.CompareTo(b.Order);
            return byOrder != 0 ? byOrder : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });

    // ── Slugs (stable call ids for Run Macro / expressions) ──────────────

    /// <summary>Resolve a macro reference (id, slug, or unique name, in that order)
    /// for the Run Macro step and expression strings. Null/empty resolves to null (an
    /// intentional "run nothing", which is how expression if-conditions skip the call);
    /// anything else that doesn't match throws with the available slugs listed.</summary>
    public static Macro FindByRef(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        EnsureLoaded();
        var r = reference.Trim();
        var all = AllMacros().ToList();

        var match = all.FirstOrDefault(m => m.Id == r)
                 ?? all.FirstOrDefault(m => string.Equals(m.Slug, r, StringComparison.OrdinalIgnoreCase));
        if (match != null) return match;

        var byName = all.Where(m => string.Equals(m.Name, r, StringComparison.OrdinalIgnoreCase)).ToList();
        if (byName.Count == 1) return byName[0];
        if (byName.Count > 1)
            throw new ArgumentException(
                $"Macro name '{r}' is ambiguous; use an id: {string.Join(", ", byName.Select(m => m.Slug))}");
        throw new ArgumentException(
            $"No macro '{r}'. Available: {string.Join(", ", all.Select(m => m.Slug))}");
    }

    /// <summary>Identifier-style slug from a display name ("Farm Loop!" → "farm_loop").</summary>
    public static string SlugFor(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in (name ?? "").Trim())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(char.ToLowerInvariant(ch));
            else if ((ch == ' ' || ch == '_' || ch == '-') && sb.Length > 0 && sb[sb.Length - 1] != '_')
                sb.Append('_');
        }
        var slug = sb.ToString().TrimEnd('_');
        if (slug.Length == 0) return "macro";
        return char.IsDigit(slug[0]) ? "m" + slug : slug;
    }

    private static string UniqueSlug(string name, HashSet<string> taken = null)
    {
        bool Taken(string s) => taken != null
            ? taken.Contains(s)
            : AllMacros().Any(m => string.Equals(m.Slug, s, StringComparison.OrdinalIgnoreCase));

        var baseSlug = SlugFor(name);
        if (!Taken(baseSlug)) return baseSlug;
        for (var i = 2; ; i++)
            if (!Taken($"{baseSlug}{i}"))
                return $"{baseSlug}{i}";
    }

    private static string GroupKey(string id) => $"{GroupsFolder}/{id}";

    /// <summary>Write every group to its own file. Removed groups' files are deleted at the
    /// point of removal, so they aren't recreated here.</summary>
    private static void Save()
    {
        foreach (var group in _groups)
            DataStorage.Save(StorageId, GroupKey(group.Id), group);
    }

    // ── Trigger arming ────────────────────────────────────────────────────

    private static void ArmTriggers()
    {
        // Nothing to arm against before the overlay window (and thus the hotkey manager and
        // coroutine host) exists; Initialize() arms again once it does.
        if (Plugin.Window == null) return;

        foreach (var handle in _armedHandles)
        {
            try { handle.Dispose(); }
            catch (Exception ex) { Plugin.LogSource.LogError($"[Macros] Trigger disarm failed: {ex}"); }
        }
        _armedHandles.Clear();

        foreach (var macro in AllMacros())
        {
            if (!macro.Enabled) continue;

            var descriptor = MacroTriggerRegistry.Find(macro.Trigger.TypeId);
            if (descriptor?.Arm == null) continue;

            try
            {
                var handle = descriptor.Arm(new MacroTriggerContext(macro));
                if (handle != null) _armedHandles.Add(handle);
            }
            catch (Exception ex)
            {
                Plugin.LogSource.LogError($"[Macros] Trigger '{descriptor.Id}' failed to arm macro '{macro.Name}': {ex}");
            }
        }
    }

    /// <summary>
    /// THE way to activate a macro: the hotkey trigger, the editor's play button and the
    /// Run Macro step (wait off) all come through here, so a macro always behaves the same
    /// no matter what fired it: Toggle-mode macros alternate their On and Off step lists
    /// via one shared toggle state, everything else runs the On steps.
    /// </summary>
    public static void Fire(Macro macro, IReadOnlyDictionary<string, object> triggerValues = null)
    {
        // Mirrors Run's own guards: a fire that would run nothing must not advance
        // the toggle state, or the next press would silently skip the Off list.
        if (macro == null || !macro.Enabled || MacroRunner.IsRootRunning(macro)) return;
        MacroRunner.Run(macro, offSteps: AdvanceToggle(macro), triggerValues: triggerValues);
    }

    /// <summary>Whether the next activation of <paramref name="macro"/> should run the Off
    /// list, advancing the shared toggle state (no-op false for non-Toggle macros). Callers
    /// that can't use <see cref="Fire"/> (the Run Macro step needs the routine itself) call
    /// this so their activations stay part of the same toggle sequence.</summary>
    public static bool AdvanceToggle(Macro macro)
    {
        if (macro == null || MacroTriggerRegistry.For(macro.Trigger)?.UsesOffList?.Invoke(macro.Trigger) != true)
            return false;
        _toggleState.TryGetValue(macro.Id, out var wasOn);
        _toggleState[macro.Id] = !wasOn;
        return wasOn;
    }
}
