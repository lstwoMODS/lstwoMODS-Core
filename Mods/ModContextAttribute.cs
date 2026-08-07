using System;

namespace lstwoMODS_Core.Hacks
{
    /// <summary>
    /// Declares that a mod class (or base class) requires a specific object to be injected
    /// into its <see cref="ModExecutionContext"/> before actions or settings are invoked.
    ///
    /// Apply this at the class level, usually on an abstract base in an extension:
    /// <code>
    /// [ModContext(typeof(IPlayer), Description = "The player this mod operates on")]
    /// public abstract class PlayerMod : BaseMod
    /// {
    ///     protected IPlayer Player => GetContext&lt;IPlayer&gt;();
    /// }
    /// </code>
    ///
    /// The registry collects all requirements via <see cref="ModRegistry.GetContextRequirements"/>
    /// so external systems (node graphs, scripting, etc.) know what to supply before calling
    /// <see cref="BaseMod.SetContext"/> and invoking an action.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public sealed class ModContextAttribute : Attribute
    {
        /// <summary>The type that must be present in the context.</summary>
        public Type   ContextType  { get; }

        /// <summary>
        /// Key used to store/retrieve this value in <see cref="ModExecutionContext"/>.
        /// Defaults to the full type name when not set.
        /// </summary>
        public string Key         { get; set; }

        /// <summary>Human-readable description for tooling / node UIs.</summary>
        public string Description { get; set; }

        public ModContextAttribute(Type contextType)
        {
            ContextType = contextType;
            Key         = contextType.FullName;
        }
    }
}
