using System;
using Hexa.NET.ImGui;
using UnityEngine;

namespace lstwoMODS_Core.UI;

public static class ModsUIHelper
{
    public static void CenteredSeparator(float width = 0f)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorPos();
        var contentWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;

        var x1 = cursorPos.X;
        var x2 = x1 + contentWidth;

        var y = cursorPos.Y + ImGui.GetTextLineHeight() / 2f;

        ImGui.Dummy(new Vec2(contentWidth, 1));

        drawList.AddLine(
            new Vec2(x1 + ImGui.GetWindowPos().X, y + ImGui.GetWindowPos().Y),
            new Vec2(x2 + ImGui.GetWindowPos().X, y + ImGui.GetWindowPos().Y),
            ImGui.GetColorU32(ImGuiCol.Border)
        );
    }

    public static void SameLineSeparator(float width = 0f)
    {
        ImGui.SameLine();
        CenteredSeparator(width);
    }

    public static void PreSeparator(float width = 0f)
    {
        CenteredSeparator(width);
        ImGui.SameLine();
    }

    public static void TextSeparator(string text, float preWidth = 10f)
    {
        PreSeparator(preWidth);
        ImGui.Text(text);
        SameLineSeparator();
    }
    
    public static bool ConfirmDialog(string id, string message, ref bool opened, Action onCancel = null)
    {
        if (!ImGui.BeginPopupModal(id, ref opened, ImGuiWindowFlags.AlwaysAutoResize))
        {
            return false;
        }
        
        ImGui.Text(message);
        ImGui.Separator();

        if (ImGui.Button("Confirm", new Vec2(120, 0)))
        {
            ImGui.CloseCurrentPopup();
            return true;
        }

        ImGui.SameLine();

        if (ImGui.Button("Cancel", new Vec2(120, 0)))
        {
            onCancel?.Invoke();
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
        
        return false;
    }
}