using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.Elements;
using UnityEngine;

namespace lstwoMODS_Core.Hacks
{
    // =========================================================================
    // Descriptors
    // =========================================================================

    /// <summary>Represents one [ModSetting]-annotated field or property on a mod.</summary>
    public class ModSettingDescriptor
    {
        public BaseMod  Mod         { get; }
        public string   ModName     { get; }
        public string   MemberName  { get; }  // field/property name
        /// <summary>The object whose fields/properties back this setting's value. Equals <see cref="Mod"/> unless registered via <see cref="ModRegistry.RegisterFrom"/>.</summary>
        public object   ValueTarget { get; }
        public string   Label       { get; }
        public string   Description { get; }
        public bool     ShowInUI    { get; }
        public int      Order       { get; }
        public Type     ValueType   { get; }
        /// <summary>float.NaN when not set.</summary>
        public float    Min         { get; }
        public float    Max         { get; }
        public string   Format      { get; }
        /// <summary>ImGui ID pushed before this widget, or null.</summary>
        public string   Id          { get; }
        /// <summary>When true, a Separator is inserted above this item in the auto-built panel.</summary>
        public bool     Separator      { get; }
        /// <summary>When non-null, a SeparatorText with this label is inserted above this item in the auto-built panel.</summary>
        public string   SeparatorText  { get; }
        /// <summary>Which widget the auto-builder should use. <see cref="WidgetType.Default"/> = auto-select by type.</summary>
        public WidgetType Widget       { get; }
        /// <summary>Drag speed for drag widgets. 0 = default (1.0).</summary>
        public float      Speed        { get; }
        /// <summary>When true, the auto-builder wraps the widget in an HStack with a button that commits the pending value via <see cref="SetValue"/>.</summary>
        public bool       ApplyButton      { get; }
        /// <summary>Custom button label when <see cref="ApplyButton"/> is true. Null = "Set {Label}".</summary>
        public string     ApplyButtonLabel { get; }
        /// <summary>When true (default), the auto-built UI wraps this setting in an "Add to macro" / "Create hotkey" context menu.</summary>
        public bool       Macroable        { get; }

        private readonly FieldInfo    _field;
        private readonly PropertyInfo _prop;

        // When the field/property type is Ref<T>, these hold the Ref instance and its Value property.
        // ValueType is set to T (the inner type), not Ref<T>.
        private readonly object       _refObject;
        private readonly PropertyInfo _refValueProp;
        /// <summary>The underlying Ref&lt;T&gt; instance, or null when the member is a plain value.</summary>
        internal object RefObject => _refObject;

        // Push actions registered by AutoUIBuilder so SetValue can sync non-Ref overlay elements.
        private readonly List<Action<object>> _uiPushActions = new();
        internal void AddUIPush(Action<object> push) => _uiPushActions.Add(push);

        /// <summary>
        /// Push the current value to all registered UI elements without triggering
        /// setting-changed callbacks or writing back to the field/property.
        /// Call this to sync the overlay after an external change (e.g. Physics.gravity changed).
        /// </summary>
        internal void PushToUI()
        {
            if (_uiPushActions.Count == 0) return;
            var v = GetValue();
            foreach (var push in _uiPushActions) push(v);
        }

        internal ModSettingDescriptor(BaseMod mod, FieldInfo fi, ModSettingAttribute attr)
            : this(mod, mod, fi, attr) { }

        internal ModSettingDescriptor(BaseMod mod, PropertyInfo pi, ModSettingAttribute attr)
            : this(mod, mod, pi, attr) { }

        internal ModSettingDescriptor(BaseMod mod, object valueTarget, FieldInfo fi, ModSettingAttribute attr)
        {
            Mod = mod; ModName = mod.Name; ValueTarget = valueTarget; MemberName = fi.Name;
            Label = attr.Label ?? ModRegistry.NicifyName(fi.Name); Description = attr.Description;
            ShowInUI = attr.ShowInUI; Order = attr.Order;
            Min = attr.Min; Max = attr.Max; Format = attr.Format; Id = attr.Id; Separator = attr.Separator; SeparatorText = attr.SeparatorText; Widget = attr.Widget; Speed = attr.Speed;
            ApplyButton = attr.ApplyButton; ApplyButtonLabel = attr.ApplyButtonLabel; Macroable = attr.Macroable;
            _field = fi;

            var ft    = fi.FieldType;
            var isRef = ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(Ref<>);
            ValueType = isRef ? ft.GetGenericArguments()[0] : ft;
            if (isRef) { _refObject = fi.GetValue(valueTarget); _refValueProp = ft.GetProperty("Value"); }

            var ctx = fi.GetCustomAttribute<ModContextAttribute>();
            if (ctx != null)
                ContextRequirement = new ContextRequirement(ctx);
        }

        internal ModSettingDescriptor(BaseMod mod, object valueTarget, PropertyInfo pi, ModSettingAttribute attr)
        {
            Mod = mod; ModName = mod.Name; ValueTarget = valueTarget; MemberName = pi.Name;
            Label = attr.Label ?? ModRegistry.NicifyName(pi.Name); Description = attr.Description;
            ShowInUI = attr.ShowInUI; Order = attr.Order;
            Min = attr.Min; Max = attr.Max; Format = attr.Format; Id = attr.Id; Separator = attr.Separator; SeparatorText = attr.SeparatorText; Widget = attr.Widget; Speed = attr.Speed;
            ApplyButton = attr.ApplyButton; ApplyButtonLabel = attr.ApplyButtonLabel; Macroable = attr.Macroable;
            _prop = pi;

            var pt    = pi.PropertyType;
            var isRef = pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Ref<>);
            ValueType = isRef ? pt.GetGenericArguments()[0] : pt;
            if (isRef) { _refObject = pi.GetValue(valueTarget); _refValueProp = pt.GetProperty("Value"); }

            var ctx = pi.GetCustomAttribute<ModContextAttribute>();
            if (ctx != null)
                ContextRequirement = new ContextRequirement(ctx);
        }

        /// <summary>Context requirement declared via <see cref="ModContextAttribute"/> on this member, or null.</summary>
        public ContextRequirement ContextRequirement { get; }

        public object GetValue()
        {
            if (_refObject != null) return _refValueProp.GetValue(_refObject);
            return _field != null ? _field.GetValue(ValueTarget) : _prop.GetValue(ValueTarget);
        }

        private bool CanRebindTo(object target)
        {
            if (target == null || ReferenceEquals(target, ValueTarget)) return false;
            var declaring = _field?.DeclaringType ?? _prop.DeclaringType;
            return declaring != null && declaring.IsInstanceOfType(target);
        }

        /// <summary>Read this setting's value from a different instance of the same class 
        /// used for detached (per-context) mod instances. Falls back to the registered
        /// instance when <paramref name="target"/> is null or incompatible.</summary>
        public object GetValue(object target)
        {
            if (!CanRebindTo(target)) return GetValue();
            if (_refObject != null)
            {
                var refObj = _field != null ? _field.GetValue(target) : _prop.GetValue(target);
                return _refValueProp.GetValue(refObj);
            }
            return _field != null ? _field.GetValue(target) : _prop.GetValue(target);
        }

        /// <summary>Write this setting's value on a different instance of the same class.
        /// Fires only the target mod's own <see cref="BaseMod.OnSettingChanged"/>  external
        /// callbacks and UI pushes belong to the registered UI instance and are skipped.</summary>
        public void SetValue(object target, object v)
        {
            if (!CanRebindTo(target)) { SetValue(v); return; }
            if (_refObject != null)
            {
                var refObj = _field != null ? _field.GetValue(target) : _prop.GetValue(target);
                _refValueProp.SetValue(refObj, v);
            }
            else
            {
                _field?.SetValue(target, v);
                _prop?.SetValue(target, v);
            }
            (target as BaseMod)?.NotifySettingChanged(MemberName, v);
        }

        /// <summary>
        /// Set the value of this setting, then fire all registered OnSettingChanged callbacks.
        /// Use this instead of direct field assignment when you need callbacks to fire from code.
        /// </summary>
        public void SetValue(object v)
        {
            if (_refObject != null)
                _refValueProp.SetValue(_refObject, v); // fires Ref<T>.Changed → all bound elements update
            else
            {
                _field?.SetValue(ValueTarget, v);
                _prop?.SetValue(ValueTarget, v);
            }
            ModRegistry.FireSettingChanged(this, v);
            foreach (var push in _uiPushActions) push(v);
        }

        /// <summary>Create an appropriate UI element for this setting.</summary>
        public BaseUIElement BuildElement(string idPrefix)
            => AutoUIBuilder.BuildSettingElement(this, idPrefix);
    }

    /// <summary>Describes one parameter of a [ModAction] method, holding the current UI-supplied value.</summary>
    public class ModActionParameterDescriptor
    {
        /// <summary>Parameter name as declared in source.</summary>
        public string Name          { get; }
        /// <summary>Either <see cref="Name"/> cleaned up, or the specified <see cref="ModActionParamAttribute.Label"/>.</summary>
        public string Label => ParamAttribute?.Label ?? ModRegistry.NicifyName(Name);
        /// <summary>Parameter type.</summary>
        public Type   ParameterType { get; }
        /// <summary>True when listed in <see cref="ModActionAttribute.ExcludeParameters"/>. No widget is built; default is used.</summary>
        public bool   IsExcluded    { get; }

        /// <summary>Current value to pass when the action is invoked. Initialized to the parameter's default.</summary>
        public object CurrentValue  { get; set; }

        /// <summary>Widget override declared via <see cref="ModActionParamAttribute"/>, or null (use default).</summary>
        public ModActionParamAttribute ParamAttribute { get; }
        /// <summary>ImGui ID pushed before this widget, or null.</summary>
        public string Id => ParamAttribute?.Id;

        internal ModActionParameterDescriptor(ParameterInfo pi, bool excluded)
        {
            Name          = pi.Name;
            ParameterType = pi.ParameterType;
            IsExcluded    = excluded;
            CurrentValue  = pi.HasDefaultValue ? pi.DefaultValue : GetTypeDefault(pi.ParameterType);
            ParamAttribute = pi.GetCustomAttribute<ModActionParamAttribute>();
        }

        private static object GetTypeDefault(Type t)
            => t.IsValueType ? Activator.CreateInstance(t) : null;

        public BaseUIElement BuildElement(string idPrefix)
            => AutoUIBuilder.BuildParameterElement(this, idPrefix);
    }

    /// <summary>Represents one [ModAction]-annotated method on a mod.</summary>
    public class ModActionDescriptor
    {
        public BaseMod  Mod         { get; }
        public string   ModName     { get; }
        public string   MethodName  { get; }
        public string   Label       { get; }
        public string   Description { get; }
        public bool     ShowInUI    { get; }
        public int      Order       { get; }
        /// <summary>ImGui ID pushed before this widget, or null.</summary>
        public string   Id            { get; }
        /// <summary>Whether the button width matches ImGui.CalcItemWidth().</summary>
        public bool     ContentWidth  { get; }
        /// <summary>When true, a Separator is inserted above this action in the auto-built panel.</summary>
        public bool     Separator     { get; }
        /// <summary>When non-null, a SeparatorText with this label is inserted above this action in the auto-built panel.</summary>
        public string   SeparatorText { get; }
        /// <summary>When true (default), the auto-built UI wraps this action in an "Add to macro" / "Create hotkey" context menu.</summary>
        public bool     Macroable     { get; }

        /// <summary>All parameters of this method, including excluded ones.</summary>
        public IReadOnlyList<ModActionParameterDescriptor> Parameters { get; }

        private readonly MethodInfo _method;

        /// <summary>Context requirement declared via <see cref="ModContextAttribute"/> on this member, or null.</summary>
        public ContextRequirement ContextRequirement { get; }

        /// <summary>The object whose methods back this action's invocation. Equals <see cref="Mod"/> unless registered via <see cref="ModRegistry.RegisterFrom"/>.</summary>
        public object   InvokeTarget { get; }

        internal ModActionDescriptor(BaseMod mod, MethodInfo mi, ModActionAttribute attr)
            : this(mod, mod, mi, attr) { }

        internal ModActionDescriptor(BaseMod mod, object invokeTarget, MethodInfo mi, ModActionAttribute attr)
        {
            Mod = mod; ModName = mod.Name; InvokeTarget = invokeTarget; MethodName = mi.Name;
            Label = attr.Label ?? ModRegistry.NicifyName(mi.Name); Description = attr.Description;
            ShowInUI = attr.ShowInUI; Order = attr.Order; Id = attr.Id; ContentWidth = attr.ContentWidth; Separator = attr.Separator; SeparatorText = attr.SeparatorText; Macroable = attr.Macroable;
            _method = mi;

            var excluded = new HashSet<string>(attr.ExcludeParameters ?? Array.Empty<string>(), StringComparer.Ordinal);
            var ps = mi.GetParameters();
            var paramDescs = new ModActionParameterDescriptor[ps.Length];
            for (int i = 0; i < ps.Length; i++)
                paramDescs[i] = new ModActionParameterDescriptor(ps[i], excluded.Contains(ps[i].Name));
            Parameters = Array.AsReadOnly(paramDescs);

            var ctx = mi.GetCustomAttribute<ModContextAttribute>();
            if (ctx != null)
                ContextRequirement = new ContextRequirement(ctx);
        }

        /// <summary>Return type of the underlying method (typeof(void) for actions without a result).</summary>
        public Type ReturnType => _method.ReturnType;

        public void Invoke()
        {
            if (Parameters.Count == 0)
                _method.Invoke(InvokeTarget, null);
            else
                _method.Invoke(InvokeTarget, Parameters.Select(p => p.CurrentValue).ToArray());
        }

        /// <summary>
        /// Invoke with explicit per-call arguments: one per non-excluded parameter, in
        /// declaration order; excluded parameters receive their <see cref="ModActionParameterDescriptor.CurrentValue"/>.
        /// Unlike <see cref="Invoke()"/> this never reads or writes CurrentValue for the
        /// supplied parameters, so callers (e.g. macros) don't rewrite the values shown in
        /// the mod panel.
        /// </summary>
        public object Invoke(object[] args)
            => Invoke(null, args);

        /// <summary>Like <see cref="Invoke(object[])"/> but invoked on a different instance of
        /// the same class, used for detached (per-context) mod instances. Falls back to the
        /// registered instance when <paramref name="target"/> is null or incompatible.</summary>
        public object Invoke(object target, object[] args)
        {
            if (target == null || ReferenceEquals(target, InvokeTarget)
                || _method.DeclaringType == null || !_method.DeclaringType.IsInstanceOfType(target))
                target = InvokeTarget;
            if (_method.IsStatic) target = null;

            if (Parameters.Count == 0)
                return _method.Invoke(target, null);

            var full = new object[Parameters.Count];
            var next = 0;
            for (int i = 0; i < Parameters.Count; i++)
                full[i] = Parameters[i].IsExcluded ? Parameters[i].CurrentValue : args[next++];
            return _method.Invoke(target, full);
        }

        public BaseUIElement BuildElement(string idPrefix) => AutoUIBuilder.BuildActionElement(this, idPrefix);
    }

    /// <summary>
    /// Describes a single context value that a mod type requires before its actions or
    /// settings can be invoked externally. Collected from <see cref="ModContextAttribute"/>
    /// by <see cref="ModRegistry.GetContextRequirements"/>.
    /// </summary>
    public class ContextRequirement
    {
        /// <summary>The type that must be present in the <see cref="ModExecutionContext"/>.</summary>
        public Type   ContextType  { get; }
        /// <summary>Key used to store/retrieve the value (matches <see cref="ModContextAttribute.Key"/>).</summary>
        public string Key         { get; }
        /// <summary>Human-readable description for tooling. May be null.</summary>
        public string Description { get; }

        internal ContextRequirement(ModContextAttribute attr)
        {
            ContextType  = attr.ContextType;
            Key         = attr.Key;
            Description = attr.Description;
        }

        internal ContextRequirement(Type contextType, string key)
        {
            ContextType = contextType;
            Key         = key ?? contextType.FullName;
        }
    }

    // =========================================================================
    // Registry
    // =========================================================================

    /// <summary>
    /// Static database of all [ModSetting] and [ModAction] members across every BaseMod instance.
    /// Use this to enumerate mod capabilities at runtime, e.g. to build node graphs,
    /// scripting systems, or alternative UIs without touching individual mod classes.
    /// </summary>
    public static class ModRegistry
    {
        private static readonly List<ModSettingDescriptor>                              _settings  = new List<ModSettingDescriptor>();
        private static readonly List<ModActionDescriptor>                               _actions   = new List<ModActionDescriptor>();
        private static readonly Dictionary<(BaseMod, string), List<Action<object>>>     _callbacks = new Dictionary<(BaseMod, string), List<Action<object>>>();

        public static IReadOnlyList<ModSettingDescriptor> AllSettings => _settings.AsReadOnly();
        public static IReadOnlyList<ModActionDescriptor>  AllActions  => _actions.AsReadOnly();

        /// <summary>Bumped whenever settings/actions are (un)registered, so consumers
        /// (e.g. MacroRegistry's projection cache) can detect changes cheaply.</summary>
        internal static int Version { get; private set; }

        /// <summary>All settings belonging to <paramref name="mod"/>, in display order.</summary>
        public static IEnumerable<ModSettingDescriptor> GetSettings(BaseMod mod)
            => _settings.Where(s => s.Mod == mod);

        /// <summary>All actions belonging to <paramref name="mod"/>, in display order.</summary>
        public static IEnumerable<ModActionDescriptor> GetActions(BaseMod mod)
            => _actions.Where(a => a.Mod == mod);

        /// <summary>All settings with a given value type across all mods.</summary>
        public static IEnumerable<ModSettingDescriptor> GetSettingsByType(Type t)
            => _settings.Where(s => s.ValueType == t);

        /// <summary>Find a single setting by mod and member name.</summary>
        public static ModSettingDescriptor FindSetting(BaseMod mod, string memberName)
            => _settings.FirstOrDefault(s => s.Mod == mod && s.MemberName == memberName);

        /// <summary>Find a single action by mod and method name. Handy for wrapping a custom
        /// widget in a context menu: <c>ModContextMenu.ForAction(button, ModRegistry.FindAction(this, nameof(Teleport)))</c>.</summary>
        public static ModActionDescriptor FindAction(BaseMod mod, string methodName)
            => _actions.FirstOrDefault(a => a.Mod == mod && a.MethodName == methodName);

        // ── Context requirements ──────────────────────────────────────────────

        /// <summary>
        /// Returns all context requirements declared on <paramref name="modType"/> and its base classes
        /// via <see cref="ModContextAttribute"/>. Use this before constructing a mod instance to know
        /// what values to collect and pass to <see cref="BaseMod.SetContext"/>.
        /// <code>
        /// var reqs = ModRegistry.GetContextRequirements(typeof(MyPlayerMod));
        /// // reqs: [{ ContextType: IPlayer, Key: "MyExt.IPlayer", Description: "..." }]
        ///
        /// var ctx = new ModExecutionContext().With(player);
        /// mod.SetContext(ctx);
        /// action.Invoke();
        /// </code>
        /// </summary>
        public static IReadOnlyList<ContextRequirement> GetContextRequirements(Type modType)
        {
            var attrs = modType.GetCustomAttributes(typeof(ModContextAttribute), inherit: true);
            var result = new List<ContextRequirement>(attrs.Length);
            foreach (ModContextAttribute attr in attrs)
                result.Add(new ContextRequirement(attr));
            return result.AsReadOnly();
        }

        /// <summary>
        /// Returns all context requirements declared directly on actions and settings for
        /// <paramref name="mod"/> via <see cref="ModActionAttribute.ContextType"/> /
        /// <see cref="ModSettingAttribute.ContextType"/>. Class-level requirements are returned
        /// by <see cref="GetContextRequirements(Type)"/>.
        /// </summary>
        public static IReadOnlyList<ContextRequirement> GetMemberContextRequirements(BaseMod mod)
        {
            var result = new List<ContextRequirement>();
            foreach (var a in GetActions(mod))
                if (a.ContextRequirement != null) result.Add(a.ContextRequirement);
            foreach (var s in GetSettings(mod))
                if (s.ContextRequirement != null) result.Add(s.ContextRequirement);
            return result.AsReadOnly();
        }

        // ── Detached (per-context) instances ─────────────────────────────────

        private static readonly Dictionary<(Type Type, object Key), BaseMod> _detached = new Dictionary<(Type, object), BaseMod>();
        private static BaseMod[] _detachedTick = Array.Empty<BaseMod>();

        /// <summary>Detached instances, for the plugin's Update loop. Snapshot array, safe
        /// to iterate while instances are created.</summary>
        public static IReadOnlyList<BaseMod> DetachedInstances => _detachedTick;

        /// <summary>
        /// Get (or create) the detached instance of <paramref name="modType"/> for a context
        /// key; external callers like macros run against these so the UI instance's state
        /// and context are never touched. One instance lives per (mod type, key); keys that
        /// are destroyed Unity objects are pruned automatically. Null key = the mod's
        /// context-free twin.
        /// </summary>
        public static BaseMod GetDetachedInstance(Type modType, object contextKey = null)
        {
            PruneDetached();

            var key = (modType, contextKey);
            if (_detached.TryGetValue(key, out var instance)) return instance;

            instance = BaseMod.CreateDetached(modType);
            _detached[key] = instance;
            _detachedTick = _detached.Values.ToArray();
            return instance;
        }

        private static void PruneDetached()
        {
            List<(Type, object)> dead = null;
            foreach (var kv in _detached)
                if (kv.Key.Key is UnityEngine.Object uo && !uo)
                    (dead ??= new List<(Type, object)>()).Add(kv.Key);
            if (dead == null) return;
            foreach (var k in dead) _detached.Remove(k);
            _detachedTick = _detached.Values.ToArray();
        }

        // ── Change callbacks ──────────────────────────────────────────────────

        /// <summary>
        /// Register a callback that fires whenever a specific setting changes (via UI or SetValue()).
        /// This is an alternative to using a property setter; both can be used together.
        /// <code>
        /// ModRegistry.OnSettingChanged(this, nameof(MoveSpeed), v => ApplySpeed((float)v));
        /// </code>
        /// </summary>
        public static void OnSettingChanged(BaseMod mod, string memberName, Action<object> callback)
        {
            var key = (mod, memberName);
            if (!_callbacks.ContainsKey(key)) _callbacks[key] = new List<Action<object>>();
            _callbacks[key].Add(callback);
        }

        /// <summary>
        /// Strongly-typed overload; the cast is handled automatically.
        /// <code>
        /// ModRegistry.OnSettingChanged&lt;float&gt;(this, nameof(MoveSpeed), v => ApplySpeed(v));
        /// </code>
        /// </summary>
        public static void OnSettingChanged<T>(BaseMod mod, string memberName, Action<T> callback)
            => OnSettingChanged(mod, memberName, v => callback((T)v));

        /// <summary>Remove all callbacks registered for a specific setting on a mod.</summary>
        public static void ClearCallbacks(BaseMod mod, string memberName)
            => _callbacks.Remove((mod, memberName));

        /// <summary>Called by ModSettingDescriptor.SetValue; fires all registered callbacks.</summary>
        internal static void FireSettingChanged(ModSettingDescriptor desc, object newValue)
        {
            // 1. Virtual hook on the mod itself (override OnSettingChanged in BaseMod subclass)
            desc.Mod.NotifySettingChanged(desc.MemberName, newValue);

            // 2. Externally registered callbacks
            if (_callbacks.TryGetValue((desc.Mod, desc.MemberName), out var list))
                foreach (var cb in list) cb(newValue);
        }

        /// <summary>Scan a mod instance and register all annotated members. Called by BaseMod constructor.</summary>
        internal static void Register(BaseMod mod)
            => RegisterFrom(mod, mod);

        /// <summary>
        /// Scan <paramref name="source"/> for [ModSetting] and [ModAction] members and register them
        /// under <paramref name="mod"/>. Values are read/written from <paramref name="source"/>;
        /// all callbacks and <see cref="BaseMod.OnSettingChanged"/> notifications still fire on <paramref name="mod"/>.
        /// Call this from <see cref="BaseMod.Awake"/> to include a settings sub-object or child class in the mod's auto-built panel.
        /// </summary>
        public static void RegisterFrom(BaseMod mod, object source)
        {
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type  = source.GetType();

            foreach (var fi in type.GetFields(flags))
            {
                var attr = fi.GetCustomAttribute<ModSettingAttribute>();
                if (attr != null) _settings.Add(new ModSettingDescriptor(mod, source, fi, attr));
            }
            foreach (var pi in type.GetProperties(flags))
            {
                if (!pi.CanRead) continue;
                var attr = pi.GetCustomAttribute<ModSettingAttribute>();
                if (attr == null) continue;
                var pt    = pi.PropertyType;
                var isRef = pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Ref<>);
                if (!isRef && !pi.CanWrite) continue;
                _settings.Add(new ModSettingDescriptor(mod, source, pi, attr));
            }
            foreach (var mi in type.GetMethods(flags))
            {
                var attr = mi.GetCustomAttribute<ModActionAttribute>();
                if (attr != null) _actions.Add(new ModActionDescriptor(mod, source, mi, attr));
            }

            _settings.Sort((a, b) => a.Mod == b.Mod ? a.Order.CompareTo(b.Order) : 0);
            _actions.Sort((a, b)  => a.Mod == b.Mod ? a.Order.CompareTo(b.Order) : 0);
            Version++;
        }

        /// <summary>
        /// Converts any common identifier style to a human-readable display name. Supports:
        ///   camelCase / PascalCase  : "moveSpeed"      → "Move Speed"
        ///   SCREAMING_SNAKE_CASE    : "ENUM_VALUE"     → "Enum Value"
        ///   mixed / acronyms        : "myHTTPRequest"  → "My HTTP Request"
        ///   leading underscores/m_  : "_speed", "m_speed" → "Speed"
        ///   plain snake_case        : "some_value"     → "Some Value"
        /// </summary>
        public static string NicifyName(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;

            // Strip leading underscores and optional m_ prefix
            int s = 0;
            while (s < name.Length && name[s] == '_') s++;
            name = s > 0 ? name.Substring(s) : name;
            if (name.StartsWith("m_")) name = name.Substring(2);
            if (string.IsNullOrEmpty(name)) return name;

            // SCREAMING_SNAKE_CASE: every letter is uppercase (digits allowed), separated by underscores
            bool isScreaming = true;
            foreach (char ch in name)
                if (char.IsLetter(ch) && char.IsLower(ch)) { isScreaming = false; break; }

            if (isScreaming)
            {
                var words = name.Split('_');
                var titled = new System.Text.StringBuilder();
                foreach (var word in words)
                {
                    if (word.Length == 0) continue;
                    if (titled.Length > 0) titled.Append(' ');
                    titled.Append(char.ToUpper(word[0]));
                    titled.Append(word.Substring(1).ToLower());
                }
                return titled.ToString();
            }

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];

                if (c == '_') { sb.Append(' '); continue; }

                if (i == 0) { sb.Append(char.ToUpper(c)); continue; }

                if (char.IsUpper(c))
                {
                    char prev     = name[i - 1];
                    bool prevLow  = char.IsLower(prev) || char.IsDigit(prev);
                    bool nextLow  = i + 1 < name.Length && (char.IsLower(name[i + 1]) || char.IsDigit(name[i + 1]));
                    if (prevLow || (char.IsUpper(prev) && nextLow))
                        sb.Append(' ');
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>Remove all entries and callbacks for a mod (e.g. when the mod is unloaded).</summary>
        internal static void Unregister(BaseMod mod)
        {
            _settings.RemoveAll(s => s.Mod == mod);
            _actions.RemoveAll(a => a.Mod == mod);
            foreach (var key in new List<(BaseMod, string)>(_callbacks.Keys))
                if (key.Item1 == mod) _callbacks.Remove(key);
            Version++;
        }
    }
}
