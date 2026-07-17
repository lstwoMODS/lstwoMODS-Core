using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.Hotkeys;
using lstwoMODS_Core.Macros;
using lstwoMODS_Core.UI.Elements;
using Newtonsoft.Json;
using UnityEngine;

namespace lstwoMODS_Core.UI.TabMenus;

/// <summary>
/// Editor window for the macro system: a grid of fixed-width cards, one per macro,
/// with the whole editor on the card: editable name, Run/Duplicate/Delete, trigger
/// with a collapsible options group, and a collapsible steps list where each step
/// shows its method name, quick actions and collapsible parameter options.
///
/// Cards, step rows and picker entries are created and removed at runtime via
/// <see cref="OSWindow.AddElement(BaseUIElement, BaseUIElement, int)"/>  no pools,
/// no fixed caps. All edits persist automatically via <see cref="MacroManager"/>.
/// </summary>
public class MacrosWindow : BaseWindow
{
    private const float MinCardWidth = 380f;
    private const int StepNameMaxChars = 26;
    private const int MaxSearchResults = 50;

    /// <summary>Drag-and-drop payload type for a whole macro card (its id): drag the card's grip
    /// handle onto another card to reorder / move to that card's group, or onto an empty group.</summary>
    private const string MacroCardPayloadType = "macro-card";

    private static OSWindow Win => Plugin.Window;

    // ── Static skeleton ──
    private Container _groupsHost;
    private bool _uiBuilt;

    // ── New-macro modal (targets a specific group) ──
    private readonly Ref<string> _nameInput = new("");
    private Modal _newMacroModal;
    private InputText _nameInputBox;
    private string _newMacroTargetGroupId;

    // ── New-group modal ──
    private readonly Ref<string> _groupNameInput = new("");
    private Modal _newGroupModal;
    private InputText _groupNameBox;

    // ── Delete-group modal ──
    private Modal  _deleteGroupModal;
    private UIText _deleteGroupText;
    private string _pendingDeleteGroupId;

    // ── Move-to-group modal ──
    private Modal  _moveModal;
    private Combo  _moveCombo;
    private string _moveTargetMacroId;
    private readonly List<string> _moveGroupIds = new();

    // ── Delete-confirm modal ──
    private Modal  _deleteModal;
    private UIText _deleteModalText;
    private string _pendingDeleteId;

    // ── Simple-mode warning modal (advanced → simple with advanced features present) ──
    private Modal  _simpleModal;
    private UIText _simpleModalText;
    private string _pendingSimpleId;

    // ── Add-step picker (shared modal: search box + collapsible categories) ──
    private Modal     _addStepModal;
    private InputText _pickerFilterBox;
    private Container _pickerCatsWrap;
    private Container _pickerFlatWrap;
    private UIText    _pickerMoreHint;
    private string    _pickerFilter = "";
    private string    _pickerTargetId;
    private bool      _pickerOff;
    private bool      _pickerPicked;
    private string    _pickerSignature;
    private readonly List<BaseUIElement> _pickerCatNodes = new();
    private readonly List<BaseUIElement> _pickerFlatRows = new();

    // ── Dynamic per-group / per-macro UI ──
    private readonly List<GroupUI> _groups = new();

    private List<MacroMethodDescriptor> _methodDescs = new();
    private Dictionary<string, MacroMethodDescriptor> _methodById = new();

    /// <summary>One rendered group: a vertical block of title, action row, separator and a
    /// responsive grid of the group's macro cards.</summary>
    private sealed class GroupUI
    {
        public string GroupId;
        /// <summary>The whole block, added to <see cref="_groupsHost"/>.</summary>
        public Container Block;
        public InputText NameBox;
        public UIText CountText;
        public Button DeleteButton;   // absent (disabled) for the default group
        public FlowGrid Grid;
        /// <summary>Drop zone shown when the group is empty  lets a dragged macro land in an
        /// otherwise card-less group. Wraps <see cref="EmptyHint"/>.</summary>
        public BaseUIElement EmptyDrop;
        public UIText EmptyHint;
        public readonly List<CardUI> Cards = new();
    }

    // ── Hotkey bind modal (press-the-keys capture) ──
    private Modal      _bindModal;
    private KeyCapture _bindCapture;
    private UIText     _bindHint;
    private string     _bindTargetId;
    private string     _bindConfigKey;
    private ImGuiKey        _bindKey;
    private HotkeyModifiers _bindMods;

    private sealed class CardUI
    {
        public string MacroId;
        /// <summary>The element added to the group's grid: a drop-target wrapper around the
        /// card's ChildWindow body (so the whole card accepts a dragged macro).</summary>
        public BaseUIElement Slot;
        public InputText NameBox;
        public UIText SlugText;
        public Button RunButton;
        public Checkbox EnabledBox;
        public Checkbox AdvancedBox;
        public Combo TriggerType;
        public CollapsingHeader TriggerOpts;
        /// <summary>Holds the selected trigger's config field rows, rebuilt when the trigger kind changes.</summary>
        public Container TriggerConfigHost;
        /// <summary>Id of the trigger descriptor <see cref="TriggerFields"/> was last built for.</summary>
        public string TriggerDescId;
        public readonly List<TriggerFieldUI> TriggerFields = new();
        /// <summary>Read-only listing of the trigger's declared outputs (the expression variable
        /// names it provides); null when the trigger declares none. Rebuilt when the resolved output
        /// set changes  which, for a trigger with dynamic outputs, is on config edits too.</summary>
        public BaseUIElement TriggerOutputsBlock;
        /// <summary>Signature of the outputs <see cref="TriggerOutputsBlock"/> was last built for, so
        /// a config change that alters the outputs (dynamic-output triggers) rebuilds the block.</summary>
        public string TriggerOutputsSig;
        public Container ListRow;
        public Combo StepListCombo;
        public Container StepsHost;
        public bool EditingOff;
        public readonly List<StepUI> Steps = new();
    }

    /// <summary>Editor for one config field of a trigger (see <see cref="MacroTriggerParam"/>):
    /// a keybind button, a registered macro type's mode picker, or one typed value widget picked
    /// from the field's type.</summary>
    private sealed class TriggerFieldUI
    {
        public string MacroId;
        /// <summary>Shared id fragment for this field's elements (macro + trigger + key), so
        /// refresh can reconstruct the keybind button's <c>###</c> id.</summary>
        public string Slug;
        public MacroTriggerParam Param;
        /// <summary>The whole label + editor row, added to the card's <see cref="CardUI.TriggerConfigHost"/>.</summary>
        public BaseUIElement Row;
        public Button    BindButton;   // Keybind widget
        public Checkbox  BoolBox;
        public Combo     EnumCombo;
        public string[]  EnumNames;
        public DragInt   IntBox;
        public DragFloat FloatBox;
        public InputText Input;

        // Macro-type field (Player, ...): the type's modes as a dropdown, plus each mode's own
        // argument editor  the same shape a step parameter of the type gets.
        public MacroTypeDescriptor MacroType;
        public Combo ModeCombo;
        /// <summary>Mode id per <see cref="ModeCombo"/> entry; "" for the EmptyLabel entry.</summary>
        public string[] ModeIds;
        public readonly Dictionary<string, TypedEditorUI> TypedEditors = new();
    }

    private sealed class StepUI
    {
        public string StepId;
        /// <summary>The step's card: a compact tree row (name, drag handle, X to delete)
        /// with the parameters and quick actions inside.</summary>
        public TreeNode Header;
        /// <summary>Present when the method returns a value: names the output for expressions.</summary>
        public InputText OutputNameBox;
        /// <summary>The whole "output" row (label + <see cref="OutputNameBox"/>); hidden in simple mode.</summary>
        public BaseUIElement OutputRow;
        /// <summary>Rows for the step's context/target params (e.g. Player); hidden in simple mode.</summary>
        public readonly List<BaseUIElement> ContextRows = new();
        public readonly List<ParamUI> Params = new();

        /// <summary>Set for a step whose method supplies its own editor (e.g. Switch); when present,
        /// the param/context/output rows are replaced by the editor's widgets.</summary>
        public MacroStepEditor CustomEditor;
        public MacroStepEditorContext EditorCtx;
    }

    /// <summary>Editor for one mode of a registered macro type (e.g. Player "By Name"):
    /// a live-choices combo or a free text input for the mode's argument.</summary>
    private sealed class TypedEditorUI
    {
        public MacroTypeMode Mode;
        public Container Wrap;
        public InputText Input;                       // when the mode has no Choices
        public Combo Choices;                         // when it does
        public readonly List<string> Items = new();   // current choices, parallel to combo items
    }

    /// <summary>One parameter row: a mode combo (Value/Toggle/Step/Expr, or a macro type's
    /// modes for registered types) plus one editor per mode, of which exactly one is visible.</summary>
    private sealed class ParamUI
    {
        /// <summary>Display labels for the mode combo.</summary>
        public string[] Modes;
        /// <summary>Semantic keys parallel to <see cref="Modes"/>: "Value", "Toggle",
        /// "Step", "Expr", or "typed:{modeId}". Also used as arg-stash keys.</summary>
        public string[] ModeKeys;
        public MacroTypeDescriptor MacroType;          // non-null for registered types
        public readonly Dictionary<string, TypedEditorUI> TypedEditors = new();
        public Combo ModeCombo;
        public Container ValueWrap;
        // Typed Value-mode editor: exactly one is non-null, picked from the param type.
        public InputText Input;          // string / fallback
        public Checkbox BoolBox;
        public Combo EnumCombo;
        public string[] EnumNames;
        public DragInt IntBox;
        public DragFloat FloatBox;
        public DragFloat2 Vec2Box;
        public DragFloat3 Vec3Box;
        public ColorEdit4 ColorBox;
        public Combo StepCombo;          // Step output
        public Container StepWrap;
        public InputText ExprInput;      // Expression
        public Container ExprWrap;
        public TextWrapped ExprError;
        /// <summary>Self-call warning; only built for Macro-typed params (Run Macro).</summary>
        public TextWrapped MacroHint;
        /// <summary>Producer step ids, parallel to StepCombo's items.</summary>
        public readonly List<string> StepChoiceIds = new();
    }

    public MacrosWindow()
    {
        Name = "Macros";
        TitleIcon = Lucide.Workflow;
    }

    public override Group ConstructUI()
    {
        // There is no window teardown hook to unsubscribe in; -= first keeps a rebuild idempotent.
        MacroManager.Changed -= RefreshGroups;
        MacroManager.Changed += RefreshGroups;
        MacroRunner.RunningChanged -= OnRunningChanged;
        MacroRunner.RunningChanged += OnRunningChanged;

        _nameInputBox = new InputText("##MacrosWindow-name", hint: "Macro name...", maxLength: 64)
            .WithValue(_nameInput)
            .WatchKeys(ImGuiKey.Enter, ImGuiKey.KeypadEnter, ImGuiKey.Escape)
            .OnKey(OnNameInputKey)
            .WithItemWidth(260f);

        _newMacroModal = new Modal("MacrosWindow-new-modal", "New Macro",
            _nameInputBox,
            new Spacing("MacrosWindow-new-sp"),
            new Button("Create", CreateMacroFromInput).WithItemWidth(80f),
            new SameLine("MacrosWindow-new-sl"),
            new Button("Cancel", () => _newMacroModal.Close()).WithItemWidth(80f)
        );

        _groupNameBox = new InputText("##MacrosWindow-gname", hint: "Group name...", maxLength: 48)
            .WithValue(_groupNameInput)
            .WatchKeys(ImGuiKey.Enter, ImGuiKey.KeypadEnter, ImGuiKey.Escape)
            .OnKey(OnGroupNameInputKey)
            .WithItemWidth(260f);
        _newGroupModal = new Modal("MacrosWindow-newgroup-modal", "New Group",
            _groupNameBox,
            new Spacing("MacrosWindow-newgroup-sp"),
            new Button("Create##MacrosWindow-newgroup-ok", CreateGroupFromInput).WithItemWidth(80f),
            new SameLine("MacrosWindow-newgroup-sl"),
            new Button("Cancel##MacrosWindow-newgroup-no", () => _newGroupModal.Close()).WithItemWidth(80f)
        );

        _deleteGroupText = new UIText("MacrosWindow-delgroup-text", "");
        _deleteGroupModal = new Modal("MacrosWindow-delgroup-modal", "Delete Group",
            _deleteGroupText,
            new Spacing("MacrosWindow-delgroup-sp"),
            new Button("Delete##MacrosWindow-delgroup-yes", DeletePendingGroup).WithItemWidth(80f),
            new SameLine("MacrosWindow-delgroup-sl"),
            new Button("Cancel##MacrosWindow-delgroup-no", () => _deleteGroupModal.Close()).WithItemWidth(80f)
        );

        _moveCombo = new Combo("##MacrosWindow-move-combo", Array.Empty<string>(), 0).WithItemWidth(240f);
        _moveModal = new Modal("MacrosWindow-move-modal", "Move to Group",
            new UIText("MacrosWindow-move-label", "Move this macro to:"),
            _moveCombo,
            new Spacing("MacrosWindow-move-sp"),
            new Button("Move##MacrosWindow-move-ok", ConfirmMove).WithItemWidth(80f),
            new SameLine("MacrosWindow-move-sl"),
            new Button("Cancel##MacrosWindow-move-no", () => _moveModal.Close()).WithItemWidth(80f)
        );

        _deleteModalText = new UIText("MacrosWindow-del-text", "");
        _deleteModal = new Modal("MacrosWindow-del-modal", "Delete Macro",
            _deleteModalText,
            new Spacing("MacrosWindow-del-sp"),
            new Button("Delete##MacrosWindow-del-yes", DeletePendingMacro).WithItemWidth(80f),
            new SameLine("MacrosWindow-del-sl"),
            new Button("Cancel##MacrosWindow-del-no", () => _deleteModal.Close()).WithItemWidth(80f)
        );

        _simpleModalText = new UIText("MacrosWindow-simple-text", "");
        _simpleModal = new Modal("MacrosWindow-simple-modal", "Switch to Simple Mode?",
            _simpleModalText,
            new Spacing("MacrosWindow-simple-sp"),
            new Button("Switch##MacrosWindow-simple-yes", ConfirmSwitchToSimple).WithItemWidth(90f),
            new SameLine("MacrosWindow-simple-sl"),
            new Button("Cancel##MacrosWindow-simple-no", CancelSwitchToSimple).WithItemWidth(80f)
        ).OnClose(RevertAdvancedCheckbox);

        _pickerFilterBox = new InputText("##MacrosWindow-pick-filter", hint: "Search methods...", maxLength: 64,
                onChanged: v => { _pickerFilter = v ?? ""; RefreshPickerFilter(); })
            .WatchKeys(ImGuiKey.Enter, ImGuiKey.KeypadEnter, ImGuiKey.Escape)
            .OnKey(OnPickerFilterKey)
            .WithItemWidth(-1f);

        _pickerCatsWrap = new Container("MacrosWindow-pick-cats");
        _pickerFlatWrap = new Container("MacrosWindow-pick-flat");
        _pickerFlatWrap.Data.Enabled = false;
        _pickerMoreHint = new UIText("MacrosWindow-pick-more", "");
        _pickerMoreHint.Data.Enabled = false;

        _addStepModal = new Modal("MacrosWindow-pick-modal", "Add Step",
            _pickerFilterBox,
            new ChildWindow("MacrosWindow-pick-scroll", 0f, 0f, _pickerFlatWrap, _pickerMoreHint, _pickerCatsWrap)
                .WithFlags(ImGuiChildFlags.Borders)
                .WithFooterReserve(),
            new Button("Cancel##MacrosWindow-pick-cancel", () => _addStepModal.Close()).WithItemWidth(80f)
        ).WithSize(400f, 420f);

        _bindCapture = new KeyCapture("MacrosWindow-bind-capture", OnBindCaptured);
        _bindHint = new UIText("MacrosWindow-bind-hint", "Press the key combination for this macro.");
        _bindModal = new Modal("MacrosWindow-bind-modal", "Bind Hotkey",
                _bindHint,
                new Spacing("MacrosWindow-bind-sp0"),
                _bindCapture,
                new Spacing("MacrosWindow-bind-sp1"),
                new Button("Confirm##MacrosWindow-bind-ok", ConfirmBind).WithItemWidth(80f),
                new SameLine("MacrosWindow-bind-sl"),
                new Button("Cancel##MacrosWindow-bind-no", CancelBind).WithItemWidth(80f))
            .OnClose(() => _bindCapture.Stop());

        _groupsHost = new Container("MacrosWindow-groups");

        _uiBuilt = true;
        MainThread.Enqueue(RefreshGroups);

        return new Group("MacrosWindow",
            _newMacroModal,
            _newGroupModal,
            _deleteGroupModal,
            _moveModal,
            _deleteModal,
            _simpleModal,
            _addStepModal,
            _bindModal,
            new Button($"{Lucide.FolderPlus} New Group", OpenNewGroupModal)
                .WithTooltip("Groups organize macros and each saves to its own file you can share."),
            new Separator("MacrosWindow-sep"),
            _groupsHost);
    }

    public override void RefreshUI()
    {
        RefreshGroups();
    }


    /// <summary>Reconcile the rendered group blocks (and the cards inside each) with the
    /// current group list. Groups render as a vertical list of blocks in
    /// <see cref="_groupsHost"/>; each block owns its own card <see cref="FlowGrid"/>.</summary>
    private void RefreshGroups()
    {
        if (!_uiBuilt || Win == null) return;

        RefreshMethodRegistry();

        var groups = MacroManager.Groups;

        // ── Structural: remove blocks for gone groups ──
        for (var i = _groups.Count - 1; i >= 0; i--)
        {
            if (groups.All(g => g.Id != _groups[i].GroupId))
            {
                foreach (var card in _groups[i].Cards) Win.RemoveElement(card.Slot);
                Win.RemoveElement(_groups[i].Block);
                _groups.RemoveAt(i);
            }
        }

        // ── Structural: insert / reorder blocks so _groups matches the group order ──
        for (var i = 0; i < groups.Count; i++)
        {
            if (i < _groups.Count && _groups[i].GroupId == groups[i].Id) continue;

            // Already built but out of position: rebuild rather than re-add the same block
            // instance  re-adding an element id in the same frame it's removed makes the overlay
            // skip the create and apply the remove, vanishing it (same hazard as the cards below).
            var existing = _groups.FirstOrDefault(g => g.GroupId == groups[i].Id);
            if (existing != null)
            {
                foreach (var card in existing.Cards) Win.RemoveElement(card.Slot);
                Win.RemoveElement(existing.Block);
                _groups.Remove(existing);
            }

            var groupUI = BuildGroupBlock(groups[i]);
            Win.AddElement(groupUI.Block, _groupsHost, i);
            _groups.Insert(i, groupUI);
        }

        // ── Cards: remove-all stale first, then add, so a macro moving between groups never
        // has its card (same element id) added under the new group before the old group drops
        // it  otherwise a same-frame add-then-remove would erase it. ──
        foreach (var groupUI in _groups)
        {
            var group = groups.First(g => g.Id == groupUI.GroupId);
            for (var i = groupUI.Cards.Count - 1; i >= 0; i--)
                if (group.Macros.All(m => m.Id != groupUI.Cards[i].MacroId))
                {
                    Win.RemoveElement(groupUI.Cards[i].Slot);
                    groupUI.Cards.RemoveAt(i);
                }
        }

        for (var gi = 0; gi < _groups.Count; gi++)
        {
            var groupUI = _groups[gi];
            var group = groups[gi];
            RefreshGroupHeader(groupUI, group);

            var macros = group.Macros;
            for (var i = 0; i < macros.Count; i++)
            {
                if (i < groupUI.Cards.Count && groupUI.Cards[i].MacroId == macros[i].Id) continue;

                // Card already built but out of position (a drag reordered it). It must be
                // rebuilt, NOT re-added: re-adding the same element instance/id in the same frame
                // it's removed makes the overlay skip the create (id still registered) and then
                // apply the remove, so the card would vanish. A fresh instance gets a fresh id.
                var existing = groupUI.Cards.FindIndex(c => c.MacroId == macros[i].Id);
                if (existing >= 0)
                {
                    Win.RemoveElement(groupUI.Cards[existing].Slot);
                    groupUI.Cards.RemoveAt(existing);
                }

                var card = BuildCard(macros[i]);
                Win.AddElement(card.Slot, groupUI.Grid, i);
                groupUI.Cards.Insert(i, card);
            }

            for (var i = 0; i < groupUI.Cards.Count; i++)
                RefreshCard(groupUI.Cards[i], macros[i]);
        }
    }

    private Macro FindMacro(string id) => MacroManager.Macros.FirstOrDefault(m => m.Id == id);

    private CardUI FindCard(string macroId)
        => _groups.SelectMany(g => g.Cards).FirstOrDefault(c => c.MacroId == macroId);

    /// <summary>Build one group's vertical block: title row, action row, separator, then the
    /// responsive grid of its macro cards (with an empty hint shown when it holds none).</summary>
    private GroupUI BuildGroupBlock(MacroGroup group)
    {
        var gid = group.Id;
        var groupUI = new GroupUI { GroupId = gid };

        // The editable name box doubles as the group title.
        groupUI.NameBox = new InputText($"##Mg-{gid}-name", maxLength: 48, onChanged: v => OnGroupNameEdited(gid, v))
            .WithItemWidth(240f)
            .WithTooltip("Group name. This group saves to its own file you can share with others.");
        groupUI.CountText = new UIText($"Mg-{gid}-count", "")
            .WithStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f);

        // Title (icon + editable name + count) leads; the action buttons pin to the right so
        // the header is one line, wrapping the buttons below only when the block gets too narrow.
        var header = new PinRow($"Mg-{gid}-header",
            new AlignText($"Mg-{gid}-icon-al"), new UIText($"Mg-{gid}-icon", Lucide.Folder),
            new SameLine($"Mg-{gid}-tsl0"), groupUI.NameBox,
            new SameLine($"Mg-{gid}-tsl1"), new AlignText($"Mg-{gid}-count-al"), groupUI.CountText);

        var actions = new List<BaseUIElement>
        {
            new Button($"{Lucide.Plus} New Macro##Mg-{gid}", () => OpenNewMacroModal(gid))
                .WithTooltip("Create a new macro in this group"),
        };
        if (!group.IsDefault)
        {
            groupUI.DeleteButton = new Button($"{Lucide.Trash2} Delete Group##Mg-{gid}", () => OpenDeleteGroupModal(gid))
                .WithTooltip("Delete this group and every macro in it");
            actions.Add(groupUI.DeleteButton);
        }
        header.WithTrailing(actions.ToArray());

        // The grid's tail fills the empty space to the right of the last row's last card, so a
        // macro dragged into that space lands after the last macro of the group.
        groupUI.Grid = new FlowGrid($"Mg-{gid}-grid", MinCardWidth)
            .WithTail(new DragTarget($"Mg-{gid}-tail-drop",
                (_, payload) => OnTailDrop(gid, payload),
                new[] { MacroCardPayloadType },
                new InvisibleButton($"Mg-{gid}-tail-btn", 1f, 1f)));
        groupUI.EmptyHint = new UIText($"Mg-{gid}-empty", "No macros in this group yet.  Drop a macro here to move it in.")
            .WithStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f);
        // The empty hint (plus a little padding) is a drop zone so a dragged macro can move into
        // a group that has no cards to drop onto.
        groupUI.EmptyDrop = new DragTarget($"Mg-{gid}-empty-drop",
            (_, payload) => OnGroupDrop(gid, payload),
            new[] { MacroCardPayloadType },
            groupUI.EmptyHint,
            new Dummy($"Mg-{gid}-empty-pad", MinCardWidth, 8f));

        groupUI.Block = new Container($"Mg-{gid}-block",
            header,
            new Separator($"Mg-{gid}-sep"),
            groupUI.EmptyDrop,
            groupUI.Grid,
            new Spacing($"Mg-{gid}-tail0"),
            new Spacing($"Mg-{gid}-tail1"));

        return groupUI;
    }

    private void RefreshGroupHeader(GroupUI groupUI, MacroGroup group)
    {
        if (!groupUI.NameBox.IsFocused && groupUI.NameBox.Value != group.Name)
            groupUI.NameBox.Value = group.Name;
        var n = group.Macros.Count;
        SetText(groupUI.CountText, n == 1 ? "1 macro" : $"{n} macros");
        ShowIf(groupUI.EmptyDrop, n == 0);
    }

    private static List<MacroStep> StepsOf(Macro macro, bool off)
    {
        if (off) return macro.OffSteps ??= new List<MacroStep>();
        return macro.Steps ??= new List<MacroStep>();
    }


    private CardUI BuildCard(Macro macro)
    {
        var id = macro.Id;
        var card = new CardUI { MacroId = id };

        card.NameBox = new InputText($"##Mc-{id}-name", maxLength: 64, onChanged: v => OnNameEdited(id, v))
            .WithItemWidth(295f);
        card.SlugText = new UIText($"Mc-{id}-slug", "id: ")
            .WithStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f)
            .WithTooltip("Call id for the Run Macro step and expressions.\nStays the same when the macro is renamed.");
        card.EnabledBox = new Checkbox($"On##Mc-{id}", onChanged: v => OnEnabledChanged(id, v))
            .WithTooltip("Enable / disable this macro");
        card.AdvancedBox = new Checkbox($"Advanced##Mc-{id}", onChanged: v => OnAdvancedToggled(id, v))
            .WithTooltip("Advanced mode: per-parameter value modes (Toggle, Step output, Expression),\n"
                       + "targets, and named outputs.\n"
                       + "Simple mode hides all of that  every parameter is just a value to set,\n"
                       + "the quick path for keybind macros.");

        card.TriggerType = new Combo($"##Mc-{id}-trigger", TriggerLabels(),
            onChanged: idx => OnTriggerTypeChanged(id, idx)).WithItemWidth(140f);

        // The config field rows are built into this host per trigger kind by RebuildTriggerConfig.
        card.TriggerConfigHost = new Container($"Mc-{id}-trigcfg-host");
        card.TriggerOpts = new CollapsingHeader($"Mc-{id}-trigopts", "Trigger Options",
            card.TriggerConfigHost);

        card.StepListCombo = new Combo($"##Mc-{id}-steplist", new[] { "On", "Off" },
            onChanged: idx => OnStepListChanged(id, idx)).WithItemWidth(80f);
        card.ListRow = new Container($"Mc-{id}-listrow",
            new UIText($"Mc-{id}-list-label", "List"),
            new SameLine($"Mc-{id}-lr-sl"), card.StepListCombo);

        card.StepsHost = new Container($"Mc-{id}-steps-host");

        var stepsHeader = new CollapsingHeader($"Mc-{id}-steps", "Steps",
                card.ListRow,
                card.StepsHost,
                new Button($"{Lucide.Plus} Add Step##Mc-{id}", () => OpenAddStepPicker(id)))
            .DefaultOpen();

        // Width comes from the FlowGrid cell; 0 = fill (fallback outside a grid).
        card.RunButton = new Button($"{Lucide.Play}###Mc-{id}-run", () => OnRunClicked(id)).WithTooltip("Run");

        // Grip handle: drag it to reorder within the group or drop onto a card in / an empty
        // other group to move it there. Drag preview shows the macro name.
        var grip = new DragSource($"Mc-{id}-grip", MacroCardPayloadType, id,
                new AlignText($"Mc-{id}-grip-al"),
                new UIText($"Mc-{id}-grip-icon", Lucide.GripVertical)
                    .WithStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f)
                    .WithTooltip("Drag to reorder, or onto another group to move it"))
            .WithDisplayLabel(macro.Name);

        var body = new ChildWindow($"Mc-{id}-card", 0f, 0f,
                grip,
                new SameLine($"Mc-{id}-grip-sl"), card.NameBox,
                new SameLine($"Mc-{id}-sl0"), card.EnabledBox,
                card.RunButton,
                new SameLine($"Mc-{id}-sl1"), new Button($"{Lucide.Copy}##Mc-{id}", () => DuplicateMacro(id)).WithTooltip("Duplicate"),
                new SameLine($"Mc-{id}-sl-mv"), new Button($"{Lucide.FolderInput}##Mc-{id}", () => OpenMoveModal(id)).WithTooltip("Move to group"),
                new SameLine($"Mc-{id}-sl2"), new Button($"{Lucide.Trash2}##Mc-{id}", () => OpenDeleteModal(id)).WithTooltip("Delete"),
                card.SlugText,
                new SameLine($"Mc-{id}-sl-adv"), card.AdvancedBox,
                new Spacing($"Mc-{id}-sp0"),
                new UIText($"Mc-{id}-trigger-label", "Trigger"),
                new SameLine($"Mc-{id}-sl3"), card.TriggerType,
                card.TriggerOpts,
                stepsHeader)
            .WithFlags(ImGuiChildFlags.Borders | ImGuiChildFlags.AutoResizeY | ImGuiChildFlags.AlwaysUseWindowPadding);

        // The card is an insert-between drop target: a dragged card lands in the gap before or
        // after this one (whichever half the cursor is over), shown by a vertical insertion line,
        // so one gesture both reorders within a group and moves across groups.
        card.Slot = new DragTarget($"Mc-{id}-drop",
                null, new[] { MacroCardPayloadType }, body)
            .WithInsertBetween((_, payload, after) => OnCardDrop(id, payload, after));
        return card;
    }

    private void RefreshCard(CardUI card, Macro macro)
    {
        if (!card.NameBox.IsFocused && card.NameBox.Value != macro.Name)
            card.NameBox.Value = macro.Name;
        SetText(card.SlugText, $"id: {macro.Slug}");
        SetCheck(card.EnabledBox, macro.Enabled);
        SetCheck(card.AdvancedBox, !macro.Simple);
        RefreshRunButton(card, macro);

        var descriptors = MacroTriggerRegistry.All;
        var descriptor = MacroTriggerRegistry.For(macro.Trigger);
        var descIndex = 0;
        for (var i = 0; i < descriptors.Count; i++)
            if (descriptors[i].Id == descriptor.Id) { descIndex = i; break; }
        SetCombo(card.TriggerType, descIndex);

        // Rebuild the config rows when the trigger kind changes (its fields differ).
        if (card.TriggerDescId != descriptor.Id) RebuildTriggerConfig(card, descriptor);
        foreach (var field in card.TriggerFields)
            RefreshTriggerField(field, macro.Trigger);

        // The outputs may depend on config (dynamic-output triggers), so resolve and rebuild the
        // "Provides" block whenever its content changes  after the fields, which may have just
        // written the config value the outputs read.
        var resolvedOutputs = descriptor.ResolveOutputs(macro.Trigger);
        RefreshTriggerOutputs(card, descriptor, resolvedOutputs);
        ShowIf(card.TriggerOpts, descriptor.Params.Length > 0 || resolvedOutputs.Length > 0);

        var hasOffList = descriptor.UsesOffList?.Invoke(macro.Trigger) == true;
        if (!hasOffList) card.EditingOff = false;
        ShowIf(card.ListRow, hasOffList);
        SetCombo(card.StepListCombo, card.EditingOff ? 1 : 0);

        RefreshSteps(card, macro);
    }


    private void RefreshSteps(CardUI card, Macro macro)
    {
        var steps = StepsOf(macro, card.EditingOff);

        var structureChanged = card.Steps.Count != steps.Count;
        if (!structureChanged)
            for (var j = 0; j < steps.Count; j++)
                if (card.Steps[j].StepId != steps[j].Id) { structureChanged = true; break; }

        if (structureChanged)
        {
            foreach (var stepUI in card.Steps)
            {
                stepUI.CustomEditor?.Teardown(stepUI.EditorCtx);
                Win.RemoveElement(stepUI.Header);
            }
            card.Steps.Clear();

            for (var j = 0; j < steps.Count; j++)
            {
                var stepUI = BuildStepRow(card, steps, j);
                Win.AddElement(stepUI.Header, card.StepsHost, j);
                card.Steps.Add(stepUI);
            }
        }

        for (var j = 0; j < steps.Count; j++)
            RefreshStepRow(card, card.Steps[j], steps, j, macro.Simple);
    }

    private const string StepPayloadType = "macro-step";

    private StepUI BuildStepRow(CardUI card, List<MacroStep> steps, int index)
    {
        var step = steps[index];
        var macroId = card.MacroId;
        var stepId = step.Id;
        var stepUI = new StepUI { StepId = stepId };

        var body = new List<BaseUIElement>();

        _methodById.TryGetValue(step.MethodId ?? "", out var desc);
        if (desc?.CustomEditor != null)
        {
            // A method with its own editor (Switch, plugin custom steps) renders its widgets in
            // place of the auto-built parameter rows. The context is reused across refreshes and
            // torn down with the step row.
            stepUI.CustomEditor = desc.CustomEditor;
            stepUI.EditorCtx = new MacroStepEditorContext(
                FindMacro(macroId), step, $"Mc-{macroId}-s{stepId}", MacroManager.NotifyEdited);
            body.AddRange(desc.CustomEditor.Build(stepUI.EditorCtx));
        }
        else if (desc != null)
        {
            step.MigrateLegacyArgs(desc);

            BaseUIElement outputRow = null;
            if (HasOutput(desc))
            {
                stepUI.OutputNameBox = new InputText($"##Mc-{macroId}-s{stepId}-outname",
                        hint: "name...", maxLength: 32,
                        onChanged: v => OnOutputNameChanged(macroId, stepId, v))
                    .WithItemWidth(140f)
                    .WithTooltip("Name this step's return value to use it in later\nsteps' expressions (letters, digits, _).\nFunction names (min, time, ...) get an underscore appended.");
                outputRow = new Container($"Mc-{macroId}-s{stepId}-outrow",
                    new UIText($"Mc-{macroId}-s{stepId}-outlabel", "output"),
                    new SameLine($"Mc-{macroId}-s{stepId}-outsl"), stepUI.OutputNameBox);
            }

            // Context params (who/what the step applies to) read best above everything
            // else, including the output row; the method's own params follow it.
            var contextRows = new List<BaseUIElement>();
            var paramRows = new List<BaseUIElement>();

            // Producer choices for Step mode: earlier steps that return a value.
            var choices = new List<(string Id, string Label)>();
            for (var p = 0; p < index; p++)
            {
                _methodById.TryGetValue(steps[p].MethodId ?? "", out var pdesc);
                if (pdesc == null || !HasOutput(pdesc)) continue;
                choices.Add((steps[p].Id, $"#{p + 1} {pdesc.Label}"));
            }

            for (var k = 0; k < desc.Parameters.Length; k++)
            {
                var param = desc.Parameters[k];
                var pk = k;
                var source = step.GetArg(param.Name);
                if (source == null)
                {
                    // Same seeding as PickMethod; covers params a mod update (or the
                    // context system) added to a method after the step was saved.
                    source = DefaultSourceFor(param);
                    step.SetArg(param.Name, source);
                }

                // A loaded Step reference whose producer is gone (or now runs later)
                // still needs a visible dropdown entry.
                var paramChoices = new List<(string Id, string Label)>(choices);
                if (source is StepOutputValueSource so && !string.IsNullOrEmpty(so.StepId)
                    && paramChoices.All(c => c.Id != so.StepId))
                    paramChoices.Add((so.StepId, "(missing step)"));

                var macroType = MacroTypes.For(param.Type);
                var modes = new List<string>();
                var modeKeys = new List<string>();
                if (macroType != null)
                {
                    // Registered object type: its own selection modes replace Value/Toggle.
                    foreach (var m in macroType.Modes)
                    {
                        modes.Add(m.Label);
                        modeKeys.Add($"typed:{m.Id}");
                    }
                }
                else
                {
                    modes.Add("Value"); modeKeys.Add("Value");
                    // bool? counts too; BuildValueEditor unwraps nullables the same way.
                    if (UnwrapNullable(param.Type) == typeof(bool) && param.CurrentValueGetter != null)
                    {
                        modes.Add("Toggle"); modeKeys.Add("Toggle");
                    }
                }
                if (paramChoices.Count > 0) { modes.Add("Step"); modeKeys.Add("Step"); }
                modes.Add("Expr"); modeKeys.Add("Expr");

                var paramUI = new ParamUI
                {
                    Modes = modes.ToArray(),
                    ModeKeys = modeKeys.ToArray(),
                    MacroType = macroType,
                };
                paramUI.StepChoiceIds.AddRange(paramChoices.Select(c => c.Id));

                paramUI.ModeCombo = new Combo($"##Mc-{macroId}-s{stepId}-p{k}-mode", paramUI.Modes, 0,
                    i => OnParamModeChanged(macroId, stepId, pk, i)).WithItemWidth(150f);

                // Label + mode combo share the first line; the active editor gets its own
                // full-width line below.
                if (macroType == null)
                    paramUI.ValueWrap = new Container($"Mc-{macroId}-s{stepId}-p{k}-vwrap",
                        BuildValueEditor(paramUI, param, macroId, stepId, pk));

                foreach (var m in macroType?.Modes.Where(m => m.Param != null) ?? Enumerable.Empty<MacroTypeMode>())
                {
                    var mode = m;
                    var ed = new TypedEditorUI { Mode = mode };
                    if (mode.Choices != null)
                    {
                        ed.Choices = new Combo($"##Mc-{macroId}-s{stepId}-p{k}-t{mode.Id}", Array.Empty<string>(), 0,
                            i => OnParamTypedArgPicked(macroId, stepId, pk, ed, i)).WithItemWidth(-1f);
                        ed.Wrap = new Container($"Mc-{macroId}-s{stepId}-p{k}-t{mode.Id}-wrap", ed.Choices);
                    }
                    else
                    {
                        ed.Input = new InputText($"##Mc-{macroId}-s{stepId}-p{k}-t{mode.Id}",
                            onChanged: v => OnParamTypedArgChanged(macroId, stepId, pk, v)).WithItemWidth(-1f);
                        ed.Wrap = new Container($"Mc-{macroId}-s{stepId}-p{k}-t{mode.Id}-wrap", ed.Input);
                    }
                    ed.Wrap.Data.Enabled = false;
                    paramUI.TypedEditors[mode.Id] = ed;
                }

                paramUI.StepCombo = new Combo($"##Mc-{macroId}-s{stepId}-p{k}-step",
                    paramChoices.Select(c => c.Label).ToArray(), 0,
                    i => OnParamStepChanged(macroId, stepId, pk, i)).WithItemWidth(-1f);
                paramUI.StepWrap = new Container($"Mc-{macroId}-s{stepId}-p{k}-swrap", paramUI.StepCombo);
                paramUI.StepWrap.Data.Enabled = false;

                paramUI.ExprInput = new InputText($"##Mc-{macroId}-s{stepId}-p{k}-expr",
                        hint: "expression...", maxLength: 256,
                        onChanged: v => OnParamExprChanged(macroId, stepId, pk, v))
                    .WithItemWidth(-1f)
                    .WithTooltip("C# expression, e.g. current * 2 + money\n"
                               + "Variables: current, prev, named step outputs, this macro's trigger outputs.\n"
                               + $"Functions: {MacroExpressions.FunctionHelp}");
                paramUI.ExprWrap = new Container($"Mc-{macroId}-s{stepId}-p{k}-ewrap", paramUI.ExprInput);
                paramUI.ExprWrap.Data.Enabled = false;

                paramUI.ExprError = new TextWrapped($"Mc-{macroId}-s{stepId}-p{k}-err", "")
                    .WithStyleColor(ImGuiCol.Text, 1f, 0.35f, 0.35f, 1f);
                paramUI.ExprError.Data.Enabled = false;

                if (macroType?.Type == typeof(Macro))
                {
                    paramUI.MacroHint = new TextWrapped($"Mc-{macroId}-s{stepId}-p{k}-mhint",
                            "Calls itself and may result in an infinite loop.")
                        .WithStyleColor(ImGuiCol.Text, 0.95f, 0.75f, 0.3f, 1f);
                    paramUI.MacroHint.Data.Enabled = false;
                }

                // Unwrap so a float? labels as "float", matching the editor it gets.
                var typeName = MacroValues.FriendlyTypeName(UnwrapNullable(param.Type));
                var label = string.Equals(param.Name, typeName, StringComparison.OrdinalIgnoreCase)
                    ? param.Name
                    : $"{param.Name} ({typeName})";

                var rowChildren = new List<BaseUIElement>
                {
                    new UIText($"Mc-{macroId}-s{stepId}-p{k}-label", label),
                    new SameLine($"Mc-{macroId}-s{stepId}-p{k}-sl"), paramUI.ModeCombo,
                };
                if (paramUI.ValueWrap != null) rowChildren.Add(paramUI.ValueWrap);
                rowChildren.AddRange(paramUI.TypedEditors.Values.Select(e => (BaseUIElement)e.Wrap));
                rowChildren.Add(paramUI.StepWrap);
                rowChildren.Add(paramUI.ExprWrap);
                rowChildren.Add(paramUI.ExprError);
                if (paramUI.MacroHint != null) rowChildren.Add(paramUI.MacroHint);
                (param.IsContext ? contextRows : paramRows)
                    .Add(new Container($"Mc-{macroId}-s{stepId}-p{k}-row", rowChildren.ToArray()));

                stepUI.Params.Add(paramUI);
            }

            body.AddRange(contextRows);
            if (outputRow != null) body.Add(outputRow);
            body.AddRange(paramRows);

            stepUI.OutputRow = outputRow;
            stepUI.ContextRows.AddRange(contextRows);
        }

        var payload = $"{macroId}|{(card.EditingOff ? 1 : 0)}|{stepId}";
        stepUI.Header = new TreeNode($"Mc-{macroId}-s{stepId}", $"###Mc-{macroId}-s{stepId}", body.ToArray())
            .WithLineElements(
                new SmallButton($"{Lucide.Copy}###Mc-{macroId}-s{stepId}-dup", () => DuplicateStep(macroId, stepId))
                    .WithTooltip("Duplicate step"),
                new SmallButton($"{Lucide.X}###Mc-{macroId}-s{stepId}-del", () => RemoveStep(macroId, stepId))
                    .WithTooltip("Delete step"))
            .PinLineElementsEnd()
            .WithDragSource(StepPayloadType, payload)
            .WithDropTarget((_, dropped, below) => OnStepDropped(macroId, stepId, dropped, below), StepPayloadType);

        if (desc != null)
        {
            // Which mod/category the method comes from: steps like "Set Move Speed" exist
            // in several mods and are otherwise indistinguishable on the card. The overlay
            // ellipsizes the tag to the space left between label and buttons.
            stepUI.Header.WithLineTag(ShortCategoryOf(desc),
                $"{CategoryPathOf(desc).Replace("/", " > ")}: {desc.Label}");
        }

        return stepUI;
    }

    /// <summary>The step-line category tag: the mod name for "Mods/..." paths, else the
    /// top-level category ("Flow").</summary>
    private static string ShortCategoryOf(MacroMethodDescriptor desc)
    {
        var segments = CategoryPathOf(desc).Split('/');
        return segments.Length > 1 ? segments[1] : segments[0];
    }

    /// <summary>The Value-mode editor widget for a parameter, picked from its type:
    /// checkbox for bools, name combo for enums, drag fields for numbers/vectors, a color
    /// picker for colors, and a text input for strings/anything else. All editors persist
    /// through the constant's display-string form, so <see cref="MacroValues.Coerce"/> and
    /// old macro files keep working unchanged.</summary>
    private BaseUIElement BuildValueEditor(ParamUI paramUI, MacroParam param, string macroId, string stepId, int pk)
    {
        var t = UnwrapNullable(param.Type);
        var id = $"##Mc-{macroId}-s{stepId}-p{pk}";

        if (t == typeof(bool))
        {
            paramUI.BoolBox = new Checkbox(id, onChanged: v => OnParamConstantEdited(macroId, stepId, pk, v));
            return paramUI.BoolBox;
        }
        if (t is { IsEnum: true })
        {
            paramUI.EnumNames = Enum.GetNames(t);
            paramUI.EnumCombo = new Combo(id, paramUI.EnumNames, 0, i =>
            {
                if (i >= 0 && i < paramUI.EnumNames.Length)
                    OnParamConstantEdited(macroId, stepId, pk, paramUI.EnumNames[i]);
            }).WithItemWidth(-1f);
            return paramUI.EnumCombo;
        }
        if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
        {
            paramUI.IntBox = new DragInt(id, speed: 0.25f,
                onValueChanged: v => OnParamConstantEdited(macroId, stepId, pk, v)).WithItemWidth(-1f);
            return paramUI.IntBox;
        }
        if (t == typeof(float) || t == typeof(double))
        {
            paramUI.FloatBox = new DragFloat(id, speed: 0.05f,
                onValueChanged: v => OnParamConstantEdited(macroId, stepId, pk, v)).WithItemWidth(-1f);
            return paramUI.FloatBox;
        }
        if (t == typeof(Vector2))
        {
            paramUI.Vec2Box = new DragFloat2(id, speed: 0.05f,
                onValueChanged: v => OnParamConstantEdited(macroId, stepId, pk, v)).WithItemWidth(-1f);
            return paramUI.Vec2Box;
        }
        if (t == typeof(Vector3))
        {
            paramUI.Vec3Box = new DragFloat3(id, speed: 0.05f,
                onValueChanged: v => OnParamConstantEdited(macroId, stepId, pk, v)).WithItemWidth(-1f);
            return paramUI.Vec3Box;
        }
        if (t == typeof(Color))
        {
            paramUI.ColorBox = new ColorEdit4(id,
                onChanged: v => OnParamConstantEdited(macroId, stepId, pk, v)).WithItemWidth(-1f);
            return paramUI.ColorBox;
        }

        paramUI.Input = new InputText(id,
            onChanged: v => OnParamTextChanged(macroId, stepId, pk, v)).WithItemWidth(-1f);
        return paramUI.Input;
    }

    private void OnStepDropped(string macroId, string targetStepId, string payload, bool below)
    {
        var parts = payload?.Split('|');
        if (parts == null || parts.Length != 3) return;

        var card = FindCard(macroId);
        var macro = FindMacro(macroId);
        if (card == null || macro == null) return;

        if (parts[0] != macroId || parts[1] != (card.EditingOff ? "1" : "0")) return;

        var steps = StepsOf(macro, card.EditingOff);
        var source = steps.FirstOrDefault(s => s.Id == parts[2]);
        var target = steps.FirstOrDefault(s => s.Id == targetStepId);
        if (source == null || target == null || source == target) return;

        steps.Remove(source);
        steps.Insert(steps.IndexOf(target) + (below ? 1 : 0), source);
        MacroManager.NotifyEdited();
    }

    private void RefreshStepRow(CardUI card, StepUI stepUI, List<MacroStep> steps, int index, bool simple)
    {
        var step = steps[index];
        _methodById.TryGetValue(step.MethodId ?? "", out var desc);

        // Simple mode hides the target/context rows and the output-name row outright.
        if (stepUI.OutputRow != null) ShowIf(stepUI.OutputRow, !simple);
        foreach (var contextRow in stepUI.ContextRows)
            ShowIf(contextRow, !simple);

        string label;
        if (desc != null)
        {
            label = desc.Label;
            var summary = stepUI.CustomEditor?.Summary(step);
            if (!string.IsNullOrEmpty(summary)) label = $"{label}: {summary}";
        }
        else if (!string.IsNullOrEmpty(step.MethodId))
            label = $"(missing: {step.MethodId})";
        else
            label = "(no method)";
        if (label.Length > StepNameMaxChars)
            label = label.Substring(0, StepNameMaxChars - 1) + "…";

        var header = $"{label}###Mc-{card.MacroId}-s{stepUI.StepId}";
        if (stepUI.Header.Label != header)
            stepUI.Header.Label = header;

        if (stepUI.OutputNameBox != null && !stepUI.OutputNameBox.IsFocused
            && stepUI.OutputNameBox.Value != (step.OutputName ?? ""))
            stepUI.OutputNameBox.Value = step.OutputName ?? "";

        if (stepUI.CustomEditor != null)
        {
            stepUI.CustomEditor.Refresh(stepUI.EditorCtx);
            return; // custom steps own their whole body; no param rows to sync
        }

        if (desc != null) step.MigrateLegacyArgs(desc);

        for (var k = 0; k < stepUI.Params.Count; k++)
        {
            var paramUI = stepUI.Params[k];
            var source = desc != null && k < desc.Parameters.Length
                ? step.GetArg(desc.Parameters[k].Name)
                : null;

            var mode = ModeOf(source);
            var modeIdx = Array.IndexOf(paramUI.ModeKeys, mode);
            if (modeIdx < 0) modeIdx = 0;
            SetCombo(paramUI.ModeCombo, modeIdx);
            // Simple mode: no mode selector  every parameter edits as its plain value.
            ShowIf(paramUI.ModeCombo, !simple);

            if (paramUI.ValueWrap != null) ShowIf(paramUI.ValueWrap, mode == "Value");
            ShowIf(paramUI.StepWrap, mode == "Step");
            ShowIf(paramUI.ExprWrap, mode == "Expr");
            foreach (var kv in paramUI.TypedEditors)
                ShowIf(kv.Value.Wrap, mode == $"typed:{kv.Key}");

            string error = null;
            switch (source)
            {
                case StepOutputValueSource so:
                {
                    var choice = paramUI.StepChoiceIds.IndexOf(so.StepId ?? "");
                    if (choice >= 0) SetCombo(paramUI.StepCombo, choice);
                    break;
                }
                case ExpressionValueSource expr:
                {
                    if (!paramUI.ExprInput.IsFocused && paramUI.ExprInput.Value != (expr.Text ?? ""))
                        paramUI.ExprInput.Value = expr.Text ?? "";
                    error = ValidateExpr(card, steps, index, k, expr.Text);
                    break;
                }
                case TypedModeValueSource tm:
                {
                    if (paramUI.TypedEditors.TryGetValue(tm.ModeId, out var ed))
                        RefreshTypedEditor(ed, tm.Arg);
                    break;
                }
                default:
                {
                    var display = source is ConstantValueSource c ? MacroValues.ToDisplay(c.Value) : "";
                    RefreshValueEditor(paramUI, display);
                    break;
                }
            }

            ShowIf(paramUI.ExprError, mode == "Expr" && error != null);
            if (error != null) SetWrapText(paramUI.ExprError, error);

            if (paramUI.MacroHint != null)
            {
                var self = source is TypedModeValueSource picked && !string.IsNullOrEmpty(picked.Arg)
                    && FindMacro(card.MacroId) is { } m
                    && (picked.Arg == m.Id
                        || string.Equals(picked.Arg, m.Slug, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(picked.Arg, m.Name, StringComparison.OrdinalIgnoreCase));
                ShowIf(paramUI.MacroHint, self);
            }
        }
    }

    /// <summary>Sync a macro-type mode editor: refresh live choices and show the stored arg.</summary>
    private static void RefreshTypedEditor(TypedEditorUI ed, string arg)
    {
        if (ed.Input != null)
        {
            if (!ed.Input.IsFocused && ed.Input.Value != (arg ?? ""))
                ed.Input.Value = arg ?? "";
            return;
        }

        string[] items;
        try { items = ed.Mode.Choices() ?? Array.Empty<string>(); }
        catch { items = Array.Empty<string>(); } // no game instance yet etc.

        // manual compare; array.SequenceEqual NREs under this game's Mono
        var changed = items.Length != ed.Items.Count;
        if (!changed)
            for (var i = 0; i < items.Length; i++)
                if (items[i] != ed.Items[i]) { changed = true; break; }
        if (changed)
        {
            ed.Items.Clear();
            ed.Items.AddRange(items);
            ((ComboData)ed.Choices.Data).Items = items;
            ed.Choices.MarkChanged();
        }

        var idx = ed.Items.IndexOf(arg ?? "");
        if (idx >= 0) SetCombo(ed.Choices, idx);
    }

    /// <summary>Push the constant's display string into whichever typed editor this param
    /// uses, skipping the send when the editor already shows the value.</summary>
    private static void RefreshValueEditor(ParamUI paramUI, string display)
    {
        if (paramUI.BoolBox != null)
        {
            SetCheck(paramUI.BoolBox, SafeCoerce<bool>(display));
        }
        else if (paramUI.EnumCombo != null)
        {
            var idx = Array.FindIndex(paramUI.EnumNames,
                n => string.Equals(n, display?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) SetCombo(paramUI.EnumCombo, idx);
        }
        else if (paramUI.IntBox != null)
        {
            var v = SafeCoerce<int>(display);
            if (paramUI.IntBox.Value != v) paramUI.IntBox.Value = v;
        }
        else if (paramUI.FloatBox != null)
        {
            var v = SafeCoerce<float>(display);
            if (paramUI.FloatBox.Value != v) paramUI.FloatBox.Value = v;
        }
        else if (paramUI.Vec2Box != null)
        {
            var v = SafeCoerce<Vector2>(display);
            if (paramUI.Vec2Box.Value != v) paramUI.Vec2Box.Value = v;
        }
        else if (paramUI.Vec3Box != null)
        {
            var v = SafeCoerce<Vector3>(display);
            if (paramUI.Vec3Box.Value != v) paramUI.Vec3Box.Value = v;
        }
        else if (paramUI.ColorBox != null)
        {
            var v = SafeCoerce<Color>(display);
            if (paramUI.ColorBox.Value != v) paramUI.ColorBox.Value = v;
        }
        else if (paramUI.Input != null)
        {
            if (!paramUI.Input.IsFocused && paramUI.Input.Value != display)
                paramUI.Input.Value = display;
        }
    }

    private static T SafeCoerce<T>(string display)
    {
        try { return (T)MacroValues.Coerce(display, typeof(T)); }
        catch { return default; }
    }

    private static string ModeOf(ValueSource source) => source switch
    {
        ToggleValueSource       => "Toggle",
        StepOutputValueSource   => "Step",
        ExpressionValueSource   => "Expr",
        TypedModeValueSource tm => $"typed:{tm.ModeId}",
        _                       => "Value",
    };

    /// <summary>Edit-time expression check with the same variables the runner will declare:
    /// current (when the param has a live getter), prev, earlier steps' output names, and the
    /// macro's trigger outputs (bound to their fired value at run time).</summary>
    private string ValidateExpr(CardUI card, List<MacroStep> steps, int index, int paramIndex, string text)
    {
        _methodById.TryGetValue(steps[index].MethodId ?? "", out var desc);
        var param = desc != null && paramIndex < desc.Parameters.Length ? desc.Parameters[paramIndex] : null;

        var vars = new List<KeyValuePair<string, Type>>();
        if (param?.CurrentValueGetter != null)
            vars.Add(new KeyValuePair<string, Type>("current", param.Type ?? typeof(object)));

        var prevType = typeof(object);
        if (index > 0)
        {
            _methodById.TryGetValue(steps[index - 1].MethodId ?? "", out var pd);
            if (pd != null && HasOutput(pd)) prevType = OutputTypeOf(pd);
        }
        vars.Add(new KeyValuePair<string, Type>("prev", prevType));

        for (var p = 0; p < index; p++)
        {
            var name = steps[p].OutputName;
            if (string.IsNullOrEmpty(name)) continue;
            if (vars.Any(v => string.Equals(v.Key, name, StringComparison.OrdinalIgnoreCase))) continue;
            _methodById.TryGetValue(steps[p].MethodId ?? "", out var pd);
            vars.Add(new KeyValuePair<string, Type>(name,
                pd != null && HasOutput(pd) ? OutputTypeOf(pd) : typeof(object)));
        }

        // The macro's trigger outputs, lowest precedence (current/prev/step outputs shadow them),
        // matching ExpressionValueSource's runtime binding order.
        var macro = FindMacro(card.MacroId);
        var outputs = MacroTriggerRegistry.For(macro?.Trigger)?.ResolveOutputs(macro?.Trigger);
        if (outputs != null)
            foreach (var output in outputs)
            {
                if (string.IsNullOrEmpty(output?.Key)) continue;
                if (vars.Any(v => string.Equals(v.Key, output.Key, StringComparison.OrdinalIgnoreCase))) continue;
                vars.Add(new KeyValuePair<string, Type>(output.Key, output.Type ?? typeof(object)));
            }

        return MacroExpressions.Validate(text ?? "", vars);
    }

    private static bool HasOutput(MacroMethodDescriptor desc)
    {
        var t = OutputTypeOf(desc);
        return t != null && t != typeof(void)
               && !typeof(System.Collections.IEnumerator).IsAssignableFrom(t);
    }

    /// <summary>The type of a step's named output: the method's explicit
    /// <see cref="MacroMethodDescriptor.OutputType"/> when set (a waited macro call returns a value
    /// while handing the runner an IEnumerator), else its <see cref="MacroMethodDescriptor.ReturnType"/>.</summary>
    private static Type OutputTypeOf(MacroMethodDescriptor desc) => desc.OutputType ?? desc.ReturnType;

    /// <summary>Nullable params (float?, int?, bool?) edit and label like their underlying
    /// type; <see cref="MacroValues.Coerce"/> already unwraps them the same way.</summary>
    private static Type UnwrapNullable(Type t)
        => t == null ? null : Nullable.GetUnderlyingType(t) ?? t;


    private void OpenNewMacroModal(string groupId)
    {
        _newMacroTargetGroupId = groupId;
        _nameInput.Value = "";
        _newMacroModal.Open();
        _nameInputBox.FocusNextFrame();
    }

    private void OnNameInputKey(ImGuiKey key)
    {
        switch (key)
        {
            case ImGuiKey.Enter:
            case ImGuiKey.KeypadEnter:
                CreateMacroFromInput();
                break;

            case ImGuiKey.Escape:
                _newMacroModal.Close();
                break;
        }
    }

    private void CreateMacroFromInput()
    {
        var name = _nameInput.Value?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _newMacroModal.Close();
        var group = MacroManager.Groups.FirstOrDefault(g => g.Id == _newMacroTargetGroupId);
        MacroManager.Add(name, group);
    }

    // ── Group create / rename / delete ────────────────────────────────────

    private void OpenNewGroupModal()
    {
        _groupNameInput.Value = "";
        _newGroupModal.Open();
        _groupNameBox.FocusNextFrame();
    }

    private void OnGroupNameInputKey(ImGuiKey key)
    {
        switch (key)
        {
            case ImGuiKey.Enter:
            case ImGuiKey.KeypadEnter:
                CreateGroupFromInput();
                break;

            case ImGuiKey.Escape:
                _newGroupModal.Close();
                break;
        }
    }

    private void CreateGroupFromInput()
    {
        var name = _groupNameInput.Value?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        _newGroupModal.Close();
        MacroManager.AddGroup(name);
    }

    private void OnGroupNameEdited(string groupId, string value)
    {
        var group = MacroManager.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null || group.Name == value) return;
        MacroManager.RenameGroup(group, value);
    }

    private void OpenDeleteGroupModal(string groupId)
    {
        var group = MacroManager.Groups.FirstOrDefault(g => g.Id == groupId);
        if (group == null || group.IsDefault) return;
        _pendingDeleteGroupId = groupId;
        var n = group.Macros.Count;
        var count = n == 0 ? "no macros" : n == 1 ? "1 macro" : $"{n} macros";
        SetText(_deleteGroupText,
            $"Delete group \"{group.Name}\" and its {count}? This cannot be undone.\n"
            + "(The group's shared file is removed too.)");
        _deleteGroupModal.Open();
    }

    private void DeletePendingGroup()
    {
        _deleteGroupModal.Close();
        var group = MacroManager.Groups.FirstOrDefault(g => g.Id == _pendingDeleteGroupId);
        _pendingDeleteGroupId = null;
        if (group != null) MacroManager.RemoveGroup(group);
    }

    // ── Move macro to another group ────────────────────────────────────────

    private void OpenMoveModal(string macroId)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;

        _moveTargetMacroId = macroId;
        var current = MacroManager.GroupOf(macro);

        _moveGroupIds.Clear();
        var labels = new List<string>();
        foreach (var group in MacroManager.Groups)
        {
            if (group == current) continue; // no point moving to where it already is
            _moveGroupIds.Add(group.Id);
            labels.Add(group.IsDefault ? $"{group.Name} (default)" : group.Name);
        }

        if (_moveGroupIds.Count == 0) return; // only one group exists; nowhere to move

        ((ComboData)_moveCombo.Data).Items = labels.ToArray();
        _moveCombo.MarkChanged();
        SetCombo(_moveCombo, 0);
        _moveModal.Open();
    }

    private void ConfirmMove()
    {
        _moveModal.Close();
        var macro = FindMacro(_moveTargetMacroId);
        _moveTargetMacroId = null;
        if (macro == null) return;

        var idx = _moveCombo.SelectedIndex;
        if (idx < 0 || idx >= _moveGroupIds.Count) return;
        var target = MacroManager.Groups.FirstOrDefault(g => g.Id == _moveGroupIds[idx]);
        if (target != null) MacroManager.MoveMacro(macro, target);
    }

    // ── Drag-and-drop reorder / move (the card grip handle) ────────────────

    /// <summary>A card was dropped into the gap before/after the card <paramref name="targetMacroId"/>
    /// (<paramref name="after"/> = the right half): place the dragged macro there, which reorders
    /// within a group and moves across groups with the same gesture.</summary>
    private void OnCardDrop(string targetMacroId, string sourceMacroId, bool after)
    {
        if (string.IsNullOrEmpty(sourceMacroId) || sourceMacroId == targetMacroId) return;
        var source = FindMacro(sourceMacroId);
        var target = FindMacro(targetMacroId);
        if (source == null || target == null) return;
        MacroManager.PlaceMacro(source, target, after);
    }

    /// <summary>A card's grip was dropped on an empty group: move the dragged macro into it.</summary>
    private void OnGroupDrop(string groupId, string sourceMacroId)
    {
        if (string.IsNullOrEmpty(sourceMacroId)) return;
        var source = FindMacro(sourceMacroId);
        var group  = MacroManager.Groups.FirstOrDefault(g => g.Id == groupId);
        if (source == null || group == null) return;
        MacroManager.MoveMacro(source, group);
    }

    /// <summary>A card was dropped into the empty space after the last row (the grid tail): place
    /// it after the group's last macro. Handles same-group "move to end" too (MoveMacro would
    /// no-op when the source is already in the group).</summary>
    private void OnTailDrop(string groupId, string sourceMacroId)
    {
        if (string.IsNullOrEmpty(sourceMacroId)) return;
        var source = FindMacro(sourceMacroId);
        var group  = MacroManager.Groups.FirstOrDefault(g => g.Id == groupId);
        if (source == null || group == null || group.Macros.Count == 0) return;
        var last = group.Macros[group.Macros.Count - 1];
        MacroManager.PlaceMacro(source, last, after: true);
    }

    private void DuplicateMacro(string macroId)
    {
        var macro = FindMacro(macroId);
        if (macro != null)
            MacroManager.Duplicate(macro);
    }

    private void OpenDeleteModal(string macroId)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;
        _pendingDeleteId = macro.Id;
        _deleteModalText.Text = $"Delete \"{macro.Name}\"? This cannot be undone.";
        _deleteModal.Open();
    }

    private void DeletePendingMacro()
    {
        _deleteModal.Close();
        var macro = FindMacro(_pendingDeleteId);
        _pendingDeleteId = null;
        if (macro != null)
            MacroManager.Remove(macro);
    }


    // ── Simple / Advanced mode ────────────────────────────────────────────

    /// <summary>The Advanced checkbox: unchecking it (advanced → simple) warns first when
    /// the macro actually uses advanced features, since simple mode flattens them to plain
    /// values. Checking it (simple → advanced) just reveals the full editor, no warning.</summary>
    private void OnAdvancedToggled(string macroId, bool advanced)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;
        var wantSimple = !advanced;
        if (macro.Simple == wantSimple) return;

        if (wantSimple && HasAdvancedFeatures(macro))
        {
            _pendingSimpleId = macroId;
            var card = FindCard(macroId);
            if (card != null) SetCheck(card.AdvancedBox, true); // hold at advanced until confirmed
            SetText(_simpleModalText,
                $"\"{macro.Name}\" uses advanced features (expressions, toggles, step outputs, or named "
                + "outputs). Switching to Simple Mode resets those parameters to plain values and hides "
                + "the advanced options.\n\nYou can switch back to Advanced Mode later, but you may have to "
                + "re-enter the expressions.");
            _simpleModal.Open();
            return;
        }

        macro.Simple = wantSimple;
        MacroManager.NotifyEdited();
    }

    private void ConfirmSwitchToSimple()
    {
        var macroId = _pendingSimpleId;
        _pendingSimpleId = null;
        _simpleModal.Close();

        var macro = FindMacro(macroId);
        if (macro == null) return;
        RefreshMethodRegistry();
        FlattenToSimple(macro);
        macro.Simple = true;
        MacroManager.NotifyEdited();
    }

    private void CancelSwitchToSimple()
    {
        _simpleModal.Close();
        RevertAdvancedCheckbox();
    }

    /// <summary>Modal dismissed without confirming (Cancel button, or the overlay closing it
    /// via the X / click-away <see cref="Modal.OnClose"/>): restore the checkbox to the macro's
    /// actual (still-advanced) state. No-op once <see cref="ConfirmSwitchToSimple"/> cleared
    /// the pending id.</summary>
    private void RevertAdvancedCheckbox()
    {
        var macroId = _pendingSimpleId;
        _pendingSimpleId = null;
        var macro = FindMacro(macroId);
        var card = FindCard(macroId);
        if (macro != null && card != null) SetCheck(card.AdvancedBox, !macro.Simple);
    }

    /// <summary>Whether the macro holds anything simple mode can't show: a Toggle / Step /
    /// Expression value source on an ordinary parameter, or a named step output. A param that
    /// is inherently an expression (core.expr) doesn't count  it has no plain-value form.</summary>
    private bool HasAdvancedFeatures(Macro macro)
    {
        foreach (var step in StepsOf(macro, false).Concat(StepsOf(macro, true)))
        {
            if (!string.IsNullOrEmpty(step.OutputName)) return true;
            _methodById.TryGetValue(step.MethodId ?? "", out var desc);
            if (desc?.CustomEditor != null) return true; // a custom step is never a plain value
            step.MigrateLegacyArgs(desc);
            foreach (var kv in step.NamedArgs)
            {
                var mode = ModeOf(kv.Value);
                if (mode == "Toggle" || mode == "Step") return true;
                if (mode == "Expr")
                {
                    var param = desc?.Parameters.FirstOrDefault(
                        p => string.Equals(p.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                    if (param is not { PrefersExpression: true }) return true;
                }
            }
        }
        return false;
    }

    /// <summary>Rewrite every advanced value source (Toggle / Step / Expression) on ordinary
    /// parameters to a plain constant seeded from the parameter's live value, and drop named
    /// outputs, so a simple macro only ever holds constants. The old sources are stashed by
    /// mode, so switching back to Advanced and re-picking the mode restores them.</summary>
    private void FlattenToSimple(Macro macro)
    {
        foreach (var step in StepsOf(macro, false).Concat(StepsOf(macro, true)))
        {
            _methodById.TryGetValue(step.MethodId ?? "", out var desc);
            if (desc?.CustomEditor != null) continue; // custom steps own their data; leave it intact
            step.OutputName = null;
            if (desc == null) continue;
            step.MigrateLegacyArgs(desc);

            foreach (var param in desc.Parameters)
            {
                if (param.PrefersExpression) continue; // no plain-value form to flatten to
                var source = step.GetArg(param.Name);
                if (source == null) continue;
                var mode = ModeOf(source);
                if (mode != "Toggle" && mode != "Step" && mode != "Expr") continue;

                step.ArgStash[$"{param.Name}:{mode}"] = source;
                step.SetArg(param.Name, new ConstantValueSource
                {
                    Value = param.CurrentValueGetter != null
                        ? MacroValues.ToDisplay(param.CurrentValueGetter())
                        : MacroValues.ToDisplay(MacroValues.DefaultFor(param.Type)),
                });
            }
        }
    }


    /// <summary>Play fires the macro exactly like its trigger would (Toggle macros
    /// alternate on/off); while any run of it is alive (including the detached iterations
    /// of a Wait-driven loop) the same button reads Stop and winds the whole run tree down.</summary>
    private void OnRunClicked(string macroId)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;
        if (MacroRunner.IsRunning(macro)) MacroRunner.Stop(macro);
        else MacroManager.Fire(macro);
    }

    private void OnRunningChanged(string macroId)
    {
        var card = FindCard(macroId);
        var macro = FindMacro(macroId);
        if (card != null && macro != null) RefreshRunButton(card, macro);
    }

    private static void RefreshRunButton(CardUI card, Macro macro)
    {
        var running = MacroRunner.IsRunning(macro);
        var label = $"{(running ? Lucide.Square : Lucide.Play)}###Mc-{card.MacroId}-run";
        if (card.RunButton.Data.Name == label) return;
        card.RunButton.Data.Name = label;
        card.RunButton.Data.Tooltip = running ? "Stop" : "Run";
        card.RunButton.MarkChanged();
    }

    private void OnNameEdited(string macroId, string value)
    {
        var macro = FindMacro(macroId);
        if (macro == null || macro.Name == value) return;
        MacroManager.Rename(macro, value);
    }

    private void OnEnabledChanged(string macroId, bool value)
    {
        var macro = FindMacro(macroId);
        if (macro == null || macro.Enabled == value) return;
        macro.Enabled = value;
        MacroManager.NotifyTriggerChanged();
    }

    /// <summary>Trigger-dropdown labels, in registry order  the index maps straight to
    /// <see cref="MacroTriggerRegistry.All"/>.</summary>
    private static string[] TriggerLabels()
        => MacroTriggerRegistry.All.Select(t => t.Label).ToArray();

    private void OnTriggerTypeChanged(string macroId, int index)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;
        var all = MacroTriggerRegistry.All;
        if (index < 0 || index >= all.Count) return;

        var descriptor = all[index];
        if (macro.Trigger.TypeId == descriptor.Id) return;

        // Switching kinds: drop the old kind's config and seed the new one's defaults so its
        // fields start from something valid.
        macro.Trigger.TypeId = descriptor.Id;
        macro.Trigger.Config.Clear();
        SeedTriggerDefaults(macro.Trigger, descriptor);
        MacroManager.NotifyTriggerChanged();
    }

    private static void SeedTriggerDefaults(MacroTrigger trigger, MacroTriggerDescriptor descriptor)
    {
        foreach (var param in descriptor.Params)
            trigger.Config[param.Key] = MacroValues.ToDisplay(param.Default);
    }

    // ── Trigger config rows (generic; drives Manual/Hotkey and any mod trigger) ──

    /// <summary>Rebuild the config field rows for a card's currently-selected trigger kind.</summary>
    private void RebuildTriggerConfig(CardUI card, MacroTriggerDescriptor descriptor)
    {
        foreach (var field in card.TriggerFields)
            Win.RemoveElement(field.Row);
        card.TriggerFields.Clear();
        if (card.TriggerOutputsBlock != null)
        {
            Win.RemoveElement(card.TriggerOutputsBlock);
            card.TriggerOutputsBlock = null;
        }
        card.TriggerOutputsSig = null; // force RefreshTriggerOutputs to rebuild for the new kind
        card.TriggerDescId = descriptor.Id;

        for (var i = 0; i < descriptor.Params.Length; i++)
        {
            var field = BuildTriggerField(card.MacroId, descriptor.Id, descriptor.Params[i]);
            Win.AddElement(field.Row, card.TriggerConfigHost, i);
            card.TriggerFields.Add(field);
        }

        // The "Provides" (outputs) block is built by RefreshTriggerOutputs, which also rebuilds it
        // when a dynamic-output trigger's config changes the output set.
    }

    /// <summary>Rebuild the read-only "Provides" outputs block when the resolved output set changes
    /// (on a kind switch, or a config edit for a dynamic-output trigger). A stable signature avoids
    /// rebuilding  and the same-frame add/remove churn  when nothing changed.</summary>
    private void RefreshTriggerOutputs(CardUI card, MacroTriggerDescriptor descriptor, MacroTriggerOutput[] outputs)
    {
        var signature = OutputsSignature(outputs);
        if (signature == card.TriggerOutputsSig) return;
        card.TriggerOutputsSig = signature;

        if (card.TriggerOutputsBlock != null)
        {
            Win.RemoveElement(card.TriggerOutputsBlock);
            card.TriggerOutputsBlock = null;
        }

        if (outputs.Length == 0) return;

        // The block's ids include a signature hash so a rebuild (removed + re-added the same frame)
        // never reuses a just-removed id  which the overlay would drop.
        card.TriggerOutputsBlock = BuildTriggerOutputs(card.MacroId, descriptor, outputs, signature);
        Win.AddElement(card.TriggerOutputsBlock, card.TriggerConfigHost, card.TriggerFields.Count);
    }

    /// <summary>A stable string identifying an output set by keys + types, used to detect changes.</summary>
    private static string OutputsSignature(MacroTriggerOutput[] outputs)
    {
        if (outputs == null || outputs.Length == 0) return "";
        var sb = new System.Text.StringBuilder();
        foreach (var o in outputs)
        {
            if (string.IsNullOrEmpty(o?.Key)) continue;
            sb.Append(o.Key).Append(':').Append(o.Type?.FullName ?? "?").Append('|');
        }
        return sb.ToString();
    }

    /// <summary>The trigger's declared outputs as a read-only block: each line is the variable name
    /// (its key) and type, hovering shows the description. These are the names usable in the macro's
    /// expressions  as a bare variable or via <c>trigger("name")</c>.</summary>
    private BaseUIElement BuildTriggerOutputs(string macroId, MacroTriggerDescriptor descriptor,
        MacroTriggerOutput[] outputs, string signature)
    {
        // Uniquify ids by a short signature hash so dynamic rebuilds don't collide with the block
        // just removed this frame (see the runtime add/remove hazard).
        var slug = $"{macroId}-{Slugify(descriptor.Id)}-{(uint)signature.GetHashCode():x8}";
        var children = new List<BaseUIElement>
        {
            new Separator($"Mc-tout-sep-{slug}"),
            new UIText($"Mc-tout-hdr-{slug}", "Provides (use in expressions):")
                .WithStyleColor(ImGuiCol.Text, 0.55f, 0.55f, 0.55f, 1f)
                .WithTooltip("Values this trigger hands the macro when it fires.\n"
                           + "Use a name directly in an expression (e.g. amount * 2)\n"
                           + "or as trigger(\"name\"). A manual run sees them as their type default."),
        };
        foreach (var output in outputs)
        {
            if (string.IsNullOrEmpty(output?.Key)) continue;
            var line = new UIText($"Mc-tout-{slug}-{output.Key}",
                $"{Lucide.Dot} {output.Key} ({MacroValues.FriendlyTypeName(output.Type)})");
            var tip = string.IsNullOrEmpty(output.Label) ? output.Tooltip
                : string.IsNullOrEmpty(output.Tooltip) ? output.Label
                : $"{output.Label}  {output.Tooltip}";
            if (!string.IsNullOrEmpty(tip)) line.WithTooltip(tip);
            children.Add(line);
        }
        return new Container($"Mc-tout-{slug}", children.ToArray());
    }

    /// <summary>One config field: a keybind capture button, or a typed value widget chosen from
    /// the field's type  the same widget set the step parameter editor uses. Element ids are
    /// scoped by trigger id so switching between two kinds that share a config key never re-adds a
    /// just-removed id in the same frame (which the overlay would drop).</summary>
    private TriggerFieldUI BuildTriggerField(string macroId, string descriptorId, MacroTriggerParam param)
    {
        var slug = $"{macroId}-{Slugify(descriptorId)}-{param.Key}";
        var field = new TriggerFieldUI { MacroId = macroId, Slug = slug, Param = param };
        var wid = $"##Mc-tcfg-{slug}";
        BaseUIElement widget;
        // Extra full-width lines under the label row (a macro type's per-mode arg editors).
        var extraLines = new List<BaseUIElement>();

        if (param.Widget == MacroTriggerWidget.Keybind)
        {
            field.BindButton = new Button($"{Lucide.Keyboard} Set key...###Mc-tbind-{slug}",
                () => OpenBindModal(macroId, param.Key));
            widget = field.BindButton;
        }
        else if (MacroTypes.For(UnwrapNullable(param.Type)) is { } macroType)
        {
            field.MacroType = macroType;
            var labels = new List<string>();
            var ids    = new List<string>();
            if (param.EmptyLabel != null) { labels.Add(param.EmptyLabel); ids.Add(""); }
            foreach (var m in macroType.Modes) { labels.Add(m.Label); ids.Add(m.Id); }
            field.ModeIds = ids.ToArray();

            field.ModeCombo = new Combo(wid, labels.ToArray(), 0,
                i => OnTriggerModePicked(field, i)).WithItemWidth(150f);
            widget = field.ModeCombo;

            foreach (var m in macroType.Modes.Where(m => m.Param != null))
            {
                var mode = m;
                var ed = new TypedEditorUI { Mode = mode };
                if (mode.Choices != null)
                {
                    ed.Choices = new Combo($"##Mc-tcfg-{slug}-t{mode.Id}", Array.Empty<string>(), 0,
                        i => OnTriggerTypedArgPicked(field, ed, i)).WithItemWidth(-1f);
                    ed.Wrap = new Container($"Mc-tcfg-{slug}-t{mode.Id}-wrap", ed.Choices);
                }
                else
                {
                    ed.Input = new InputText($"##Mc-tcfg-{slug}-t{mode.Id}",
                        onChanged: v => OnTriggerTypedArgChanged(field, v)).WithItemWidth(-1f);
                    ed.Wrap = new Container($"Mc-tcfg-{slug}-t{mode.Id}-wrap", ed.Input);
                }
                ed.Wrap.Data.Enabled = false;
                field.TypedEditors[mode.Id] = ed;
                extraLines.Add(ed.Wrap);
            }
        }
        else
        {
            var t = UnwrapNullable(param.Type);
            if (t == typeof(bool))
            {
                field.BoolBox = new Checkbox(wid, onChanged: v => OnTriggerConfigEdited(macroId, param.Key, v));
                widget = field.BoolBox;
            }
            else if (t is { IsEnum: true })
            {
                field.EnumNames = Enum.GetNames(t);
                field.EnumCombo = new Combo(wid, field.EnumNames, 0, i =>
                {
                    if (i >= 0 && i < field.EnumNames.Length)
                        OnTriggerConfigEdited(macroId, param.Key, field.EnumNames[i]);
                }).WithItemWidth(150f);
                widget = field.EnumCombo;
            }
            else if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte))
            {
                field.IntBox = new DragInt(wid, speed: 0.25f,
                    onValueChanged: v => OnTriggerConfigEdited(macroId, param.Key, v)).WithItemWidth(150f);
                widget = field.IntBox;
            }
            else if (t == typeof(float) || t == typeof(double))
            {
                field.FloatBox = new DragFloat(wid, speed: 0.05f,
                    onValueChanged: v => OnTriggerConfigEdited(macroId, param.Key, v)).WithItemWidth(150f);
                widget = field.FloatBox;
            }
            else
            {
                field.Input = new InputText(wid,
                    onChanged: v => OnTriggerConfigEdited(macroId, param.Key, v)).WithItemWidth(150f);
                widget = field.Input;
            }
        }

        var label = new UIText($"Mc-tcfg-lbl-{slug}", param.Label);
        if (!string.IsNullOrEmpty(param.Tooltip)) label.WithTooltip(param.Tooltip);
        var children = new List<BaseUIElement> { label, new SameLine($"Mc-tcfg-sl-{slug}"), widget };
        children.AddRange(extraLines);
        field.Row = new Container($"Mc-tcfg-row-{slug}", children.ToArray());
        return field;
    }

    /// <summary>Identifier-safe form of a trigger id ("core.hotkey" → "core_hotkey") for use in
    /// element ids.</summary>
    private static string Slugify(string id)
    {
        if (string.IsNullOrEmpty(id)) return "x";
        var sb = new StringBuilder(id.Length);
        foreach (var ch in id)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        return sb.ToString();
    }

    /// <summary>Sync one config field's widget from the stored value.</summary>
    private static void RefreshTriggerField(TriggerFieldUI field, MacroTrigger trigger)
    {
        var display = trigger.GetString(field.Param.Key) ?? MacroValues.ToDisplay(field.Param.Default);

        if (field.BindButton != null)
        {
            var label = $"{Lucide.Keyboard} {BindingLabel(display)}###Mc-tbind-{field.Slug}";
            if (field.BindButton.Data.Name != label)
            {
                field.BindButton.Data.Name = label;
                field.BindButton.MarkChanged();
            }
        }
        else if (field.BoolBox != null) SetCheck(field.BoolBox, SafeCoerce<bool>(display));
        else if (field.EnumCombo != null)
        {
            var idx = Array.FindIndex(field.EnumNames,
                n => string.Equals(n, display?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (idx >= 0) SetCombo(field.EnumCombo, idx);
        }
        else if (field.IntBox != null)
        {
            var v = SafeCoerce<int>(display);
            if (field.IntBox.Value != v) field.IntBox.Value = v;
        }
        else if (field.FloatBox != null)
        {
            var v = SafeCoerce<float>(display);
            if (field.FloatBox.Value != v) field.FloatBox.Value = v;
        }
        else if (field.Input != null)
        {
            if (!field.Input.IsFocused && field.Input.Value != (display ?? ""))
                field.Input.Value = display ?? "";
        }
        else if (field.ModeCombo != null)
        {
            MacroTypes.DecodeSelection(display, out var modeId, out var arg);
            var idx = Array.IndexOf(field.ModeIds, modeId ?? "");
            // Unknown/unset mode with no "any" entry: fall back to the first, matching what
            // MacroTypes.ResolveSelection does with it.
            if (idx < 0) idx = 0;
            SetCombo(field.ModeCombo, idx);

            var shown = field.ModeIds[idx];
            foreach (var kv in field.TypedEditors)
                ShowIf(kv.Value.Wrap, kv.Key == shown);
            if (field.TypedEditors.TryGetValue(shown, out var ed)) RefreshTypedEditor(ed, arg);
        }
    }

    /// <summary>A macro-type config field's mode dropdown: keep the current arg when it still
    /// applies, so flipping By Name → Local → By Name doesn't lose the typed name.</summary>
    private void OnTriggerModePicked(TriggerFieldUI field, int index)
    {
        if (index < 0 || index >= field.ModeIds.Length) return;
        var modeId = field.ModeIds[index];
        MacroTypes.DecodeSelection(FindMacro(field.MacroId)?.Trigger.GetString(field.Param.Key),
            out _, out var arg);
        var takesArg = !string.IsNullOrEmpty(modeId) && field.MacroType?.FindMode(modeId)?.Param != null;
        OnTriggerConfigEdited(field.MacroId, field.Param.Key,
            MacroTypes.EncodeSelection(modeId, takesArg ? arg : null));
    }

    private void OnTriggerTypedArgChanged(TriggerFieldUI field, string text)
    {
        var stored = FindMacro(field.MacroId)?.Trigger.GetString(field.Param.Key);
        MacroTypes.DecodeSelection(stored, out var modeId, out _);
        if (modeId == null) return; // stale event after a switch to the "any" entry
        OnTriggerConfigEdited(field.MacroId, field.Param.Key, MacroTypes.EncodeSelection(modeId, text));
    }

    private void OnTriggerTypedArgPicked(TriggerFieldUI field, TypedEditorUI ed, int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= ed.Items.Count) return;
        OnTriggerTypedArgChanged(field, ed.Items[choiceIndex]);
    }

    private void OnTriggerConfigEdited(string macroId, string key, object value)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;
        var display = MacroValues.ToDisplay(value);
        if (macro.Trigger.Config.TryGetValue(key, out var current) && current == display) return;
        macro.Trigger.Config[key] = display;
        MacroManager.NotifyTriggerChanged();
    }

    // ── Hotkey binding (press-the-keys capture) ───────────────────────────

    /// <summary>Button/label text for a stored binding string ("Ctrl+Shift+F"), or "Set key..."
    /// when unset.</summary>
    private static string BindingLabel(string binding)
        => HotkeyBinding.TryParse(binding, out var b) && b.Key != KeyCode.None ? b.ToString() : "Set key...";

    private void OpenBindModal(string macroId, string configKey)
    {
        var macro = FindMacro(macroId);
        if (macro == null) return;

        _bindTargetId = macroId;
        _bindConfigKey = configKey;
        _bindKey = ImGuiKey.None;
        _bindMods = HotkeyModifiers.None;
        SetText(_bindHint, "Press the key combination for this macro.");
        _bindCapture.Reset(BindingLabel(macro.Trigger.GetString(configKey)));
        _bindModal.Open();
    }

    private void OnBindCaptured(ImGuiKey key, HotkeyModifiers mods)
    {
        if (key == ImGuiKey.Escape)
        {
            CancelBind();
            return;
        }

        _bindKey = key;
        _bindMods = mods;
        SetText(_bindHint, KeyMapper.ToKeyCode(key) == KeyCode.None
            ? $"'{key}' has no game-side key mapping; try another key."
            : "Press the key combination for this macro.");
    }

    private void ConfirmBind()
    {
        var macro = FindMacro(_bindTargetId);
        if (macro == null || _bindKey == ImGuiKey.None)
        {
            CancelBind();
            return;
        }

        var keyCode = KeyMapper.ToKeyCode(_bindKey);
        if (keyCode == KeyCode.None) return; // hint text already explains; pick another key

        macro.Trigger.Config[_bindConfigKey] = new HotkeyBinding(keyCode, _bindMods).ToString();
        _bindCapture.Stop();
        _bindModal.Close();
        MacroManager.NotifyTriggerChanged();
    }

    private void CancelBind()
    {
        _bindCapture.Stop();
        _bindModal.Close();
    }

    private void OnStepListChanged(string macroId, int index)
    {
        var card = FindCard(macroId);
        if (card == null || card.EditingOff == (index == 1)) return;
        card.EditingOff = index == 1;
        RefreshGroups();
    }

    // ── Step callbacks ────────────────────────────────────────────────────

    private MacroStep FindStep(string macroId, string stepId, out List<MacroStep> steps)
    {
        steps = null;
        var macro = FindMacro(macroId);
        var card = FindCard(macroId);
        if (macro == null || card == null) return null;
        steps = StepsOf(macro, card.EditingOff);
        return steps.FirstOrDefault(s => s.Id == stepId);
    }

    private void DuplicateStep(string macroId, string stepId)
    {
        var step = FindStep(macroId, stepId, out var steps);
        if (step == null) return;

        var clone = JsonConvert.DeserializeObject<MacroStep>(JsonConvert.SerializeObject(step));
        clone.Id = Guid.NewGuid().ToString();
        steps.Insert(steps.IndexOf(step) + 1, clone);
        MacroManager.NotifyEdited();
    }

    private void RemoveStep(string macroId, string stepId)
    {
        var step = FindStep(macroId, stepId, out var steps);
        if (step == null) return;
        steps.Remove(step);
        MacroManager.NotifyEdited();
    }

    private void OnParamTextChanged(string macroId, string stepId, int paramIndex, string text)
    {
        var step = FindStep(macroId, stepId, out _);
        var param = ParamOf(step, paramIndex);
        if (param == null) return;
        if (step.GetArg(param.Name) is not ConstantValueSource constant) return; // stale event while Toggle active
        if (MacroValues.ToDisplay(constant.Value) == text) return;
        constant.Value = text;
        MacroManager.NotifyEdited();
    }

    /// <summary>A typed Value editor (checkbox, drag, color, enum combo) changed: store the
    /// value in its display-string form, the same shape the text path and macro JSON use.</summary>
    private void OnParamConstantEdited(string macroId, string stepId, int paramIndex, object value)
    {
        var step = FindStep(macroId, stepId, out _);
        var param = ParamOf(step, paramIndex);
        if (param == null) return;
        if (step.GetArg(param.Name) is not ConstantValueSource constant) return; // stale event after mode switch
        var display = MacroValues.ToDisplay(value);
        if (MacroValues.ToDisplay(constant.Value) == display) return;
        constant.Value = display;
        MacroManager.NotifyEdited();
    }

    private void OnParamModeChanged(string macroId, string stepId, int paramIndex, int modeIndex)
    {
        var step = FindStep(macroId, stepId, out _);
        var paramUI = FindParamUI(macroId, stepId, paramIndex);
        var param = ParamOf(step, paramIndex);
        if (param == null || paramUI == null) return;
        if (modeIndex < 0 || modeIndex >= paramUI.ModeKeys.Length) return;

        var source = EnsureArg(step, param);
        var mode = paramUI.ModeKeys[modeIndex];
        if (mode == ModeOf(source)) return;

        // Stash the outgoing source so switching back restores it as typed.
        step.ArgStash[$"{param.Name}:{ModeOf(source)}"] = source;

        if (step.ArgStash.TryGetValue($"{param.Name}:{mode}", out var stashed)
            && stashed != null && ModeOf(stashed) == mode
            && !(stashed is StepOutputValueSource sr && !paramUI.StepChoiceIds.Contains(sr.StepId ?? "")))
        {
            step.SetArg(param.Name, stashed);
            MacroManager.NotifyEdited();
            return;
        }

        var getter = param.CurrentValueGetter;

        step.SetArg(param.Name, mode switch
        {
            "Toggle" => new ToggleValueSource(),
            "Step"   => new StepOutputValueSource { StepId = paramUI.StepChoiceIds.FirstOrDefault() ?? "" },
            "Expr"   => new ExpressionValueSource { Text = ExprSeedFor(source, param) },
            _ when mode.StartsWith("typed:", StringComparison.Ordinal)
                     => MakeTypedSource(paramUI.MacroType, mode.Substring("typed:".Length)),
            _        => new ConstantValueSource { Value = getter != null ? MacroValues.ToDisplay(getter()) : "" },
        });

        MacroManager.NotifyEdited();
    }

    private static TypedModeValueSource MakeTypedSource(MacroTypeDescriptor macroType, string modeId)
        => MacroManager.MakeTypedSource(macroType, modeId);

    private void OnParamTypedArgChanged(string macroId, string stepId, int paramIndex, string text)
    {
        var step = FindStep(macroId, stepId, out _);
        var param = ParamOf(step, paramIndex);
        if (param == null) return;
        if (step.GetArg(param.Name) is not TypedModeValueSource tm) return; // stale event after mode switch
        if (tm.Arg == text) return;
        tm.Arg = text;
        MacroManager.NotifyEdited();
    }

    private void OnParamTypedArgPicked(string macroId, string stepId, int paramIndex, TypedEditorUI ed, int choiceIndex)
    {
        if (choiceIndex < 0 || choiceIndex >= ed.Items.Count) return;
        OnParamTypedArgChanged(macroId, stepId, paramIndex, ed.Items[choiceIndex]);
    }

    /// <summary>Seed text when a param switches to Expr: the old constant rewritten as a
    /// valid expression for its type (bool literal, quoted string, vec()/rgba() call),
    /// else <c>current</c> when the param has a live getter.</summary>
    private static string ExprSeedFor(ValueSource source, MacroParam param)
    {
        // A typed pick (macro id, player name) carries over as the equivalent string literal.
        if (source is TypedModeValueSource tm && !string.IsNullOrEmpty(tm.Arg))
            return $"\"{tm.Arg.Replace("\"", "\\\"")}\"";

        if (source is ConstantValueSource cc)
        {
            var display = MacroValues.ToDisplay(cc.Value);
            if (!string.IsNullOrEmpty(display))
            {
                // Unwrap so a bool? constant seeds true/false, not an invalid "True" literal.
                var t = UnwrapNullable(param?.Type);
                try
                {
                    if (t == typeof(bool))
                        return (bool)MacroValues.Coerce(display, t) ? "true" : "false";
                    if (t == typeof(Vector2))
                    {
                        var v = (Vector2)MacroValues.Coerce(display, t);
                        return FormattableString.Invariant($"vec2({v.x}, {v.y})");
                    }
                    if (t == typeof(Vector3))
                    {
                        var v = (Vector3)MacroValues.Coerce(display, t);
                        return FormattableString.Invariant($"vec({v.x}, {v.y}, {v.z})");
                    }
                    if (t == typeof(Color))
                    {
                        var v = (Color)MacroValues.Coerce(display, t);
                        return FormattableString.Invariant($"rgba({v.r}, {v.g}, {v.b}, {v.a})");
                    }
                }
                catch
                {
                    // unparseable constant; fall through to the generic seeds below
                }
                if (t is { IsEnum: true } || t == typeof(string))
                    return $"\"{display.Replace("\"", "\\\"")}\"";
                return display; // numeric literals are already valid expressions
            }
        }

        return param?.CurrentValueGetter != null ? "current" : "";
    }

    private void OnParamStepChanged(string macroId, string stepId, int paramIndex, int choiceIndex)
    {
        var step = FindStep(macroId, stepId, out _);
        var paramUI = FindParamUI(macroId, stepId, paramIndex);
        var param = ParamOf(step, paramIndex);
        if (param == null || paramUI == null) return;
        if (choiceIndex < 0 || choiceIndex >= paramUI.StepChoiceIds.Count) return;

        if (step.GetArg(param.Name) is not StepOutputValueSource so) return; // stale event after mode switch
        if (so.StepId == paramUI.StepChoiceIds[choiceIndex]) return;
        so.StepId = paramUI.StepChoiceIds[choiceIndex];
        MacroManager.NotifyEdited();
    }

    private void OnParamExprChanged(string macroId, string stepId, int paramIndex, string text)
    {
        var step = FindStep(macroId, stepId, out _);
        var param = ParamOf(step, paramIndex);
        if (param == null) return;
        if (step.GetArg(param.Name) is not ExpressionValueSource expr) return; // stale event after mode switch
        if (expr.Text == text) return;
        expr.Text = text ?? "";
        MacroManager.NotifyEdited();
    }

    private void OnOutputNameChanged(string macroId, string stepId, string text)
    {
        var step = FindStep(macroId, stepId, out _);
        if (step == null) return;
        var name = SanitizeOutputName(text);
        if ((step.OutputName ?? "") == name) return;
        step.OutputName = name;
        MacroManager.NotifyEdited();
    }

    /// <summary>Identifier characters only, and no leading digit: what expressions can reference.</summary>
    private static string SanitizeOutputName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var sb = new StringBuilder(raw.Length);
        foreach (var ch in raw)
            if (char.IsLetterOrDigit(ch) || ch == '_')
                sb.Append(ch);
        var name = sb.ToString().TrimStart('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
        while (name.Length > 0 && MacroExpressions.IsFunctionName(name))
            name += "_";
        return name;
    }

    private ParamUI FindParamUI(string macroId, string stepId, int paramIndex)
    {
        var card = FindCard(macroId);
        var stepUI = card?.Steps.FirstOrDefault(s => s.StepId == stepId);
        return stepUI != null && paramIndex < stepUI.Params.Count ? stepUI.Params[paramIndex] : null;
    }

    /// <summary>The step method's parameter at <paramref name="paramIndex"/>, or null when
    /// the step/method is gone or the index is stale; args are stored by parameter name.</summary>
    private MacroParam ParamOf(MacroStep step, int paramIndex)
    {
        if (step == null) return null;
        _methodById.TryGetValue(step.MethodId ?? "", out var desc);
        return desc != null && paramIndex >= 0 && paramIndex < desc.Parameters.Length
            ? desc.Parameters[paramIndex]
            : null;
    }

    private static ValueSource EnsureArg(MacroStep step, MacroParam param)
    {
        var source = step.GetArg(param.Name);
        if (source == null)
        {
            source = DefaultSourceFor(param);
            step.SetArg(param.Name, source);
        }
        return source;
    }

    /// <summary>Seed for a parameter with no stored source: a registered macro type's default
    /// mode (e.g. Player → Local Player), else a constant from the live value where available
    /// (numeric setting → its current value), otherwise the type default.</summary>
    private static ValueSource DefaultSourceFor(MacroParam param) => MacroManager.DefaultSourceFor(param);


    private void RefreshMethodRegistry()
    {
        _methodDescs = MacroRegistry.GetAll()
            .OrderBy(m => m.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _methodById = new Dictionary<string, MacroMethodDescriptor>();
        foreach (var desc in _methodDescs)
            _methodById[desc.Id] = desc;
    }

    private void OpenAddStepPicker(string macroId)
    {
        var macro = FindMacro(macroId);
        var card = FindCard(macroId);
        if (macro == null || card == null) return;

        _pickerTargetId = macroId;
        _pickerOff = card.EditingOff;
        _pickerPicked = false;
        _pickerFilter = "";
        if (_pickerFilterBox.Value != "") _pickerFilterBox.Value = "";

        RefreshMethodRegistry();
        EnsurePickerCategories();
        RefreshPickerFilter();
        _addStepModal.Open();
        _pickerFilterBox.FocusNextFrame();
    }

    /// <summary>Rebuild the collapsible category tree when the method registry changed
    /// (new mods/plugins registering methods). No-op otherwise. Categories are
    /// '/'-separated paths; every segment becomes a nested tree node.</summary>
    private void EnsurePickerCategories()
    {
        var signature = string.Join("|", _methodDescs.Select(m => m.Id));
        if (signature == _pickerSignature) return;
        _pickerSignature = signature;

        foreach (var node in _pickerCatNodes)
            Win.RemoveElement(node);
        _pickerCatNodes.Clear();

        var root = new PickerGroup();
        foreach (var desc in _methodDescs)
        {
            var group = root;
            foreach (var segment in CategoryPathOf(desc).Split('/'))
                group = group.Child(segment);
            group.Methods.Add(desc);
        }

        var c = 0;
        foreach (var pair in root.Children)
        {
            var node = BuildPickerGroupNode(pair.Key, pair.Value, ref c);
            Win.AddElement(node, _pickerCatsWrap);
            _pickerCatNodes.Add(node);
        }
    }

    /// <summary>One level of the picker tree, built from '/'-separated Category paths.</summary>
    private sealed class PickerGroup
    {
        public readonly SortedDictionary<string, PickerGroup> Children = new(StringComparer.OrdinalIgnoreCase);
        public readonly List<MacroMethodDescriptor> Methods = new();

        public PickerGroup Child(string name)
            => Children.TryGetValue(name, out var child) ? child : Children[name] = new PickerGroup();

        public int MethodCount => Methods.Count + Children.Values.Sum(g => g.MethodCount);
    }

    private TreeNode BuildPickerGroupNode(string label, PickerGroup group, ref int c)
    {
        var id = c++;

        // Subgroups and method rows sort together by display name.
        var entries = new List<(string Key, BaseUIElement Row)>();
        foreach (var pair in group.Children)
            entries.Add((pair.Key, BuildPickerGroupNode(pair.Key, pair.Value, ref c)));

        var r = 0;
        foreach (var method in group.Methods)
        {
            var desc = method;
            var rowLabel = desc.PickerLabel ?? desc.Label;
            Selectable row = null;
            row = new Selectable($"{rowLabel}###MacrosWindow-pick-{id}-{r}", onChanged: _ =>
            {
                if (row.Selected) row.Selected = false;
                PickMethod(desc);
            });
            entries.Add((rowLabel, row));
            r++;
        }

        var rows = entries
            .OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .Select(e => e.Row)
            .ToArray();
        return new TreeNode($"MacrosWindow-pick-node-{id}", $"{label} ({group.MethodCount})", rows);
    }

    private static string CategoryPathOf(MacroMethodDescriptor desc)
        => string.IsNullOrEmpty(desc.Category) ? "Misc" : desc.Category;

    private void RefreshPickerFilter()
    {
        if (Win == null) return;

        var filter = _pickerFilter.Trim();
        var filtering = filter.Length > 0;

        ShowIf(_pickerCatsWrap, !filtering);
        ShowIf(_pickerFlatWrap, filtering);

        foreach (var row in _pickerFlatRows)
            Win.RemoveElement(row);
        _pickerFlatRows.Clear();

        if (!filtering)
        {
            ShowIf(_pickerMoreHint, false);
            return;
        }

        var matches = _methodDescs.Where(m => MatchesFilter(m, filter)).ToList();
        var r = 0;
        foreach (var method in matches.Take(MaxSearchResults))
        {
            var desc = method;
            Selectable row = null;
            row = new Selectable($"{CategoryPathOf(desc).Replace("/", " > ")}: {desc.PickerLabel ?? desc.Label}###MacrosWindow-pickf-{r}", onChanged: _ =>
            {
                if (row.Selected) row.Selected = false;
                PickMethod(desc);
            });
            Win.AddElement(row, _pickerFlatWrap);
            _pickerFlatRows.Add(row);
            r++;
        }

        var truncated = matches.Count - MaxSearchResults;
        ShowIf(_pickerMoreHint, truncated > 0);
        if (truncated > 0)
            SetText(_pickerMoreHint, $"...and {truncated} more; keep typing to narrow down.");
    }

    private void OnPickerFilterKey(ImGuiKey key)
    {
        switch (key)
        {
            case ImGuiKey.Enter:
            case ImGuiKey.KeypadEnter:
                // Accept the top search result.
                if (_pickerFilter.Trim().Length > 0)
                {
                    var top = _methodDescs.FirstOrDefault(m => MatchesFilter(m, _pickerFilter.Trim()));
                    PickMethod(top);
                }
                break;

            case ImGuiKey.Escape:
                _addStepModal.Close();
                break;
        }
    }

    private static bool MatchesFilter(MacroMethodDescriptor desc, string filter)
        => ContainsIgnoreCase(desc.Label, filter)
        || ContainsIgnoreCase(desc.Category, filter)
        || ContainsIgnoreCase(desc.Id, filter);

    private static bool ContainsIgnoreCase(string haystack, string needle)
        => haystack != null && haystack.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;

    private void PickMethod(MacroMethodDescriptor desc)
    {
        if (desc == null) return;
        // The modal takes a frame or two to actually close over the remote UI, so a
        // fast double-click can fire this twice before it's gone. Consume the pick once.
        if (_pickerPicked) return;
        _pickerPicked = true;
        _addStepModal.Close();

        var macro = FindMacro(_pickerTargetId);
        if (macro == null) return;
        var steps = StepsOf(macro, _pickerOff);

        var step = MacroManager.CreateStep(desc.Id);
        if (step == null) return;
        steps.Add(step);

        MacroManager.NotifyEdited();
    }

    // ── Small update helpers (avoid resending unchanged element state) ────

    private static void ShowIf(BaseUIElement element, bool visible)
    {
        if (element.Data.Enabled != visible)
            element.SetVisible(visible);
    }

    private static void SetCheck(Checkbox box, bool value)
    {
        if (box.Value != value) box.Value = value;
    }

    private static void SetCombo(Combo combo, int index)
    {
        if (combo.SelectedIndex != index) combo.SelectedIndex = index;
    }

    private static void SetText(UIText text, string value)
    {
        if (text.Text != value) text.Text = value;
    }

    private static void SetWrapText(TextWrapped text, string value)
    {
        var data = (TextData)text.Data;
        if (data.Text == value) return;
        data.Text = value;
        text.MarkChanged();
    }
}
