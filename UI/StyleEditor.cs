using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI.Elements;
using Newtonsoft.Json;
using UnityEngine;

namespace lstwoMODS_Core.UI;

/// <summary>
/// JSON-serializable style preset. Holds all 60 ImGuiCol colors and all 39 ImGuiStyleVar values.
/// Call <see cref="ToPushCommands"/> to convert to a list suitable for
/// <see cref="lstwoMODS.ImGui.Shared.ImGuiConfig.GlobalStyle"/>.
/// </summary>
public class StylePresetData
{
    /// <summary>RGBA for each ImGuiCol. Access as Colors[col * 4 + channel], channel 0=R 1=G 2=B 3=A.</summary>
    public float[] Colors { get; set; } = new float[60 * 4];

    /// <summary>Float value, or X for Vec2 vars. Indexed by (int)ImGuiStyleVar.</summary>
    public float[] StyleVarValues { get; set; } = new float[39];

    /// <summary>Y for Vec2 style vars. Zero for float vars.</summary>
    public float[] StyleVarValuesY { get; set; } = new float[39];

    /// <summary>
    /// Optional "simple mode" inputs. When the user authors a preset in simple mode this holds the
    /// four base colors and the rounding/border/spacing values the full palette was generated from,
    /// so re-opening the preset can restore the sliders. Null for presets authored in advanced mode.
    /// </summary>
    public SimpleStyleParams Simple { get; set; }

    /// <summary>
    /// Produces a PushCommand list for <see cref="lstwoMODS.ImGui.Shared.ImGuiConfig.GlobalStyle"/>.
    /// Covers all 60 colors and all 39 style vars.
    /// </summary>
    public List<PushCommand> ToPushCommands()
    {
        var cmds = new List<PushCommand>(99);

        for (int i = 0; i < 60; i++)
            cmds.Add(new PushStyleColorCommand
            {
                Col = (ImGuiCol)i,
                R   = Colors[i * 4],
                G   = Colors[i * 4 + 1],
                B   = Colors[i * 4 + 2],
                A   = Colors[i * 4 + 3],
            });

        for (int i = 0; i < 39; i++)
        {
            var sv = (ImGuiStyleVar)i;
            if (StyleEditor.Vec2StyleVars.Contains(sv))
                cmds.Add(new PushStyleVarVec2Command { Var = sv, X = StyleVarValues[i], Y = StyleVarValuesY[i] });
            else
                cmds.Add(new PushStyleVarCommand { Var = sv, Value = StyleVarValues[i] });
        }

        return cmds;
    }
}

/// <summary>
/// Reshade-style "simple mode" inputs. Four base colors plus a handful of rounding/border/spacing
/// values; <see cref="StyleEditor.ApplySimpleToPreset"/> expands them into the full 60-color +
/// 39-style-var palette so the user only has to pick a few things.
/// </summary>
public class SimpleStyleParams
{
    /// <summary>Primary interactive / highlight color (buttons, checkmarks, sliders, tabs). RGB.</summary>
    public float[] Accent     { get; set; } = { 0.26f, 0.59f, 0.98f };
    /// <summary>Window background color. RGB.</summary>
    public float[] Background { get; set; } = { 0.09f, 0.09f, 0.11f };
    /// <summary>Foreground text color. RGB.</summary>
    public float[] Text       { get; set; } = { 0.95f, 0.95f, 0.96f };
    /// <summary>Border / separator color. RGB.</summary>
    public float[] Border     { get; set; } = { 0.43f, 0.43f, 0.50f };

    /// <summary>Corner rounding applied to windows, frames, scrollbars, grabs and tabs.</summary>
    public float Rounding   { get; set; } = 4f;
    /// <summary>Border thickness applied to windows, frames, popups and tabs.</summary>
    public float BorderSize { get; set; } = 1f;
    /// <summary>Base padding/spacing (drives FramePadding, ItemSpacing, WindowPadding, ...).</summary>
    public float Spacing    { get; set; } = 8f;
}

/// <summary>
/// Reusable style editor UIComponent. Loads and saves presets to a folder; the three
/// built-in ImGui themes (Dark / Light / Classic) are always available at the top of
/// the dropdown. Not bound to any <see cref="OSWindow"/>  fires
/// <see cref="OnStyleChanged"/> so callers can apply the result wherever they like.
///
/// <code>
/// var editor = new StyleEditor("style", Path.Combine(configDir, "styles"));
/// editor.OnStyleChanged += preset => StyleEditor.ApplyToWindow(myWindow, preset);
/// AddElement(editor);
/// </code>
/// </summary>
public class StyleEditor : UIComponent
{
    public static readonly HashSet<ImGuiStyleVar> Vec2StyleVars = new()
    {
        ImGuiStyleVar.WindowPadding,
        ImGuiStyleVar.WindowMinSize,
        ImGuiStyleVar.WindowTitleAlign,
        ImGuiStyleVar.FramePadding,
        ImGuiStyleVar.ItemSpacing,
        ImGuiStyleVar.ItemInnerSpacing,
        ImGuiStyleVar.CellPadding,
        ImGuiStyleVar.TableAngledHeadersTextAlign,
        ImGuiStyleVar.ButtonTextAlign,
        ImGuiStyleVar.SelectableTextAlign,
        ImGuiStyleVar.SeparatorTextAlign,
        ImGuiStyleVar.SeparatorTextPadding,
    };

    private static readonly string[] BuiltInNames = ["Dark", "Light", "Classic"];


    private readonly string _folderPath;
    private readonly string _selectedFile;
    private StylePresetData _currentPreset = new();
    private string[] _customNames = [];

    
    private readonly Ref<int>      _comboIndex  = new(0);
    private readonly Ref<string[]> _comboItems  = new(BuiltInNames.ToArray());
    private readonly Ref<string>   _saveAsName  = new("");

    private Button _saveBtn;
    private Button _deleteBtn;
    private Modal  _warningModal;
    private int    _currentCustomIndex = -1;

    private readonly Ref<Color>[]   _colorRefs = Enumerable.Range(0, 60).Select(_ => new Ref<Color>()).ToArray();
    private readonly Ref<float>[]   _varRefF   = Enumerable.Range(0, 39).Select(_ => new Ref<float>()).ToArray();
    private readonly Ref<Vector2>[] _varRefV   = Enumerable.Range(0, 39).Select(_ => new Ref<Vector2>()).ToArray();

    // Simple mode: four base colors + rounding/border/spacing, auto-expanded into the full palette.
    private readonly Ref<bool>  _simpleMode  = new(false);
    private readonly Ref<Color> _sAccent     = new();
    private readonly Ref<Color> _sBg         = new();
    private readonly Ref<Color> _sText       = new();
    private readonly Ref<Color> _sBorder     = new();
    private readonly Ref<float> _sRounding   = new();
    private readonly Ref<float> _sBorderSize = new();
    private readonly Ref<float> _sSpacing    = new();

    private Group            _simpleGroup;
    private CollapsingHeader _colorsHdr;
    private CollapsingHeader _varsHdr;
    private bool             _loadingSimpleRefs;

    /// <summary>Fired whenever the active preset changes - on preset switch or any value edit.</summary>
    public event Action<StylePresetData> OnStyleChanged;

    public StylePresetData CurrentPreset => _currentPreset;

    public bool hasRefreshed;

    /// <summary>
    /// Apply a preset's colors and style vars to an OSWindow.
    /// Sets <see cref="OSWindow.Config"/> which immediately sends an update to the overlay.
    /// </summary>
    public static void ApplyToWindow(OSWindow window, StylePresetData preset)
    {
        var cfg = window.Config;
        cfg.GlobalStyle = preset.ToPushCommands();
        window.Config   = cfg;
    }

    /// <param name="name">Unique element ID.</param>
    /// <param name="folderPath">Folder for saving/loading custom preset JSON files. Created if absent.</param>
    public StyleEditor(string name, string folderPath) : base(name)
    {
        _folderPath   = folderPath;
        _selectedFile = Path.Combine(folderPath, "_selected.txt");
        Directory.CreateDirectory(folderPath);

        RefreshCustomList();
        BuildUI();

        var savedName  = File.Exists(_selectedFile) ? File.ReadAllText(_selectedFile).Trim() : null;
        var savedIndex = savedName != null ? Array.IndexOf(_comboItems.Value, savedName) : -1;
        
        if (savedIndex >= 0)
        {
            _comboIndex.Value = savedIndex;
            OnPresetSelected(savedIndex);
        }
        else
        {
            RequestBuiltIn(0);
        }
    }


    private void BuildUI()
    {
        var combo = new Combo($"Presets###{Name}-combo", _comboItems.Value, 0, OnPresetSelected)
            .WithSelectedIndex(_comboIndex)
            .WithItems(_comboItems);

        var saveInput = new InputText($"###{Name}-saveas", hint: "Preset name...", maxLength: 64)
            .WithValue(_saveAsName);

        _saveBtn = new Button($"Save###{Name}-save", OnSave);

        var newBtn = new Button($"New###{Name}-new", OnNew);

        _deleteBtn = new Button($"Delete###{Name}-del", OnDelete);

        _warningModal = new Modal($"{Name}-dup-modal", "Name Already Exists",
            new UIText($"{Name}-dup-text", "A preset with that name already exists. Please choose a different name."),
            new Button($"OK###{Name}-dup-ok", () => _warningModal.Close())
        ).WithNoClose();

        
        var colorElems = new BaseUIElement[60];
        
        for (int i = 0; i < 60; i++)
        {
            var col = (ImGuiCol)i;
            var ci  = i;
            
            colorElems[i] = new ColorEdit4($"{col}###{Name}-col-{i}",
                onChanged: c =>
                {
                    _currentPreset.Colors[ci * 4]     = c.r;
                    _currentPreset.Colors[ci * 4 + 1] = c.g;
                    _currentPreset.Colors[ci * 4 + 2] = c.b;
                    _currentPreset.Colors[ci * 4 + 3] = c.a;
                    FireChanged();
                }
            ).WithValue(_colorRefs[i]);
        }
        
        _colorsHdr = new CollapsingHeader($"{Name}-colors-hdr", "Colors", colorElems);


        var varElems = new List<BaseUIElement>();
        
        for (int i = 0; i < (int)ImGuiStyleVar.Count; i++)
        {
            var sv = (ImGuiStyleVar)i;
            var vi = i;

            if (Vec2StyleVars.Contains(sv))
            {
                var (vmin, vmax) = GetVec2Range(sv);
                
                varElems.Add(new DragFloat2($"{sv}###{Name}-v2-{i}",
                    speed: .01f,
                    min: vmin, max: vmax,
                    onValueChanged: v =>
                    {
                        _currentPreset.StyleVarValues[vi]  = v.x;
                        _currentPreset.StyleVarValuesY[vi] = v.y;
                        FireChanged();
                    }
                ).WithValue(_varRefV[i]));
            }
            else
            {
                var (fmin, fmax) = GetFloatRange(sv);
                
                varElems.Add(new DragFloat($"{sv}###{Name}-vf-{i}",
                    speed: .01f,
                    min: fmin, max: fmax,
                    onValueChanged: v =>
                    {
                        _currentPreset.StyleVarValues[vi] = v;
                        FireChanged();
                    }
                ).WithValue(_varRefF[i]));
            }
        }
        
        _varsHdr = new CollapsingHeader($"{Name}-vars-hdr", "Style Variables", [.. varElems]);


        // ---- Simple mode ------------------------------------------------------------------
        var simpleToggle = new Checkbox($"Simple Mode###{Name}-simple-toggle", false,
            onChanged: _ => UpdateModeVisibility())
            .WithValue(_simpleMode)
            .WithTooltip("Set a few base colors and sizes; the rest of the theme is generated for you.");

        _simpleGroup = new Group($"{Name}-simple-grp",
            new SeparatorText($"{Name}-simple-colsep", "Base Colors"),
            new ColorEdit3($"Accent###{Name}-s-accent",   onChanged: _ => ApplySimpleEdit()).WithValue(_sAccent)
                .WithTooltip("Buttons, checkmarks, sliders, selected tabs and other highlights."),
            new ColorEdit3($"Background###{Name}-s-bg",    onChanged: _ => ApplySimpleEdit()).WithValue(_sBg)
                .WithTooltip("Window background. Frames, title bars and popups are derived from it."),
            new ColorEdit3($"Text###{Name}-s-text",        onChanged: _ => ApplySimpleEdit()).WithValue(_sText),
            new ColorEdit3($"Border###{Name}-s-border",    onChanged: _ => ApplySimpleEdit()).WithValue(_sBorder),

            new SeparatorText($"{Name}-simple-valsep", "Sizes"),
            new DragFloat($"Rounding###{Name}-s-round", speed: .1f, min: 0f, max: 12f, format: "%.1f",
                onValueChanged: _ => ApplySimpleEdit()).WithValue(_sRounding)
                .WithTooltip("Corner rounding for windows, frames, scrollbars, grabs and tabs."),
            new DragFloat($"Border Size###{Name}-s-bordersize", speed: .05f, min: 0f, max: 3f, format: "%.2f",
                onValueChanged: _ => ApplySimpleEdit()).WithValue(_sBorderSize)
                .WithTooltip("Border thickness for windows, frames, popups and tabs."),
            new DragFloat($"Spacing###{Name}-s-spacing", speed: .1f, min: 0f, max: 20f, format: "%.1f",
                onValueChanged: _ => ApplySimpleEdit()).WithValue(_sSpacing)
                .WithTooltip("Base padding and spacing between and inside widgets.")
        );

        Add(
            combo,
            saveInput,
            new SameLine($"{Name}-sl1"),
            _saveBtn,
            new SameLine($"{Name}-sl2"),
            newBtn,
            new SameLine($"{Name}-sl3"),
            _deleteBtn,
            _warningModal,
            new SeparatorText($"{Name}-sep", ""),
            simpleToggle,
            _simpleGroup,
            _colorsHdr,
            _varsHdr
        );

        UpdateModeVisibility();
    }

    /// <summary>Show either the simple controls or the full color/var headers based on <see cref="_simpleMode"/>.</summary>
    private void UpdateModeVisibility()
    {
        var simple = _simpleMode.Value;
        if (simple) LoadSimpleIntoRefs();
        _simpleGroup?.SetVisible(simple);
        _colorsHdr?.SetVisible(!simple);
        _varsHdr?.SetVisible(!simple);
    }

    /// <summary>Push the current preset's simple params (or values derived from it) into the simple-mode refs.</summary>
    private void LoadSimpleIntoRefs()
    {
        var s = _currentPreset.Simple ?? DeriveSimpleFromPreset(_currentPreset);

        _loadingSimpleRefs = true;
        _sAccent.Value     = new Color(s.Accent[0],     s.Accent[1],     s.Accent[2]);
        _sBg.Value         = new Color(s.Background[0], s.Background[1], s.Background[2]);
        _sText.Value       = new Color(s.Text[0],       s.Text[1],       s.Text[2]);
        _sBorder.Value     = new Color(s.Border[0],     s.Border[1],     s.Border[2]);
        _sRounding.Value   = s.Rounding;
        _sBorderSize.Value = s.BorderSize;
        _sSpacing.Value    = s.Spacing;
        _loadingSimpleRefs = false;
    }

    /// <summary>
    /// A simple-mode control was edited: read the refs into the preset's <see cref="StylePresetData.Simple"/>,
    /// regenerate the full palette, mirror it into the advanced refs and fire the change.
    /// </summary>
    private void ApplySimpleEdit()
    {
        if (_loadingSimpleRefs) return;

        var s = _currentPreset.Simple ??= new SimpleStyleParams();
        var a = _sAccent.Value; var b = _sBg.Value; var t = _sText.Value; var bo = _sBorder.Value;
        s.Accent     = [a.r, a.g, a.b];
        s.Background = [b.r, b.g, b.b];
        s.Text       = [t.r, t.g, t.b];
        s.Border     = [bo.r, bo.g, bo.b];
        s.Rounding   = _sRounding.Value;
        s.BorderSize = _sBorderSize.Value;
        s.Spacing    = _sSpacing.Value;

        ApplySimpleToPreset(_currentPreset);
        LoadPresetIntoRefs(_currentPreset); // keep the advanced view in sync with the generated palette
        FireChanged();
    }


    public void OnPresetSelected(int idx)
    {
        hasRefreshed = true;
        _currentCustomIndex = idx >= BuiltInNames.Length ? idx - BuiltInNames.Length : -1;

        try { File.WriteAllText(_selectedFile, _comboItems.Value[idx]); } catch { /* ignore */ }
        
        var isCustom = _currentCustomIndex >= 0;
        _saveBtn?.SetDisabled(!isCustom);
        _deleteBtn?.SetDisabled(!isCustom);
        _saveAsName.Value = isCustom ? _customNames[_currentCustomIndex] : "";

        if (idx < BuiltInNames.Length)
        {
            RequestBuiltIn(idx);
            return;
        }

        _currentPreset = LoadFromFile(Path.Combine(_folderPath, _customNames[_currentCustomIndex] + ".json")) ?? new StylePresetData();
        LoadPresetIntoRefs(_currentPreset);
        if (_simpleMode.Value) LoadSimpleIntoRefs();
        FireChanged();
    }

    private void OnSave()
    {
        if (_currentCustomIndex < 0) return;
        var n = _saveAsName.Value?.Trim();
        if (string.IsNullOrEmpty(n) || BuiltInNames.Contains(n, StringComparer.OrdinalIgnoreCase)) return;

        var oldName = _customNames[_currentCustomIndex];
        var newPath = Path.Combine(_folderPath, n + ".json");

        if (!string.Equals(oldName, n, StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_folderPath, oldName + ".json");
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        File.WriteAllText(newPath, JsonConvert.SerializeObject(_currentPreset, Formatting.Indented));
        RefreshCustomList();

        var newIdx = Array.IndexOf(_comboItems.Value, n);
        if (newIdx >= 0)
        {
            _comboIndex.Value   = newIdx;
            _currentCustomIndex = newIdx - BuiltInNames.Length;
            try { File.WriteAllText(_selectedFile, n); } catch { }
        }
    }

    private void OnNew()
    {
        var n = _saveAsName.Value?.Trim();
        if (string.IsNullOrEmpty(n) || BuiltInNames.Contains(n, StringComparer.OrdinalIgnoreCase)) return;

        var path = Path.Combine(_folderPath, n + ".json");
        if (File.Exists(path))
        {
            _warningModal.Open();
            return;
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(_currentPreset, Formatting.Indented));
        RefreshCustomList();

        var newIdx = Array.IndexOf(_comboItems.Value, n);
        if (newIdx >= 0)
        {
            _comboIndex.Value   = newIdx;
            _currentCustomIndex = newIdx - BuiltInNames.Length;
            _saveBtn.SetDisabled(false);
            _deleteBtn.SetDisabled(false);
            try { File.WriteAllText(_selectedFile, n); } catch { }
        }
    }

    private void OnDelete()
    {
        if (_currentCustomIndex < 0 || _currentCustomIndex >= _customNames.Length) return;

        var file = Path.Combine(_folderPath, _customNames[_currentCustomIndex] + ".json");
        if (File.Exists(file)) File.Delete(file);

        RefreshCustomList();
        _comboIndex.Value   = 0;
        _currentCustomIndex = -1;
        OnPresetSelected(0);
    }

    
    private void RequestBuiltIn(int themeIndex)
    {
        string windowId;
        lock (UIManager.Windows)
        {
            var any = UIManager.Windows.Values.FirstOrDefault();
            if (any == null) return;
            windowId = any.Id;
        }

        UIManager.RequestStyleData(windowId, themeIndex, msg =>
        {
            var p = new StylePresetData
            {
                Colors          = msg.Colors,
                StyleVarValues  = msg.StyleVarValues,
                StyleVarValuesY = msg.StyleVarValuesY,
            };
            _currentPreset = p;
            LoadPresetIntoRefs(p);
            if (_simpleMode.Value) LoadSimpleIntoRefs();
            FireChanged();
        });
    }

    private void RefreshCustomList()
    {
        _customNames = Directory.GetFiles(_folderPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(n => n)
            .ToArray();
        _comboItems.Value = [.. BuiltInNames, .. _customNames];
    }

    private void LoadPresetIntoRefs(StylePresetData p)
    {
        for (int i = 0; i < 60; i++)
            _colorRefs[i].Value = new Color(p.Colors[i * 4], p.Colors[i * 4 + 1], p.Colors[i * 4 + 2], p.Colors[i * 4 + 3]);

        for (int i = 0; i < 39; i++)
        {
            var sv = (ImGuiStyleVar)i;
            if (Vec2StyleVars.Contains(sv))
                _varRefV[i].Value = new Vector2(p.StyleVarValues[i], p.StyleVarValuesY[i]);
            else
                _varRefF[i].Value = p.StyleVarValues[i];
        }
    }

    /// <summary>
    /// Re-applies the currently selected preset. Safe to call after the window is initialized.
    /// For built-in themes this re-requests style data from the overlay; for custom presets it
    /// re-fires OnStyleChanged with the already-loaded preset.
    /// </summary>
    public void ReapplyCurrentPreset()
    {
        if (_comboIndex.Value < BuiltInNames.Length)
            RequestBuiltIn(_comboIndex.Value);
        else
            FireChanged();
    }

    private void FireChanged() => OnStyleChanged?.Invoke(_currentPreset);

    private static StylePresetData LoadFromFile(string path)
    {
        try   { return JsonConvert.DeserializeObject<StylePresetData>(File.ReadAllText(path)); }
        catch { return null; }
    }

    // ---- Simple-mode palette generation -------------------------------------------------

    private static readonly ImGuiStyleVar[] RoundingVars =
    {
        ImGuiStyleVar.WindowRounding, ImGuiStyleVar.ChildRounding, ImGuiStyleVar.PopupRounding,
        ImGuiStyleVar.FrameRounding, ImGuiStyleVar.ScrollbarRounding, ImGuiStyleVar.GrabRounding,
        ImGuiStyleVar.TabRounding, ImGuiStyleVar.TreeLinesRounding,
    };

    private static readonly ImGuiStyleVar[] BorderVars =
    {
        ImGuiStyleVar.WindowBorderSize, ImGuiStyleVar.ChildBorderSize, ImGuiStyleVar.PopupBorderSize,
        ImGuiStyleVar.FrameBorderSize, ImGuiStyleVar.TabBorderSize, ImGuiStyleVar.SeparatorTextBorderSize,
    };

    /// <summary>
    /// Expand a preset's <see cref="StylePresetData.Simple"/> block into its full 60-color palette and
    /// the rounding/border/spacing style vars. Style vars not derived from simple mode are left untouched,
    /// so a preset seeded from a built-in theme keeps its scrollbar sizes, grab sizes, alignments, etc.
    /// </summary>
    public static void ApplySimpleToPreset(StylePresetData p)
    {
        var s = p.Simple;
        if (s == null) return;

        var accent = new Color(s.Accent[0],     s.Accent[1],     s.Accent[2]);
        var bg     = new Color(s.Background[0], s.Background[1], s.Background[2]);
        var text   = new Color(s.Text[0],       s.Text[1],       s.Text[2]);
        var border = new Color(s.Border[0],     s.Border[1],     s.Border[2]);

        // Dark backgrounds get "raised" surfaces by mixing toward white; light backgrounds toward black.
        var darkBg = (0.299f * bg.r + 0.587f * bg.g + 0.114f * bg.b) < 0.5f;
        Color Raise(Color c, float amt) => Color.Lerp(c, darkBg ? Color.white : Color.black, amt);
        Color Sink (Color c, float amt) => Color.Lerp(c, darkBg ? Color.black : Color.white, amt);
        Color Mix  (Color c, Color d, float t) => Color.Lerp(c, d, t);
        Color WithA(Color c, float a) => new Color(c.r, c.g, c.b, a);
        var clear = new Color(0f, 0f, 0f, 0f);

        void Set(ImGuiCol col, Color c)
        {
            var i = (int)col;
            p.Colors[i * 4]     = c.r;
            p.Colors[i * 4 + 1] = c.g;
            p.Colors[i * 4 + 2] = c.b;
            p.Colors[i * 4 + 3] = c.a;
        }

        Set(ImGuiCol.Text,                 WithA(text, 1f));
        Set(ImGuiCol.TextDisabled,         Mix(text, bg, 0.55f));
        Set(ImGuiCol.WindowBg,             WithA(bg, 1f));
        Set(ImGuiCol.ChildBg,              clear);
        Set(ImGuiCol.PopupBg,              WithA(Sink(bg, 0.15f), 0.98f));
        Set(ImGuiCol.Border,               WithA(border, 0.5f));
        Set(ImGuiCol.BorderShadow,         clear);
        Set(ImGuiCol.FrameBg,              Mix(bg, accent, 0.15f));
        Set(ImGuiCol.FrameBgHovered,       Mix(bg, accent, 0.28f));
        Set(ImGuiCol.FrameBgActive,        Mix(bg, accent, 0.40f));
        Set(ImGuiCol.TitleBg,              Sink(bg, 0.25f));
        Set(ImGuiCol.TitleBgActive,        Mix(bg, accent, 0.50f));
        Set(ImGuiCol.TitleBgCollapsed,     WithA(Sink(bg, 0.25f), 0.85f));
        Set(ImGuiCol.MenuBarBg,            Sink(bg, 0.10f));
        Set(ImGuiCol.ScrollbarBg,          WithA(Sink(bg, 0.20f), 0.6f));
        Set(ImGuiCol.ScrollbarGrab,        Raise(bg, 0.20f));
        Set(ImGuiCol.ScrollbarGrabHovered, Raise(bg, 0.32f));
        Set(ImGuiCol.ScrollbarGrabActive,  accent);
        Set(ImGuiCol.CheckMark,            accent);
        Set(ImGuiCol.SliderGrab,           accent);
        Set(ImGuiCol.SliderGrabActive,     Raise(accent, 0.2f));
        Set(ImGuiCol.Button,               WithA(accent, 0.55f));
        Set(ImGuiCol.ButtonHovered,        WithA(accent, 0.80f));
        Set(ImGuiCol.ButtonActive,         WithA(accent, 1.00f));
        Set(ImGuiCol.Header,               WithA(accent, 0.45f));
        Set(ImGuiCol.HeaderHovered,        WithA(accent, 0.70f));
        Set(ImGuiCol.HeaderActive,         WithA(accent, 0.90f));
        Set(ImGuiCol.Separator,            WithA(border, 0.5f));
        Set(ImGuiCol.SeparatorHovered,     WithA(accent, 0.7f));
        Set(ImGuiCol.SeparatorActive,      accent);
        Set(ImGuiCol.ResizeGrip,           WithA(accent, 0.20f));
        Set(ImGuiCol.ResizeGripHovered,    WithA(accent, 0.55f));
        Set(ImGuiCol.ResizeGripActive,     WithA(accent, 0.90f));
        Set(ImGuiCol.InputTextCursor,      WithA(text, 1f));
        Set(ImGuiCol.TabHovered,           WithA(accent, 0.80f));
        Set(ImGuiCol.Tab,                  Mix(bg, accent, 0.30f));
        Set(ImGuiCol.TabSelected,          Mix(bg, accent, 0.55f));
        Set(ImGuiCol.TabSelectedOverline,  accent);
        Set(ImGuiCol.TabDimmed,            Mix(bg, accent, 0.12f));
        Set(ImGuiCol.TabDimmedSelected,    Mix(bg, accent, 0.28f));
        Set(ImGuiCol.TabDimmedSelectedOverline, WithA(accent, 0.5f));
        Set(ImGuiCol.DockingPreview,       WithA(accent, 0.7f));
        Set(ImGuiCol.DockingEmptyBg,       Sink(bg, 0.30f));
        Set(ImGuiCol.PlotLines,            WithA(text, 0.8f));
        Set(ImGuiCol.PlotLinesHovered,     accent);
        Set(ImGuiCol.PlotHistogram,        WithA(accent, 0.9f));
        Set(ImGuiCol.PlotHistogramHovered, Raise(accent, 0.2f));
        Set(ImGuiCol.TableHeaderBg,        Mix(bg, accent, 0.20f));
        Set(ImGuiCol.TableBorderStrong,    WithA(border, 0.7f));
        Set(ImGuiCol.TableBorderLight,     WithA(border, 0.4f));
        Set(ImGuiCol.TableRowBg,           clear);
        Set(ImGuiCol.TableRowBgAlt,        WithA(Raise(bg, 0.04f), 1f));
        Set(ImGuiCol.TextLink,             accent);
        Set(ImGuiCol.TextSelectedBg,       WithA(accent, 0.35f));
        Set(ImGuiCol.TreeLines,            WithA(border, 0.5f));
        Set(ImGuiCol.DragDropTarget,       WithA(accent, 0.90f));
        Set(ImGuiCol.NavCursor,            accent);
        Set(ImGuiCol.NavWindowingHighlight, WithA(text, 0.7f));
        Set(ImGuiCol.NavWindowingDimBg,    new Color(0.20f, 0.20f, 0.20f, 0.20f));
        Set(ImGuiCol.ModalWindowDimBg,     new Color(0.05f, 0.05f, 0.05f, 0.50f));

        foreach (var v in RoundingVars) p.StyleVarValues[(int)v] = s.Rounding;
        foreach (var v in BorderVars)   p.StyleVarValues[(int)v] = s.BorderSize;

        void SetVec2(ImGuiStyleVar v, float x, float y)
        {
            p.StyleVarValues[(int)v]  = x;
            p.StyleVarValuesY[(int)v] = y;
        }

        var sp = s.Spacing;
        SetVec2(ImGuiStyleVar.FramePadding,     sp,          Mathf.Round(sp * 0.5f));
        SetVec2(ImGuiStyleVar.ItemSpacing,      sp,          Mathf.Round(sp * 0.6f));
        SetVec2(ImGuiStyleVar.WindowPadding,    sp,          sp);
        SetVec2(ImGuiStyleVar.ItemInnerSpacing, Mathf.Round(sp * 0.6f), Mathf.Round(sp * 0.6f));
        SetVec2(ImGuiStyleVar.CellPadding,      sp,          Mathf.Round(sp * 0.5f));
    }

    /// <summary>
    /// Read a plausible set of simple-mode inputs back out of an already-populated preset, so switching
    /// a built-in or advanced-authored preset into simple mode starts from sensible slider values.
    /// </summary>
    public static SimpleStyleParams DeriveSimpleFromPreset(StylePresetData p)
    {
        float[] Col(ImGuiCol col)
        {
            var i = (int)col;
            return [p.Colors[i * 4], p.Colors[i * 4 + 1], p.Colors[i * 4 + 2]];
        }

        return new SimpleStyleParams
        {
            Accent     = Col(ImGuiCol.CheckMark),
            Background = Col(ImGuiCol.WindowBg),
            Text       = Col(ImGuiCol.Text),
            Border     = Col(ImGuiCol.Border),
            Rounding   = p.StyleVarValues[(int)ImGuiStyleVar.FrameRounding],
            BorderSize = p.StyleVarValues[(int)ImGuiStyleVar.WindowBorderSize],
            Spacing    = p.StyleVarValues[(int)ImGuiStyleVar.FramePadding],
        };
    }

    private static (float min, float max) GetFloatRange(ImGuiStyleVar sv) => sv switch
    {
        ImGuiStyleVar.Alpha or ImGuiStyleVar.DisabledAlpha
            => (0.1f, 1f),
        ImGuiStyleVar.WindowRounding or ImGuiStyleVar.ChildRounding or ImGuiStyleVar.PopupRounding
            or ImGuiStyleVar.FrameRounding or ImGuiStyleVar.ScrollbarRounding or ImGuiStyleVar.GrabRounding
            or ImGuiStyleVar.TabRounding or ImGuiStyleVar.TreeLinesRounding
            => (0f, 12f),
        ImGuiStyleVar.WindowBorderSize or ImGuiStyleVar.ChildBorderSize or ImGuiStyleVar.PopupBorderSize
            or ImGuiStyleVar.FrameBorderSize or ImGuiStyleVar.TabBorderSize or ImGuiStyleVar.TabBarBorderSize
            or ImGuiStyleVar.TabBarOverlineSize or ImGuiStyleVar.ImageBorderSize
            or ImGuiStyleVar.SeparatorTextBorderSize or ImGuiStyleVar.DockingSeparatorSize
            => (0f, 4f),
        ImGuiStyleVar.IndentSpacing
            => (0f, 40f),
        ImGuiStyleVar.ScrollbarSize or ImGuiStyleVar.GrabMinSize or ImGuiStyleVar.TreeLinesSize
            => (1f, 32f),
        ImGuiStyleVar.TabMinWidthBase or ImGuiStyleVar.TabMinWidthShrink
            => (0f, 300f),
        ImGuiStyleVar.TableAngledHeadersAngle
            => (0f, 1.5708f),
        _ => (0f, 1f),
    };

    private static (float min, float max) GetVec2Range(ImGuiStyleVar sv) => sv switch
    {
        ImGuiStyleVar.WindowPadding or ImGuiStyleVar.FramePadding or ImGuiStyleVar.ItemSpacing
            or ImGuiStyleVar.ItemInnerSpacing or ImGuiStyleVar.CellPadding
            => (0f, 32f),
        ImGuiStyleVar.WindowMinSize
            => (0f, 200f),
        ImGuiStyleVar.SeparatorTextPadding
            => (0f, 40f),
        _ => (0f, 1f)
    };
}
