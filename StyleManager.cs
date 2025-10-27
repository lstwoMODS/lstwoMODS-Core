using System;
using System.Collections.Generic;
using System.IO;
using ImGuiNET;
using Newtonsoft.Json;
using UImGui.Assets;
using UnityEngine;

namespace lstwoMODS_Core;


public class StyleManager
{
    [Serializable]
    public struct Vec2 { public float x, y; public static implicit operator Vec2(Vector2 v) => new Vec2 { x = v.x, y = v.y }; public static implicit operator Vector2(Vec2 v) => new Vector2(v.x, v.y); }
    [Serializable]
    public struct Col { public float r, g, b, a; public static implicit operator Col(Color c) => new Col { r = c.r, g = c.g, b = c.b, a = c.a }; public static implicit operator Color(Col c) => new Color(c.r, c.g, c.b, c.a); }
    
    [Serializable]
    public class NamedColor
    {
        public string name;
        public Col color;
    }

    [Serializable]
    public class StyleAssetData
    {
        public float Alpha;
        public Vec2 WindowPadding;
        public float WindowRounding;
        public float WindowBorderSize;
        public Vec2 WindowMinSize;
        public Vec2 WindowTitleAlign;
        public ImGuiDir WindowMenuButtonPosition;
        public float ChildRounding;
        public float ChildBorderSize;
        public float PopupRounding;
        public float PopupBorderSize;
        public Vec2 FramePadding;
        public float FrameRounding;
        public float FrameBorderSize;
        public Vec2 ItemSpacing;
        public Vec2 ItemInnerSpacing;
        public Vec2 CellPadding;
        public Vec2 TouchExtraPadding;
        public float IndentSpacing;
        public float ColumnsMinSpacing;
        public float ScrollbarSize;
        public float ScrollbarRounding;
        public float GrabMinSize;
        public float GrabRounding;
        public float LogSliderDeadzone;
        public float TabRounding;
        public float TabBorderSize;
        public ImGuiDir ColorButtonPosition;
        public Vec2 ButtonTextAlign;
        public Vec2 SelectableTextAlign;
        public Vec2 DisplayWindowPadding;
        public Vec2 DisplaySafeAreaPadding;
        public float MouseCursorScale;
        public bool AntiAliasedLines;
        public bool AntiAliasedLinesUseTex;
        public bool AntiAliasedFill;
        public float CurveTessellationTol;
        public float CircleTessellationMaxError;
        public List<NamedColor> Colors;
    }

    public static void SaveToJson(StyleAsset styleAsset, string filePath)
    {
        if (styleAsset == null)
        {
            return;
        }

        var data = new StyleAssetData
        {
            Alpha = styleAsset.Alpha,
            WindowPadding = styleAsset.WindowPadding,
            WindowRounding = styleAsset.WindowRounding,
            WindowBorderSize = styleAsset.WindowBorderSize,
            WindowMinSize = styleAsset.WindowMinSize,
            WindowTitleAlign = styleAsset.WindowTitleAlign,
            WindowMenuButtonPosition = styleAsset.WindowMenuButtonPosition,
            ChildRounding = styleAsset.ChildRounding,
            ChildBorderSize = styleAsset.ChildBorderSize,
            PopupRounding = styleAsset.PopupRounding,
            PopupBorderSize = styleAsset.PopupBorderSize,
            FramePadding = styleAsset.FramePadding,
            FrameRounding = styleAsset.FrameRounding,
            FrameBorderSize = styleAsset.FrameBorderSize,
            ItemSpacing = styleAsset.ItemSpacing,
            ItemInnerSpacing = styleAsset.ItemInnerSpacing,
            CellPadding = styleAsset.CellPadding,
            TouchExtraPadding = styleAsset.TouchExtraPadding,
            IndentSpacing = styleAsset.IndentSpacing,
            ColumnsMinSpacing = styleAsset.ColumnsMinSpacing,
            ScrollbarSize = styleAsset.ScrollbarSize,
            ScrollbarRounding = styleAsset.ScrollbarRounding,
            GrabMinSize = styleAsset.GrabMinSize,
            GrabRounding = styleAsset.GrabRounding,
            LogSliderDeadzone = styleAsset.LogSliderDeadzone,
            TabRounding = styleAsset.TabRounding,
            TabBorderSize = styleAsset.TabBorderSize,
            ColorButtonPosition = styleAsset.ColorButtonPosition,
            ButtonTextAlign = styleAsset.ButtonTextAlign,
            SelectableTextAlign = styleAsset.SelectableTextAlign,
            DisplayWindowPadding = styleAsset.DisplayWindowPadding,
            DisplaySafeAreaPadding = styleAsset.DisplaySafeAreaPadding,
            MouseCursorScale = styleAsset.MouseCursorScale,
            AntiAliasedLines = styleAsset.AntiAliasedLines,
            AntiAliasedLinesUseTex = styleAsset.AntiAliasedLinesUseTex,
            AntiAliasedFill = styleAsset.AntiAliasedFill,
            CurveTessellationTol = styleAsset.CurveTessellationTol,
            CircleTessellationMaxError = styleAsset.CircleTessellationMaxError,
            Colors = new List<NamedColor>()
        };
        
        for (var i = 0; i < (int)ImGuiCol.COUNT; i++)
        {
            var colorName = ImGui.GetStyleColorName((ImGuiCol)i);
            data.Colors.Add(new NamedColor
            {
                name = colorName,
                color = styleAsset.Colors[i]
            });
        }
        
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static void LoadFromJson(StyleAsset styleAsset, string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var json = File.ReadAllText(filePath);
        var data = JsonConvert.DeserializeObject<StyleAssetData>(json);
        
        if (data == null)
        {
            return;
        }

        styleAsset.Alpha = data.Alpha;
        styleAsset.WindowPadding = data.WindowPadding;
        styleAsset.WindowRounding = data.WindowRounding;
        styleAsset.WindowBorderSize = data.WindowBorderSize;
        styleAsset.WindowMinSize = data.WindowMinSize;
        styleAsset.WindowTitleAlign = data.WindowTitleAlign;
        styleAsset.WindowMenuButtonPosition = data.WindowMenuButtonPosition;
        styleAsset.ChildRounding = data.ChildRounding;
        styleAsset.ChildBorderSize = data.ChildBorderSize;
        styleAsset.PopupRounding = data.PopupRounding;
        styleAsset.PopupBorderSize = data.PopupBorderSize;
        styleAsset.FramePadding = data.FramePadding;
        styleAsset.FrameRounding = data.FrameRounding;
        styleAsset.FrameBorderSize = data.FrameBorderSize;
        styleAsset.ItemSpacing = data.ItemSpacing;
        styleAsset.ItemInnerSpacing = data.ItemInnerSpacing;
        styleAsset.CellPadding = data.CellPadding;
        styleAsset.TouchExtraPadding = data.TouchExtraPadding;
        styleAsset.IndentSpacing = data.IndentSpacing;
        styleAsset.ColumnsMinSpacing = data.ColumnsMinSpacing;
        styleAsset.ScrollbarSize = data.ScrollbarSize;
        styleAsset.ScrollbarRounding = data.ScrollbarRounding;
        styleAsset.GrabMinSize = data.GrabMinSize;
        styleAsset.GrabRounding = data.GrabRounding;
        styleAsset.LogSliderDeadzone = data.LogSliderDeadzone;
        styleAsset.TabRounding = data.TabRounding;
        styleAsset.TabBorderSize = data.TabBorderSize;
        styleAsset.ColorButtonPosition = data.ColorButtonPosition;
        styleAsset.ButtonTextAlign = data.ButtonTextAlign;
        styleAsset.SelectableTextAlign = data.SelectableTextAlign;
        styleAsset.DisplayWindowPadding = data.DisplayWindowPadding;
        styleAsset.DisplaySafeAreaPadding = data.DisplaySafeAreaPadding;
        styleAsset.MouseCursorScale = data.MouseCursorScale;
        styleAsset.AntiAliasedLines = data.AntiAliasedLines;
        styleAsset.AntiAliasedLinesUseTex = data.AntiAliasedLinesUseTex;
        styleAsset.AntiAliasedFill = data.AntiAliasedFill;
        styleAsset.CurveTessellationTol = data.CurveTessellationTol;
        styleAsset.CircleTessellationMaxError = data.CircleTessellationMaxError;

        if (data.Colors == null)
        {
            return;
        }
        
        for (var i = 0; i < Mathf.Min(data.Colors.Count, styleAsset.Colors.Length); i++)
        {
            styleAsset.Colors[i] = data.Colors[i].color;
        }
    }
}