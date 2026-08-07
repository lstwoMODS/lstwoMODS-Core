using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public abstract class BaseUIElementData
    {
        private static int _lastId;

        public int    Id      { get; set; }
        public string Name    { get; set; } = "";
        public bool             Enabled      { get; set; } = true;
        public RequireInputMode RequireInput { get; set; } = RequireInputMode.Inherit;

        public List<PushCommand> PushCommands { get; set; } = new List<PushCommand>();

        /// <summary>Tooltip text shown on hover. Null/empty = no tooltip.</summary>
        public string             Tooltip             { get; set; } = null;
        /// <summary>
        /// Controls when the tooltip appears. None = use SetItemTooltip (respects style delays).
        /// Set e.g. ImGuiHoveredFlags.DelayShort for an explicit delay.
        /// </summary>
        public ImGuiHoveredFlags  TooltipHoveredFlags { get; set; } = ImGuiHoveredFlags.None;

        public BaseUIElementData()
        {
            Id = _lastId++;
        }
    }
}