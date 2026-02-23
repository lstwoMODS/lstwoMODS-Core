using System;
using System.Collections.Generic;
using System.IO;
using Hexa.NET.ImGui;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace lstwoMODS_Core;

public static class StyleManager
{
    [Serializable]
    public class NamedColor
    {
        public string name;
        public Col color;
    }

    [Serializable]
    public class StyleData
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

    public static void SaveToJson(string filePath, ImGuiStylePtr s)
    {
        var data = new StyleData
        {
            Alpha = s.Alpha,
            WindowPadding = s.WindowPadding,
            WindowRounding = s.WindowRounding,
            WindowBorderSize = s.WindowBorderSize,
            WindowMinSize = s.WindowMinSize,
            WindowTitleAlign = s.WindowTitleAlign,
            WindowMenuButtonPosition = s.WindowMenuButtonPosition,

            ChildRounding = s.ChildRounding,
            ChildBorderSize = s.ChildBorderSize,
            PopupRounding = s.PopupRounding,
            PopupBorderSize = s.PopupBorderSize,

            FramePadding = s.FramePadding,
            FrameRounding = s.FrameRounding,
            FrameBorderSize = s.FrameBorderSize,

            ItemSpacing = s.ItemSpacing,
            ItemInnerSpacing = s.ItemInnerSpacing,
            CellPadding = s.CellPadding,
            TouchExtraPadding = s.TouchExtraPadding,

            IndentSpacing = s.IndentSpacing,
            ColumnsMinSpacing = s.ColumnsMinSpacing,

            ScrollbarSize = s.ScrollbarSize,
            ScrollbarRounding = s.ScrollbarRounding,
            GrabMinSize = s.GrabMinSize,
            GrabRounding = s.GrabRounding,

            LogSliderDeadzone = s.LogSliderDeadzone,

            TabRounding = s.TabRounding,
            TabBorderSize = s.TabBorderSize,

            ColorButtonPosition = s.ColorButtonPosition,
            ButtonTextAlign = s.ButtonTextAlign,
            SelectableTextAlign = s.SelectableTextAlign,

            DisplayWindowPadding = s.DisplayWindowPadding,
            DisplaySafeAreaPadding = s.DisplaySafeAreaPadding,

            MouseCursorScale = s.MouseCursorScale,

            AntiAliasedLines = s.AntiAliasedLines,
            AntiAliasedLinesUseTex = s.AntiAliasedLinesUseTex,
            AntiAliasedFill = s.AntiAliasedFill,

            CurveTessellationTol = s.CurveTessellationTol,
            CircleTessellationMaxError = s.CircleTessellationMaxError,

            Colors = new List<NamedColor>()
        };

        for (var i = 0; i < (int)ImGuiCol.Count; i++)
        {
            unsafe
            {
                var namePtr = ImGui.GetStyleColorName((ImGuiCol)i);
                var name = Marshal.PtrToStringAnsi((IntPtr)namePtr);
                
                data.Colors.Add(new NamedColor
                {
                    name = name,
                    color = s.Colors[i]
                });
            }
        }

        File.WriteAllText(
            filePath,
            JsonConvert.SerializeObject(data, Formatting.Indented)
        );
    }


    public static bool LoadFromJson(string filePath)
    {
        if (!File.Exists(filePath))
            return false;

        var data = JsonConvert.DeserializeObject<StyleData>(
            File.ReadAllText(filePath)
        );

        if (data == null)
            return false;

        var s = ImGui.GetStyle();

        s.Alpha = data.Alpha;
        s.WindowPadding = data.WindowPadding;
        s.WindowRounding = data.WindowRounding;
        s.WindowBorderSize = data.WindowBorderSize;
        s.WindowMinSize = data.WindowMinSize;
        s.WindowTitleAlign = data.WindowTitleAlign;
        s.WindowMenuButtonPosition = data.WindowMenuButtonPosition;

        s.ChildRounding = data.ChildRounding;
        s.ChildBorderSize = data.ChildBorderSize;
        s.PopupRounding = data.PopupRounding;
        s.PopupBorderSize = data.PopupBorderSize;

        s.FramePadding = data.FramePadding;
        s.FrameRounding = data.FrameRounding;
        s.FrameBorderSize = data.FrameBorderSize;

        s.ItemSpacing = data.ItemSpacing;
        s.ItemInnerSpacing = data.ItemInnerSpacing;
        s.CellPadding = data.CellPadding;
        s.TouchExtraPadding = data.TouchExtraPadding;

        s.IndentSpacing = data.IndentSpacing;
        s.ColumnsMinSpacing = data.ColumnsMinSpacing;

        s.ScrollbarSize = data.ScrollbarSize;
        s.ScrollbarRounding = data.ScrollbarRounding;
        s.GrabMinSize = data.GrabMinSize;
        s.GrabRounding = data.GrabRounding;

        s.LogSliderDeadzone = data.LogSliderDeadzone;

        s.TabRounding = data.TabRounding;
        s.TabBorderSize = data.TabBorderSize;

        s.ColorButtonPosition = data.ColorButtonPosition;
        s.ButtonTextAlign = data.ButtonTextAlign;
        s.SelectableTextAlign = data.SelectableTextAlign;

        s.DisplayWindowPadding = data.DisplayWindowPadding;
        s.DisplaySafeAreaPadding = data.DisplaySafeAreaPadding;

        s.MouseCursorScale = data.MouseCursorScale;

        s.AntiAliasedLines = data.AntiAliasedLines;
        s.AntiAliasedLinesUseTex = data.AntiAliasedLinesUseTex;
        s.AntiAliasedFill = data.AntiAliasedFill;

        s.CurveTessellationTol = data.CurveTessellationTol;
        s.CircleTessellationMaxError = data.CircleTessellationMaxError;

        if (data.Colors == null)
            return false;

        for (var i = 0; i < Math.Min(data.Colors.Count, (int)ImGuiCol.Count); i++)
        {
            s.Colors[i] = data.Colors[i].color;
        }

        return true;
    }
}
