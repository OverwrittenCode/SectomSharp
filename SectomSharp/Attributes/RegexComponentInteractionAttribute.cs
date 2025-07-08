using Discord.Interactions;
using SectomSharp.Utils;

namespace SectomSharp.Attributes;

/// <inheritdoc />
[AttributeUsage(AttributeTargets.Method)]
internal sealed class RegexComponentInteractionAttribute<T> : ComponentInteractionAttribute
{
    /// <summary>
    ///     Creates a command for component interaction handling.
    /// </summary>
    public RegexComponentInteractionAttribute() : base($"{typeof(T).Name}{Storage.ComponentWildcardSeparator}*{Storage.ComponentWildcardSeparator}*", true) { }
}
