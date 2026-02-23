using System.Collections.Generic;

namespace lstwoMODS.ImGui.Shared.UI
{
    public abstract class BaseUiElement
    {
        private static int _lastId;
        
        public int Id { get; private set; }
        public List<BaseUiElement> Children { get; } = new List<BaseUiElement>();

        public BaseUiElement()
        {
            Id = _lastId++;
        }
    }
}