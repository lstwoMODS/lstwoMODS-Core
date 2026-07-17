using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.Hacks;
using lstwoMODS_Core.UI.Elements;

namespace lstwoMODS_Core.UI.TabMenus
{
    public class ModsWindow : BaseWindow
    {
        public List<BaseMod> Mods = new();

        private const string OrderStorageId     = "lstwoMODS_Core";
        private const string ReorderPayloadType = "LSTWO_MOD_REORDER";

        private static readonly List<ModsWindow> _instances = new();

        private readonly Dictionary<BaseMod, Ref<bool>> _modVisibility  = new();
        private readonly Dictionary<BaseMod, CollapsingHeader> _modHeaders = new();

        private Group _modListGroup;
        private List<BaseMod> _registrationOrder;

        public ModsWindow(string name = "Mods", string icon = "")
        {
            Name = name;
            TitleIcon = icon;
            _instances.Add(this);
        }

        private string OrderStorageKey => "mod_order/" + Name;

        private Ref<bool> GetVisibilityRef(BaseMod mod)
        {
            if (!_modVisibility.TryGetValue(mod, out var r))
                _modVisibility[mod] = r = new Ref<bool>(true);
            return r;
        }

        private CollapsingHeader ConstructModUI(BaseMod mod)
        {
            var typeName = mod.GetType().FullName;

            var header = new CollapsingHeader(typeName, mod.Name,
                mod.BuildPanel(mod.GetType().Name)
            ).WithId("MOD " + typeName)
             .WithVisible(GetVisibilityRef(mod))
             .WithDragSource(ReorderPayloadType, typeName, mod.Name)
             .WithDropTarget((_, payload, below) => MoveMod(payload, mod, below), ReorderPayloadType)
             .OnToggle(open =>
             {
                 if (open)
                 {
                     try { mod.RefreshUI(); }
                     catch (Exception e) { Plugin.LogSource.LogError($"Error Refreshing Mod ({mod.Name}): {e.Message} {e.StackTrace}"); }
                 }
             });

            if (!string.IsNullOrEmpty(mod.Description))
            {
                header.WithTooltip(mod.Description);
            }

            _modHeaders[mod] = header;
            return header;
        }

        private void ApplySearch(string query)
        {
            var lower = query?.ToLowerInvariant() ?? "";
            foreach (var (mod, visibility) in _modVisibility)
            {
                visibility.Value = string.IsNullOrEmpty(lower)
                    || mod.Name.ToLowerInvariant().Contains(lower)
                    || (mod.Description ?? "").ToLowerInvariant().Contains(lower);
            }
        }

        /// <summary>
        /// Move the dragged mod (identified by type full name) directly above or below
        /// <paramref name="target"/>. Updates the UI order and persists it.
        /// </summary>
        private void MoveMod(string draggedTypeName, BaseMod target, bool below)
        {
            var dragged = Mods.FirstOrDefault(m => m.GetType().FullName == draggedTypeName);
            if (dragged == null || dragged == target) return;

            Mods.Remove(dragged);
            var index = Mods.IndexOf(target);
            if (index < 0) index = Mods.Count;
            else if (below) index++;
            Mods.Insert(index, dragged);

            ApplyModOrderToUI();
            DataStorage.Save(OrderStorageId, OrderStorageKey,
                Mods.Select(m => m.GetType().FullName).ToList());
        }

        private void ApplyModOrderToUI()
        {
            if (_modListGroup == null) return;

            var ordered = Mods
                .Where(_modHeaders.ContainsKey)
                .Select(BaseUIElement (m) => _modHeaders[m])
                .ToList();

            _modListGroup.SetChildOrder(ordered);
        }

        /// <summary>Sort <see cref="Mods"/> by the order saved from a previous session.
        /// Mods not in the saved list (e.g. newly installed) keep registration order at the end.</summary>
        private void ApplySavedOrder()
        {
            var saved = DataStorage.Load<List<string>>(OrderStorageId, OrderStorageKey);
            if (saved == null || saved.Count == 0) return;

            var indexByName = new Dictionary<string, int>();
            for (var i = 0; i < saved.Count; i++)
            {
                if (!indexByName.ContainsKey(saved[i]))
                    indexByName[saved[i]] = i;
            }

            Mods = Mods
                .OrderBy(m => indexByName.TryGetValue(m.GetType().FullName, out var i) ? i : int.MaxValue)
                .ToList();
        }

        /// <summary>
        /// Discard the saved custom order of every mods window and restore registration order.
        /// </summary>
        public static void ResetAllOrders()
        {
            foreach (var window in _instances)
                window.ResetOrder();
        }

        /// <summary>Discard this window's saved custom order and restore registration order.</summary>
        public void ResetOrder()
        {
            DataStorage.Delete(OrderStorageId, OrderStorageKey);

            if (_registrationOrder != null)
            {
                var ordered = _registrationOrder.Where(Mods.Contains).ToList();
                ordered.AddRange(Mods.Where(m => !ordered.Contains(m)));
                Mods = ordered;
            }

            ApplyModOrderToUI();
        }

        public override Group ConstructUI()
        {
            // Capture the pristine registration order once, before any saved order is
            // applied, so ResetOrder can restore it.
            _registrationOrder ??= new List<BaseMod>(Mods);

            ApplySavedOrder();

            var headers = Mods.Select(BaseUIElement (mod) =>
            {
                try
                {
                    return ConstructModUI(mod);
                }
                catch (Exception e)
                {
                    Plugin.LogSource.LogError($"Error Rendering Mod UI ({mod.Name}): {e.Message} {e.StackTrace}");
                    return null;
                }
            }).Where(x => x != null).ToArray();

            _modListGroup = new Group("ModList_" + Name, headers);

            var searchBar = new InputText("Search Mod", hint: "Search mods...", onChanged: ApplySearch, mainThread: false);

            return new Group("Tab_" + Name, searchBar, _modListGroup);
        }

        public override void RefreshUI()
        {
            foreach (var (mod, header) in _modHeaders)
            {
                if (header?.Data is not CollapsingHeaderData data || !data.IsOpen) continue;

                try { mod.RefreshUI(); }
                catch (Exception e) { Plugin.LogSource.LogError($"Error Refreshing Mod ({mod.Name}): {e.Message} {e.StackTrace}"); }
            }
        }
    }
}
