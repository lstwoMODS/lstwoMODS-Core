using System;
using System.Collections.Generic;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// A typed key-value bag passed to <see cref="BaseMod.SetContext"/> before invoking actions
    /// or reading settings externally. Keys default to <c>Type.FullName</c> to match
    /// <see cref="ModContextAttribute"/>'s default key.
    ///
    /// <code>
    /// var ctx = new ModExecutionContext()
    ///     .With(player)           // key = typeof(IPlayer).FullName
    ///     .With("extra", value);  // explicit key
    ///
    /// mod.SetContext(ctx);
    /// action.Invoke();
    /// </code>
    /// </summary>
    public sealed class ModExecutionContext
    {
        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        // ── Fluent builders ───────────────────────────────────────────────────

        /// <summary>Store <paramref name="value"/> using its runtime type name as the key.</summary>
        public ModExecutionContext With<T>(T value)
            => With(typeof(T).FullName, value);

        /// <summary>Store <paramref name="value"/> under an explicit <paramref name="key"/>.</summary>
        public ModExecutionContext With(string key, object value)
        {
            _values[key] = value;
            return this;
        }

        // ── Retrieval ─────────────────────────────────────────────────────────

        /// <summary>
        /// Get a value by type. Throws <see cref="KeyNotFoundException"/> if not present.
        /// </summary>
        public T Get<T>() => Get<T>(typeof(T).FullName);

        /// <summary>
        /// Get a value by explicit key. Throws <see cref="KeyNotFoundException"/> if not present.
        /// </summary>
        public T Get<T>(string key)
        {
            if (!_values.TryGetValue(key, out var v))
                throw new KeyNotFoundException($"ModExecutionContext: no value for key '{key}' (expected {typeof(T).Name})");
            return (T)v;
        }

        /// <summary>Try to get a value by type. Returns false and sets <paramref name="value"/> to default when missing.</summary>
        public bool TryGet<T>(out T value) => TryGet(typeof(T).FullName, out value);

        /// <summary>Try to get a value by explicit key.</summary>
        public bool TryGet<T>(string key, out T value)
        {
            if (_values.TryGetValue(key, out var raw) && raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>Get a value by type, or <paramref name="fallback"/> when not present.</summary>
        public T GetOrDefault<T>(T fallback = default) => GetOrDefault(typeof(T).FullName, fallback);

        /// <summary>Get a value by explicit key, or <paramref name="fallback"/> when not present.</summary>
        public T GetOrDefault<T>(string key, T fallback = default)
            => TryGet<T>(key, out var v) ? v : fallback;

        /// <summary>Returns true if a value is stored under the given type's key.</summary>
        public bool Has<T>() => _values.ContainsKey(typeof(T).FullName);

        /// <summary>Returns true if a value is stored under the given explicit key.</summary>
        public bool Has(string key) => _values.ContainsKey(key);
    }
}
