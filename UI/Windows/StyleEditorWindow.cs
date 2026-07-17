using System.IO;
using BepInEx;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus;

public class StyleEditorWindow : BaseWindow
{
    private StyleEditor _styleEditor;

    public StyleEditorWindow()
    {
        Name = "Style Editor";
        TitleIcon = Lucide.Palette;
    }

    public override Group ConstructUI()
    {
        _styleEditor = new StyleEditor(
            "lstwoMODS StyleEditor",
            Path.Combine(Paths.GameRootPath, "lstwoMODS", "Styles")
        );

        _styleEditor.OnStyleChanged += preset =>
        {
            if (Plugin.Window != null)
                StyleEditor.ApplyToWindow(Plugin.Window, preset);
        };

        return new Group("StyleEditor", _styleEditor);
    }

    public void ApplyCurrentPreset() => _styleEditor?.ReapplyCurrentPreset();

    public override void RefreshUI()
    {
        if(!_styleEditor.hasRefreshed)
            _styleEditor.OnPresetSelected(0);
    }
}