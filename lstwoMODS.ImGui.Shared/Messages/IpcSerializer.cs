using System;
using System.Collections.Concurrent;
using System.Reflection;
using lstwoMODS.ImGui.Shared.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace lstwoMODS.ImGui.Shared
{
    /// <summary>
    /// Central, security-hardened JSON configuration shared by every IPC message.
    ///
    /// The element tree is polymorphic (<see cref="BaseUIElementData"/> / <see cref="PushCommand"/>
    /// subtypes), so we still rely on Newtonsoft type-name handling to round-trip it. But the peer
    /// on the other end of the (loopback, previously unauthenticated) socket is not trusted:
    /// <c>TypeNameHandling</c> is a well-known .NET deserialization RCE sink. All type resolution is
    /// therefore locked behind <see cref="WhitelistSerializationBinder"/>, which only ever resolves
    /// the concrete UI element and push-command types declared in this assembly. Any other <c>$type</c>
    /// throws before an instance can be constructed.
    ///
    /// Keep the security-critical knobs (binder, <see cref="MaxDepth"/>, <see cref="TypeNameHandling"/>)
    /// in this one place, do not re-instantiate <see cref="JsonSerializerSettings"/> elsewhere.
    /// </summary>
    internal static class IpcSerializer
    {
        /// <summary>Hard cap on nested-object depth, to stop hostile deeply-nested payloads.</summary>
        internal const int MaxDepth = 64;

        /// <summary>Allow-list binder: the single gate that blocks the deserialization RCE sink.</summary>
        internal static readonly ISerializationBinder Binder = new WhitelistSerializationBinder();

        /// <summary>The canonical settings every message serializes/deserializes through.</summary>
        internal static readonly JsonSerializerSettings Settings = CreateSettings();

        /// <summary>
        /// Builds a settings instance that shares the security-critical configuration (allow-list
        /// binder, max depth, type-name handling). Pass a <paramref name="contractResolver"/> for
        /// callers that need custom member handling (e.g. FrameStateMessage's child-stripping
        /// resolver) without dropping the security settings.
        /// </summary>
        internal static JsonSerializerSettings CreateSettings(IContractResolver contractResolver = null)
        {
            var settings = new JsonSerializerSettings
            {
                // Auto (not All) keeps the whitelist minimal: only the polymorphic leaf types carry
                // $type, so containers/wrappers/config never hit the binder.
                TypeNameHandling    = TypeNameHandling.Auto,
                SerializationBinder = Binder,
                MaxDepth            = MaxDepth,
            };

            if (contractResolver != null)
                settings.ContractResolver = contractResolver;

            return settings;
        }

        /// <summary>Serializes <paramref name="message"/> into an <see cref="IpcMessage"/> envelope.</summary>
        internal static IpcMessage Wrap<T>(T message) => new IpcMessage
        {
            Type    = typeof(T).Name,
            Payload = JsonConvert.SerializeObject(message, Settings)
        };

        /// <summary>Deserializes the payload of <paramref name="message"/> back into a <typeparamref name="T"/>.</summary>
        internal static T Unwrap<T>(IpcMessage message) =>
            JsonConvert.DeserializeObject<T>(message.Payload, Settings);

        /// <summary>
        /// Resolves only concrete <see cref="BaseUIElementData"/> and <see cref="PushCommand"/>
        /// subtypes, including ones contributed by plugin/extension assemblies (e.g. the WobblyLife
        /// overlay extension's <c>PropSpawnerData</c>). The security boundary is assignability to
        /// those two base types: deserialization-gadget types (ObjectDataProvider, AssemblyInstaller,
        /// …) never satisfy it and are rejected before any instance is constructed.
        ///
        /// Resolution scans only ALREADY-LOADED assemblies and never calls <c>Assembly.Load</c> on the
        /// attacker-supplied assembly hint, so the <c>$type</c> assembly name cannot be used as a
        /// gadget-load vector.
        /// </summary>
        private sealed class WhitelistSerializationBinder : ISerializationBinder
        {
            // Positive cache: type names already resolved AND cleared the assignability gate.
            private readonly ConcurrentDictionary<string, Type> _allowed =
                new ConcurrentDictionary<string, Type>();

            // Used only for BindToName (serialization side, our own already-trusted types).
            private readonly DefaultSerializationBinder _default = new DefaultSerializationBinder();

            public Type BindToType(string assemblyName, string typeName)
            {
                if (typeName == null)
                    throw new JsonSerializationException(
                        "Refused to resolve a null type name from an IPC payload.");

                if (_allowed.TryGetValue(typeName, out var cached))
                    return cached;

                var type = ResolveFromLoadedAssemblies(assemblyName, typeName);

                // THE security gate. Runs before the type is returned (and before Newtonsoft can
                // construct an instance). Only UI element / push-command types pass; everything else,
                // including every known deserialization gadget, throws here.
                if (type == null ||
                    !(typeof(BaseUIElementData).IsAssignableFrom(type) ||
                      typeof(PushCommand).IsAssignableFrom(type)))
                {
                    throw new JsonSerializationException(
                        $"Refused to resolve disallowed type '{typeName}' from an IPC payload.");
                }

                _allowed[typeName] = type;
                return type;
            }

            /// <summary>
            /// Finds a type by full name among assemblies that are ALREADY loaded into the AppDomain.
            /// Handles extension assemblies loaded after this binder was constructed (plugins are
            /// loaded at runtime). Never triggers an assembly load.
            /// </summary>
            private static Type ResolveFromLoadedAssemblies(string assemblyName, string typeName)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                // Prefer the assembly the payload names, if (and only if) it is already loaded.
                string simpleName = null;
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    try { simpleName = new AssemblyName(assemblyName).Name; }
                    catch { simpleName = null; }
                }

                if (simpleName != null)
                {
                    foreach (var asm in assemblies)
                    {
                        if (asm.GetName().Name != simpleName)
                            continue;

                        var t = asm.GetType(typeName, throwOnError: false);
                        if (t != null)
                            return t;
                    }
                }

                // Fall back to any already-loaded assembly that declares the type.
                foreach (var asm in assemblies)
                {
                    var t = asm.GetType(typeName, throwOnError: false);
                    if (t != null)
                        return t;
                }

                return null;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
                => _default.BindToName(serializedType, out assemblyName, out typeName);
        }
    }
}
