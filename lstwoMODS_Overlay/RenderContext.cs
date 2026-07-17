using System.Collections.Generic;

namespace lstwoMODS_Overlay;

/// <summary>
/// Lightweight per-frame context that renderers can read during a render pass.
/// All rendering happens on a single thread, so no locking is needed.
/// Use Push/Pop pairs so nested containers work correctly.
/// </summary>
public static class RenderContext
{
    private static readonly Stack<float> _slotWidthStack = new Stack<float>();

    /// <summary>
    /// Width (px) of the slot currently being rendered by a width-distributing container
    /// (e.g. HStack). 0 when not inside such a slot.
    /// Renderers that do not respect PushItemWidth (e.g. Button) use this to fill their slot.
    /// </summary>
    public static float SlotWidth => _slotWidthStack.Count > 0 ? _slotWidthStack.Peek() : 0f;

    public static void PushSlotWidth(float width) => _slotWidthStack.Push(width);
    public static void PopSlotWidth()              => _slotWidthStack.Pop();
}
