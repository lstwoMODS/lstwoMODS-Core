using System;
using System.Collections;
using System.Collections.Concurrent;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.Elements;
using lstwoMODS_Core.UI.TabMenus;
using UnityEngine;

namespace lstwoMODS_Core.Hacks
{
    public abstract class BaseMod
    {
        private ModExecutionContext _executionContext;

        /// <summary>
        /// Supply runtime context values (e.g. a player, team, or session) before invoking
        /// actions or reading settings externally. The types required by this mod class are
        /// declared via <see cref="ModContextAttribute"/> and discoverable through
        /// <see cref="ModRegistry.GetContextRequirements"/>.
        /// </summary>
        public virtual void SetContext(ModExecutionContext context) => _executionContext = context;

        /// <summary>
        /// Retrieve a context value by type. Returns default when no context is set or the
        /// key is absent  use <see cref="ModRegistry.GetContextRequirements"/> to validate
        /// that all required context is provided before invoking.
        /// </summary>
        protected T GetContext<T>() => _executionContext != null ? _executionContext.GetOrDefault<T>() : default;

        /// <summary>Retrieve a context value by explicit key.</summary>
        protected T GetContext<T>(string key) => _executionContext != null ? _executionContext.GetOrDefault<T>(key) : default;

        /// <summary>Display name shown in the mod list.</summary>
        public abstract string Name        { get; }
        /// <summary>Tooltip / short description shown on hover.</summary>
        public abstract string Description { get; }
        /// <summary>Tab this mod should appear in.</summary>
        public abstract ModsWindow ModsWindow    { get; }

        private static readonly ConcurrentDictionary<Type, bool> _initializedTypes = new();

        [ThreadStatic] private static bool _creatingDetached;

        /// <summary>True for instances created via <see cref="ModRegistry.GetDetachedInstance"/> 
        /// they never register UI panels or registry entries and exist so external callers
        /// (macros, scripting) get their own state and context, separate from the UI instance.</summary>
        public bool IsDetached { get; private set; }

        /// <summary>Construct a detached instance: skips Plugin/window/registry registration
        /// but still runs one-time static init and <see cref="Awake"/>.</summary>
        internal static BaseMod CreateDetached(Type modType)
        {
            _creatingDetached = true;
            try { return (BaseMod)Activator.CreateInstance(modType); }
            finally { _creatingDetached = false; }
        }

        protected BaseMod()
        {
            try
            {
                if (_initializedTypes.TryAdd(GetType(), true))
                    OnStaticInit();

                if (_creatingDetached)
                {
                    IsDetached = true;
                }
                else
                {
                    Plugin.Mods.Add(this);
                    ModsWindow?.Mods.Add(this);
                    ModRegistry.Register(this);   // scan [ModSetting] / [ModAction] members
                }

                Awake();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[BaseMod] Failed to initialize '{GetType().FullName}': {ex}");
            }
        }

        /// <summary>
        /// Called once per concrete type on first instantiation. Use for Harmony patches
        /// and any other one-time static setup.
        /// </summary>
        protected virtual void OnStaticInit() { }

        /// <summary>
        /// Called after registration is complete. Override instead of defining a constructor
        /// to get proper error reporting on initialization failures.
        /// </summary>
        protected virtual void Awake() { }

        /// <summary>Called every frame. Override for mods that need per-frame logic.</summary>
        public virtual void Update() { }

        /// <summary>
        /// Called when the UI panel should refresh its displayed values.
        /// Override if you drive mod state outside the UI callbacks and need
        /// to push changes back into the panel elements.
        /// </summary>
        public virtual void RefreshUI()
            => AutoUIBuilder.Refresh(this);

        public void StartCoroutine(IEnumerator routine)
            => Plugin._StartCoroutine(routine);

        /// <summary>
        /// Override to react to any [ModSetting] value changing (via UI interaction or SetValue()).
        /// Called before externally-registered ModRegistry.OnSettingChanged callbacks.
        /// <code>
        /// protected override void OnSettingChanged(string name, object value)
        /// {
        ///     if (name == nameof(MoveSpeed)) ApplySpeed((float)value);
        /// }
        /// </code>
        /// </summary>
        protected virtual void OnSettingChanged(string memberName, object newValue) { }

        // ModRegistry calls this to trigger the protected virtual above
        internal void NotifySettingChanged(string memberName, object newValue)
            => OnSettingChanged(memberName, newValue);

        /// <summary>
        /// Unique id for this mod. Just shorthand for the full type name
        /// </summary>
        protected string Id => GetType().FullName;

        /// <summary>Load a previously saved value, or <c>default</c> if none exists.</summary>
        protected T LoadData<T>(string key) => DataStorage.Load<T>(Id, key);

        /// <summary>Snapshot and save <paramref name="value"/> asynchronously.</summary>
        protected void SaveData<T>(string key, T value) => DataStorage.Save(Id, key, value);

        /// <returns><c>true</c> if a saved file exists for <paramref name="key"/>.</returns>
        protected bool DataExists(string key) => DataStorage.Exists(Id, key);

        /// <summary>Delete the saved file for <paramref name="key"/> if it exists.</summary>
        protected void DeleteData(string key) => DataStorage.Delete(Id, key);

        /// <summary>
        /// Load the saved value from the shared <c>data.json</c> bag into <paramref name="field"/>
        /// (falling back to <paramref name="defaultValue"/> when the key is absent), then
        /// auto-save into the bag whenever <paramref name="field"/> changes.
        /// </summary>
        protected void BindData<T>(Ref<T> field, string key, T defaultValue = default)
        {
            field.Value = DataStorage.BagEntryExists(Id, key)
                ? DataStorage.LoadFromBag<T>(Id, key)
                : defaultValue;

            field.Changed += value => DataStorage.SaveToBag(Id, key, value);
        }

        /// <summary>
        /// Like <see cref="BindData{T}"/> but stores to a dedicated <c>{key}.json</c> file
        /// instead of the shared bag  use this when you want to save a larger object separately.
        /// </summary>
        protected void BindDataFile<T>(Ref<T> field, string key, T defaultValue = default)
        {
            field.Value = DataStorage.Exists(Id, key)
                ? DataStorage.Load<T>(Id, key)
                : defaultValue;

            field.Changed += value => DataStorage.Save(Id, key, value);
        }

        /// <summary>
        /// Build a UI panel for this mod. Called by the tab/window system whenever
        /// a panel is needed; can be called multiple times with different <paramref name="id"/>s
        /// to create independent panel instances.
        ///
        /// Default: auto-builds from [ModSetting] and [ModAction] attributes via AutoUIBuilder.
        /// Override to provide a fully custom layout.
        /// </summary>
        public virtual Container BuildPanel(string id)
            => AutoUIBuilder.Build(this, id);

        /// <summary>
        /// Wrap a custom widget in the right-click "Add to macro" / "Create hotkey" context menu
        /// for one of this mod's <c>[ModAction]</c> methods. Use in a custom <see cref="BuildPanel"/>
        /// for actions you render yourself (typically <c>ShowInUI=false</c>); the auto builder
        /// already wraps <c>ShowInUI=true</c> members. <paramref name="extraItems"/> are appended to
        /// the menu, so your own entries are never crowded out. Returns the trigger unwrapped when
        /// <paramref name="methodName"/> is not a registered action.
        /// </summary>
        protected BaseUIElement ActionMenu(BaseUIElement trigger, string methodName, params BaseUIElement[] extraItems)
        {
            var desc = ModRegistry.FindAction(this, methodName);
            return desc != null ? ModContextMenu.ForAction(trigger, desc, extraItems) : trigger;
        }

        /// <summary>Setting counterpart of <see cref="ActionMenu"/>: wrap a custom widget in the
        /// context menu for one of this mod's <c>[ModSetting]</c> members.</summary>
        protected BaseUIElement SettingMenu(BaseUIElement trigger, string memberName, params BaseUIElement[] extraItems)
        {
            var desc = ModRegistry.FindSetting(this, memberName);
            return desc != null ? ModContextMenu.ForSetting(trigger, desc, extraItems) : trigger;
        }
    }
}
