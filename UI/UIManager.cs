using System;
using System.IO;
using ImGuiNET;
using UImGui.Assets;

namespace lstwoMODS_Core.UI;

public static class UIManager
{
    public static Func<StyleAsset, Action<ImGuiIOPtr>, UImGui.UImGui> createImGuiContext;

    private static StyleAsset defaultStyle;
    
    public static UImGui.UImGui CreateImGuiContext(StyleAsset style, Action<ImGuiIOPtr> customFontInit = null, bool useLstwoModsStyle = true)
    {
        if (useLstwoModsStyle)
        {
            defaultStyle ??= LoadStyle();
            style = defaultStyle;
        }
        
        var uimgui = createImGuiContext(style, customFontInit);
        Plugin.AllImGuiRenderers.Add(uimgui);
        
        return uimgui;
    }

    private static StyleAsset LoadStyle()
    {
        var folderPath = @$"{AppDomain.CurrentDomain.BaseDirectory}\lstwoMODS\style";
        var styleAsset = Plugin.UImGuiBundle.LoadAsset<StyleAsset>("lstwoMODS uImGui Style");

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        var styleFilePath = $@"{folderPath}\style.json";

        if (File.Exists(styleFilePath))
        {
            StyleManager.LoadFromJson(styleAsset, styleFilePath);
        }

        var templateStylePath = $@"{folderPath}\template.json";
        
        StyleManager.SaveToJson(styleAsset, templateStylePath);
        
        File.WriteAllText($@"{folderPath}\README.txt", 
            "The template.json contains the default style parameters from lstwoMODS " +
            "and will get automatically updated with each launch. " +
            "Duplicate the file to change the parameters. " +
            "The mod will look for a style.json file in this folder on every launch.");

        return styleAsset;
    }
}