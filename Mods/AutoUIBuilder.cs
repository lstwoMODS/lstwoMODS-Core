using System;
using System.Collections.Generic;
using System.Linq;
using lstwoMODS.ImGui.Shared;
using lstwoMODS.ImGui.Shared.UI;
using lstwoMODS_Core.UI;
using lstwoMODS_Core.UI.Elements;
using UnityEngine;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// Converts [ModSetting] and [ModAction] descriptors into UI elements.
    /// Called by BaseMod.BuildPanel() when no override is provided.
    /// </summary>
    public static class AutoUIBuilder
    {
        /// <summary>
        /// Push the current value of every setting back to its UI element.
        /// Call this after an external source changes a setting's backing value
        /// (e.g. Physics.gravity) so the overlay reflects the new state.
        /// Has no effect on Ref&lt;T&gt;-bound elements; they update automatically.
        /// </summary>
        public static void Refresh(BaseMod mod)
        {
            foreach (var s in ModRegistry.GetSettings(mod))
                s.PushToUI();
        }

        public static void RefreshFrom(BaseMod mod, object source)
        {
            foreach (var s in ModRegistry.GetSettings(mod).Where(s => s.ValueTarget == source))
                s.PushToUI();
        }

        /// <summary>
        /// Build a Container from all ShowInUI=true settings and actions for <paramref name="mod"/>.
        /// </summary>
        public static Container Build(BaseMod mod, string id)
            => BuildFrom(mod, mod, id);

        /// <summary>
        /// Build a Container from only the ShowInUI=true settings and actions registered via
        /// <see cref="ModRegistry.RegisterFrom"/> for <paramref name="source"/>.
        /// </summary>
        public static Container BuildFrom(BaseMod mod, object source, string id)
        {
            var elements = new List<BaseUIElement>();

            foreach (var s in ModRegistry.GetSettings(mod).Where(s => s.ValueTarget == source && s.ShowInUI).OrderBy(s => s.Order))
            {
                if (s.SeparatorText != null)
                    elements.Add(new SeparatorText($"{id}-septext-{s.MemberName}", s.SeparatorText));
                else if (s.Separator)
                    AddSpacedSeparator(elements, $"{id}-sep-{s.MemberName}");
                var settingEl = s.BuildElement(id);
                elements.Add(s.Macroable ? ModContextMenu.ForSetting(settingEl, s) : settingEl);
            }

            foreach (var a in ModRegistry.GetActions(mod).Where(a => a.InvokeTarget == source && a.ShowInUI).OrderBy(a => a.Order))
            {
                if (a.SeparatorText != null)
                    elements.Add(new SeparatorText($"{id}-septext-{a.MethodName}", a.SeparatorText));
                else if (a.Separator)
                    AddSpacedSeparator(elements, $"{id}-sep-{a.MethodName}");
                var actionEl = a.BuildElement(id);
                elements.Add(a.Macroable ? ModContextMenu.ForAction(actionEl, a) : actionEl);
            }

            return new Container(id, elements.ToArray());
        }

        /// <summary>
        /// Adds a Separator with Spacing above and below so its visual margin matches SeparatorText.
        /// </summary>
        private static void AddSpacedSeparator(List<BaseUIElement> elements, string name)
        {
            elements.Add(new Spacing($"{name}-pre"));
            elements.Add(new Separator(name));
            elements.Add(new Spacing($"{name}-post"));
        }

        // ── Integer type handling ─────────────────────────────────────────────
        // The ImGui Input/Drag/Slider-Int widgets are 32-bit signed. We reuse them
        // for every integer width (uint, long, ulong, short, ushort, byte, sbyte)
        // by clamping the widget's int into the target type's range on commit and
        // clamping the target value back into int range for display.

        private static bool IsIntegerType(Type t) =>
            t == typeof(int)   || t == typeof(uint)   ||
            t == typeof(long)  || t == typeof(ulong)  ||
            t == typeof(short) || t == typeof(ushort) ||
            t == typeof(byte)  || t == typeof(sbyte);

        /// <summary>
        /// The [lo, hi] int bounds to hand a Drag/Slider widget for an integer type so ImGui
        /// clamps during the interaction itself (a bare min==max==0 is treated as unbounded,
        /// which let unsigned values dip negative for a frame). Widths wider than int saturate
        /// at the int range.
        /// </summary>
        private static void IntRange(Type t, out int lo, out int hi)
        {
            if      (t == typeof(byte))   { lo = byte.MinValue;   hi = byte.MaxValue; }
            else if (t == typeof(sbyte))  { lo = sbyte.MinValue;  hi = sbyte.MaxValue; }
            else if (t == typeof(ushort)) { lo = ushort.MinValue; hi = ushort.MaxValue; }
            else if (t == typeof(short))  { lo = short.MinValue;  hi = short.MaxValue; }
            else if (t == typeof(uint) || t == typeof(ulong)) { lo = 0; hi = int.MaxValue; }
            else                          { lo = int.MinValue;   hi = int.MaxValue; } // int, long
        }

        /// <summary>Boxed integer of any width → int clamped to the int range the 32-bit widget can show.</summary>
        private static int ToWidgetInt(object value)
        {
            if (value == null) return 0;
            try
            {
                var l = Convert.ToInt64(value);
                if (l > int.MaxValue) return int.MaxValue;
                if (l < int.MinValue) return int.MinValue;
                return (int)l;
            }
            catch (OverflowException) { return int.MaxValue; } // ulong > long.MaxValue
        }

        /// <summary>Widget int → boxed value of <paramref name="targetType"/>, clamped to that type's range.</summary>
        private static object FromWidgetInt(int v, Type targetType)
        {
            if (targetType == typeof(int))    return v;
            if (targetType == typeof(uint))   return (uint)Math.Max(0, v);
            if (targetType == typeof(long))   return (long)v;
            if (targetType == typeof(ulong))  return (ulong)Math.Max(0, v);
            if (targetType == typeof(short))  return (short)Math.Max(short.MinValue, Math.Min((int)short.MaxValue, v));
            if (targetType == typeof(ushort)) return (ushort)Math.Max(0, Math.Min((int)ushort.MaxValue, v));
            if (targetType == typeof(byte))   return (byte)Math.Max(0, Math.Min((int)byte.MaxValue, v));
            if (targetType == typeof(sbyte))  return (sbyte)Math.Max(sbyte.MinValue, Math.Min((int)sbyte.MaxValue, v));
            return v;
        }

        // ── Vector and color boxing ──────────────────────────────────────────
        //
        // Settings and action parameters are read and written as boxed objects through
        // reflection, which does not apply the Vec2/Col implicit conversions. So unbox
        // tolerantly (the member may be declared in either flavour) and re-box as the
        // flavour the member actually declares, or reflection rejects the write.

        private static Vec2 ToVec2(object v) => v switch { Vec2 a => a, Vector2 b => b, _ => default };
        private static Vec3 ToVec3(object v) => v switch { Vec3 a => a, Vector3 b => b, _ => default };
        private static Vec4 ToVec4(object v) => v switch { Vec4 a => a, Vector4 b => b, _ => default };
        private static Col  ToCol (object v) => v switch { Col  a => a, Color   b => b, _ => default };

        private static object BoxVec2(Vec2 v, Type declared) => declared == typeof(Vec2) ? v : (object)(Vector2)v;
        private static object BoxVec3(Vec3 v, Type declared) => declared == typeof(Vec3) ? v : (object)(Vector3)v;
        private static object BoxVec4(Vec4 v, Type declared) => declared == typeof(Vec4) ? v : (object)(Vector4)v;
        private static object BoxCol (Col  v, Type declared) => declared == typeof(Col)  ? v : (object)(Color)v;


        public static BaseUIElement BuildSettingElement(ModSettingDescriptor settingDescriptor, string idPrefix)
        {
            var id        = $"{idPrefix}-{settingDescriptor.MemberName}";
            var rawLabel  = settingDescriptor.Label;
            var hasApply  = settingDescriptor.ApplyButton;
            // When ApplyButton is set, hide the displayed label but keep a unique ImGui ID.
            var label     = hasApply ? $"##{settingDescriptor.MemberName}" : rawLabel;
            var widget    = settingDescriptor.Widget;
            var hasMin    = !float.IsNaN(settingDescriptor.Min);
            var hasMax    = !float.IsNaN(settingDescriptor.Max);
            var hasRange  = hasMin && hasMax;
            var format    = settingDescriptor.Format;
            var dragSpeed = settingDescriptor.Speed > 0f ? settingDescriptor.Speed : 1f;

            // In apply-button mode the widget writes to this pending slot instead of
            // committing through SetValue; the button does the commit on click.
            // Ref<T> bindings are bypassed in this mode so the Ref isn't updated per-keystroke.
            object pending = settingDescriptor.GetValue();
            Action<object> commit = hasApply
                ? (Action<object>)(v => pending = v)
                : (v => settingDescriptor.SetValue(v));

            BaseUIElement element;

            if (settingDescriptor.ValueType == typeof(bool))
            {
                if (!hasApply && settingDescriptor.RefObject is Ref<bool> rb)
                    element = new Checkbox(label, (bool)settingDescriptor.GetValue(), v => commit(v)).WithValue(rb);
                else
                {
                    var w = new Checkbox(label, (bool)settingDescriptor.GetValue(), v => commit(v));
                    settingDescriptor.AddUIPush(v => { w.Value = (bool)v; pending = v; });
                    element = w;
                }
            }
            else if (settingDescriptor.ValueType == typeof(float))
            {
                if (widget == WidgetType.Input)
                {
                    var w = new InputFloat(label, (float)settingDescriptor.GetValue(), format: format ?? "%.3f", onValueChanged: v => commit(v));
                    settingDescriptor.AddUIPush(v => { w.Value = (float)v; pending = v; });
                    element = w;
                }
                else if (widget == WidgetType.Slider && hasRange || widget == WidgetType.Default && hasRange)
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<float> rf)
                        element = new SliderFloat(label, (float)settingDescriptor.GetValue(), settingDescriptor.Min, settingDescriptor.Max, format ?? "%.3f", v => commit(v)).WithValue(rf);
                    else
                    {
                        var w = new SliderFloat(label, (float)settingDescriptor.GetValue(), settingDescriptor.Min, settingDescriptor.Max, format ?? "%.3f", v => commit(v));
                        settingDescriptor.AddUIPush(v => { w.Value = (float)v; pending = v; });
                        element = w;
                    }
                }
                else
                {
                    var dragMin = hasMin ? settingDescriptor.Min : (hasMax ? -float.MaxValue : 0f);
                    var dragMax = hasMax ? settingDescriptor.Max : (hasMin ? float.MaxValue : 0f);
                    if (!hasApply && settingDescriptor.RefObject is Ref<float> rf)
                        element = new DragFloat(label, speed: dragSpeed, min: dragMin, max: dragMax, format: format ?? "%.3f", onValueChanged: v => commit(v)).WithValue(rf);
                    else
                    {
                        var w = new DragFloat(label, (float)settingDescriptor.GetValue(), dragSpeed, min: dragMin, max: dragMax, format: format ?? "%.3f", onValueChanged: v => commit(v));
                        settingDescriptor.AddUIPush(v => { w.Value = (float)v; pending = v; });
                        element = w;
                    }
                }
            }
            else if (IsIntegerType(settingDescriptor.ValueType))
            {
                var vt    = settingDescriptor.ValueType;
                var isInt = vt == typeof(int);
                if (widget == WidgetType.Input)
                {
                    var w = new InputInt(label, ToWidgetInt(settingDescriptor.GetValue()), onValueChanged: v => commit(FromWidgetInt(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToWidgetInt(v); pending = v; });
                    element = w;
                }
                else if (widget == WidgetType.Slider && hasRange || widget == WidgetType.Default && hasRange)
                {
                    if (isInt && !hasApply && settingDescriptor.RefObject is Ref<int> ri)
                        element = new SliderInt(label, 0, (int)settingDescriptor.Min, (int)settingDescriptor.Max, format ?? "%d", v => commit(v)).WithValue(ri);
                    else
                    {
                        var w = new SliderInt(label, ToWidgetInt(settingDescriptor.GetValue()), (int)settingDescriptor.Min, (int)settingDescriptor.Max, format ?? "%d", v => commit(FromWidgetInt(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToWidgetInt(v); pending = v; });
                        element = w;
                    }
                }
                else
                {
                    int dragIntMin, dragIntMax;
                    var dragFlags = ImGuiSliderFlags.None;
                    if (isInt)
                    {
                        dragIntMin = hasMin ? (int)settingDescriptor.Min : (hasMax ? int.MinValue : 0);
                        dragIntMax = hasMax ? (int)settingDescriptor.Max : (hasMin ? int.MaxValue : 0);
                    }
                    else
                    {
                        // Hand ImGui the type's range and AlwaysClamp so the value never dips
                        // out of range (even for a frame) during drag or CTRL+click entry.
                        IntRange(vt, out var typeLo, out var typeHi);
                        dragIntMin = hasMin ? (int)settingDescriptor.Min : typeLo;
                        dragIntMax = hasMax ? (int)settingDescriptor.Max : typeHi;
                        dragFlags  = ImGuiSliderFlags.AlwaysClamp;
                    }
                    if (isInt && !hasApply && settingDescriptor.RefObject is Ref<int> ri)
                        element = new DragInt(label, speed: dragSpeed, min: dragIntMin, max: dragIntMax, format: format ?? "%d", onValueChanged: v => commit(v)).WithValue(ri);
                    else
                    {
                        var w = new DragInt(label, ToWidgetInt(settingDescriptor.GetValue()), dragSpeed, min: dragIntMin, max: dragIntMax, format: format ?? "%d", onValueChanged: v => commit(FromWidgetInt(v, vt)), flags: dragFlags);
                        settingDescriptor.AddUIPush(v => { w.Value = ToWidgetInt(v); pending = v; });
                        element = w;
                    }
                }
            }
            else if (settingDescriptor.ValueType == typeof(string))
            {
                var w = new InputText(label, (string)settingDescriptor.GetValue() ?? "", onChanged: v => commit(v));
                settingDescriptor.AddUIPush(v => { w.Value = (string)v; pending = v; });
                element = w;
            }
            // Each vector/color branch below serves both the Unity type and its serializable
            // counterpart; `vt` is the one this setting actually declares, and the value is
            // boxed back as that on every commit.
            else if (settingDescriptor.ValueType == typeof(Vector2) || settingDescriptor.ValueType == typeof(Vec2))
            {
                var vt = settingDescriptor.ValueType;
                if (widget == WidgetType.Input)
                {
                    var w = new InputFloat2(label, ToVec2(settingDescriptor.GetValue()), format: format ?? "%.3f", onValueChanged: v => commit(BoxVec2(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToVec2(v); pending = v; });
                    element = w;
                }
                else
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<Vec2> rvec)
                        element = new DragFloat2(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec2(v, vt))).WithValue(rvec);
                    else if (!hasApply && settingDescriptor.RefObject is Ref<Vector2> rv)
                        element = new DragFloat2(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec2(v, vt))).WithValue(rv);
                    else
                    {
                        var w = new DragFloat2(label, ToVec2(settingDescriptor.GetValue()), dragSpeed, onValueChanged: v => commit(BoxVec2(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToVec2(v); pending = v; });
                        element = w;
                    }
                }
            }
            else if (settingDescriptor.ValueType == typeof(Vector3) || settingDescriptor.ValueType == typeof(Vec3))
            {
                var vt = settingDescriptor.ValueType;
                if (widget == WidgetType.Color3)
                {
                    var w = new ColorEdit3(label, ToVec3(settingDescriptor.GetValue()), onChanged: v => commit(BoxVec3(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToVec3(v); pending = v; });
                    element = w;
                }
                else if (widget == WidgetType.Input)
                {
                    var w = new InputFloat3(label, ToVec3(settingDescriptor.GetValue()), format: format ?? "%.3f", onValueChanged: v => commit(BoxVec3(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToVec3(v); pending = v; });
                    element = w;
                }
                else
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<Vec3> rvec)
                        element = new DragFloat3(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec3(v, vt))).WithValue(rvec);
                    else if (!hasApply && settingDescriptor.RefObject is Ref<Vector3> rv)
                        element = new DragFloat3(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec3(v, vt))).WithValue(rv);
                    else
                    {
                        var w = new DragFloat3(label, ToVec3(settingDescriptor.GetValue()), dragSpeed, onValueChanged: v => commit(BoxVec3(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToVec3(v); pending = v; });
                        element = w;
                    }
                }
            }
            else if (settingDescriptor.ValueType == typeof(Vector4) || settingDescriptor.ValueType == typeof(Vec4))
            {
                var vt = settingDescriptor.ValueType;
                if (widget == WidgetType.Color4)
                {
                    var w = new ColorEdit4(label, ToVec4(settingDescriptor.GetValue()), onChanged: v => commit(BoxVec4(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToVec4(v); pending = v; });
                    element = w;
                }
                else if (widget == WidgetType.Input)
                {
                    var w = new InputFloat4(label, ToVec4(settingDescriptor.GetValue()), format: format ?? "%.3f", onValueChanged: v => commit(BoxVec4(v, vt)));
                    settingDescriptor.AddUIPush(v => { w.Value = ToVec4(v); pending = v; });
                    element = w;
                }
                else
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<Vec4> rvec)
                        element = new DragFloat4(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec4(v, vt))).WithValue(rvec);
                    else if (!hasApply && settingDescriptor.RefObject is Ref<Vector4> rv)
                        element = new DragFloat4(label, speed: dragSpeed, onValueChanged: v => commit(BoxVec4(v, vt))).WithValue(rv);
                    else
                    {
                        var w = new DragFloat4(label, ToVec4(settingDescriptor.GetValue()), dragSpeed, onValueChanged: v => commit(BoxVec4(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToVec4(v); pending = v; });
                        element = w;
                    }
                }
            }
            else if (settingDescriptor.ValueType == typeof(Color) || settingDescriptor.ValueType == typeof(Col))
            {
                var vt = settingDescriptor.ValueType;
                if (widget == WidgetType.Color3)
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<Col> rcol)
                        element = new ColorEdit3(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt))).WithValue(rcol);
                    else if (!hasApply && settingDescriptor.RefObject is Ref<Color> rc)
                        element = new ColorEdit3(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt))).WithValue(rc);
                    else
                    {
                        var w = new ColorEdit3(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToCol(v); pending = v; });
                        element = w;
                    }
                }
                else
                {
                    if (!hasApply && settingDescriptor.RefObject is Ref<Col> rcol)
                        element = new ColorEdit4(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt))).WithValue(rcol);
                    else if (!hasApply && settingDescriptor.RefObject is Ref<Color> rc)
                        element = new ColorEdit4(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt))).WithValue(rc);
                    else
                    {
                        var w = new ColorEdit4(label, ToCol(settingDescriptor.GetValue()), onChanged: v => commit(BoxCol(v, vt)));
                        settingDescriptor.AddUIPush(v => { w.Value = ToCol(v); pending = v; });
                        element = w;
                    }
                }
            }
            else if (settingDescriptor.ValueType.IsEnum)
            {
                // Enum → Combo maps values to indices; no Ref<T> constructor variant for this mapping.
                var values  = Enum.GetValues(settingDescriptor.ValueType);
                var names   = Enum.GetNames(settingDescriptor.ValueType);
                var current = Array.IndexOf(values, settingDescriptor.GetValue());
                var w = new Combo(label, names, current, onChanged: i => commit(values.GetValue(i)));
                settingDescriptor.AddUIPush(v => { w.SelectedIndex = Array.IndexOf(values, v); pending = v; });
                element = w;
            }
            else
            {
                // Fallback: read-only label showing current ToString()
                element = new LabelText($"{id}-lbl", label, settingDescriptor.GetValue()?.ToString() ?? "");
            }

            if (!string.IsNullOrEmpty(settingDescriptor.Description))
                element.Data.Tooltip = settingDescriptor.Description;

            if (hasApply)
            {
                var buttonLabel = settingDescriptor.ApplyButtonLabel ?? $"Set {rawLabel}";
                var btn = new Button(buttonLabel, () => settingDescriptor.SetValue(pending));
                element = new HStack($"{id}-applyrow", element, btn).WithProportions(3f, 1f).WithContentWidth();
            }

            // Scope the whole subtree by a stable, unique id. ImGui keys widgets by their
            // label string and nothing pushes a per-element id automatically, so two members
            // sharing a display label (e.g. two "Enabled"/"Reset", or an apply-row button
            // labelled "Set X") would otherwise collide. Applied to the outer element so the
            // apply button is covered too; an explicit [ModSetting(Id=...)] wins when set.
            element.Data.PushCommands.Insert(0,
                new PushIdCommand { Id = !string.IsNullOrEmpty(settingDescriptor.Id) ? settingDescriptor.Id : id });

            return element;
        }

        // ── Action element builder ────────────────────────────────────────────

        public static BaseUIElement BuildActionElement(ModActionDescriptor a, string idPrefix)
        {
            var id = $"{idPrefix}-{a.MethodName}";

            var button = new Button(a.Label, () => a.Invoke()).WithContentWidth(a.ContentWidth);

            if (!string.IsNullOrEmpty(a.Description))
                button.Data.Tooltip = a.Description;

            button.Data.PushCommands.Insert(0,
                new PushIdCommand { Id = !string.IsNullOrEmpty(a.Id) ? a.Id : id });

            var uiParams = a.Parameters.Where(p => !p.IsExcluded).ToArray();
            if (uiParams.Length == 0)
                return button;

            var children = new BaseUIElement[uiParams.Length + 1];
            for (int i = 0; i < uiParams.Length; i++)
                children[i] = uiParams[i].BuildElement(id);
            children[uiParams.Length] = button;

            return new Container(id, children);
        }

        public static BaseUIElement BuildParameterElement(ModActionParameterDescriptor p, string idPrefix)
        {
            var id = $"{idPrefix}-param-{p.Name}";
            var label = p.Label;
            var widget = p.ParamAttribute?.Widget ?? WidgetType.Default;
            var speed = p.ParamAttribute?.Speed  ?? 0f;
            var min = p.ParamAttribute?.Min    ?? float.NaN;
            var max = p.ParamAttribute?.Max    ?? float.NaN;
            var fmt = p.ParamAttribute?.Format;
            var hasRange = !float.IsNaN(min) && !float.IsNaN(max);
            var dragSpeed = speed > 0f ? speed : 1f;

            BaseUIElement el;

            if (p.ParameterType == typeof(bool))
                el = new Checkbox(label, (bool)p.CurrentValue, v => p.CurrentValue = v);
            else if (p.ParameterType == typeof(float))
            {
                if (widget == WidgetType.Input)
                    el = new InputFloat(label, (float)p.CurrentValue, format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = v);
                else if (widget == WidgetType.Slider && hasRange)
                    el = new SliderFloat(label, (float)p.CurrentValue, min, max, fmt ?? "%.3f", v => p.CurrentValue = v);
                else
                    el = new DragFloat(label, (float)p.CurrentValue, dragSpeed, format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = v);
            }
            else if (IsIntegerType(p.ParameterType))
            {
                var pt = p.ParameterType;
                if (widget == WidgetType.Input)
                    el = new InputInt(label, ToWidgetInt(p.CurrentValue), onValueChanged: v => p.CurrentValue = FromWidgetInt(v, pt));
                else if (widget == WidgetType.Slider && hasRange)
                    el = new SliderInt(label, ToWidgetInt(p.CurrentValue), (int)min, (int)max, fmt ?? "%d", v => p.CurrentValue = FromWidgetInt(v, pt));
                else
                {
                    int dmin = 0, dmax = 0;
                    var dflags = ImGuiSliderFlags.None;
                    if (pt != typeof(int)) { IntRange(pt, out dmin, out dmax); dflags = ImGuiSliderFlags.AlwaysClamp; }
                    el = new DragInt(label, ToWidgetInt(p.CurrentValue), dragSpeed, min: dmin, max: dmax, format: fmt ?? "%d", onValueChanged: v => p.CurrentValue = FromWidgetInt(v, pt), flags: dflags);
                }
            }
            else if (p.ParameterType == typeof(string))
                el = new InputText(label, (string)(p.CurrentValue ?? ""), onChanged: v => p.CurrentValue = v);
            // As with settings: the widget speaks Vec2/Col, but CurrentValue is invoked
            // through reflection and so has to be boxed as the declared parameter type.
            else if (p.ParameterType == typeof(Vector2) || p.ParameterType == typeof(Vec2))
            {
                var pt = p.ParameterType;
                if (widget == WidgetType.Input)
                    el = new InputFloat2(label, ToVec2(p.CurrentValue), format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec2(v, pt));
                else
                    el = new DragFloat2(label, ToVec2(p.CurrentValue), dragSpeed, format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec2(v, pt));
            }
            else if (p.ParameterType == typeof(Vector3) || p.ParameterType == typeof(Vec3))
            {
                var pt = p.ParameterType;
                if (widget == WidgetType.Color3)
                    el = new ColorEdit3(label, ToVec3(p.CurrentValue), onChanged: v => p.CurrentValue = BoxVec3(v, pt));
                else if (widget == WidgetType.Input)
                    el = new InputFloat3(label, ToVec3(p.CurrentValue), format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec3(v, pt));
                else
                    el = new DragFloat3(label, ToVec3(p.CurrentValue), dragSpeed, format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec3(v, pt));
            }
            else if (p.ParameterType == typeof(Vector4) || p.ParameterType == typeof(Vec4))
            {
                var pt = p.ParameterType;
                if (widget == WidgetType.Color4)
                    el = new ColorEdit4(label, ToVec4(p.CurrentValue), onChanged: v => p.CurrentValue = BoxVec4(v, pt));
                else if (widget == WidgetType.Input)
                    el = new InputFloat4(label, ToVec4(p.CurrentValue), format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec4(v, pt));
                else
                    el = new DragFloat4(label, ToVec4(p.CurrentValue), dragSpeed, format: fmt ?? "%.3f", onValueChanged: v => p.CurrentValue = BoxVec4(v, pt));
            }
            else if (p.ParameterType == typeof(Color) || p.ParameterType == typeof(Col))
            {
                var pt = p.ParameterType;
                if (widget == WidgetType.Color3)
                    el = new ColorEdit3(label, ToCol(p.CurrentValue), onChanged: v => p.CurrentValue = BoxCol(v, pt));
                else
                    el = new ColorEdit4(label, ToCol(p.CurrentValue), onChanged: v => p.CurrentValue = BoxCol(v, pt));
            }
            else if (p.ParameterType.IsEnum)
            {
                var values  = Enum.GetValues(p.ParameterType);
                var names   = Enum.GetNames(p.ParameterType);
                var current = Array.IndexOf(values, p.CurrentValue);
                el = new Combo(label, names, current, onChanged: i => p.CurrentValue = values.GetValue(i));
            }
            else
            {
                // Unsupported type  read-only label showing current value
                el = new LabelText(id, label, p.CurrentValue?.ToString() ?? "");
            }

            el.Data.PushCommands.Insert(0,
                new PushIdCommand { Id = !string.IsNullOrEmpty(p.Id) ? p.Id : id });

            return el;
        }
    }
}
