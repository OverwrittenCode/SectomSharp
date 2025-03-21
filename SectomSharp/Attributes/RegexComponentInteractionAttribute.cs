using Discord.Interactions;
using JetBrains.Annotations;
using SectomSharp.Utils;

namespace SectomSharp.Attributes;

/// <inheritdoc path="/summary" />
[MeansImplicitUse]
internal sealed class RegexComponentInteractionAttribute : ComponentInteractionAttribute
{
    /// <inheritdoc cref="RegexComponentInteractionAttribute(RunMode, String, global::System.String[])" />
    public RegexComponentInteractionAttribute(string prefix = "", params string[] wildcardNames) : this(RunMode.Default, prefix, wildcardNames) { }

    /// <inheritdoc cref="ComponentInteractionAttribute(String, Boolean, RunMode)" />
    /// <inheritdoc cref="StringUtils.GenerateComponentIdRegex(String, global::System.String[])" path="/param" />
    public RegexComponentInteractionAttribute(RunMode runMode = RunMode.Default, string prefix = "", params string[] wildcardNames) : base(
        StringUtils.GenerateComponentIdRegex(prefix, wildcardNames),
        true,
        runMode
    ) { }
}

/// <inheritdoc cref="RegexComponentInteractionAttribute" />
[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
internal sealed class RegexComponentInteractionAttribute<T> : ComponentInteractionAttribute
{
    /// <inheritdoc cref="RegexComponentInteractionAttribute(RunMode, String, global::System.String[])" />
    private RegexComponentInteractionAttribute(RunMode runMode = RunMode.Default, params string[] wildcardNames) : base(
        StringUtils.GenerateComponentIdRegex<T>(wildcardNames),
        true,
        runMode
    ) { }

    /// <inheritdoc cref="RegexComponentInteractionAttribute{T}(RunMode, global::System.String[])" />
    public RegexComponentInteractionAttribute(params string[] wildcardNames) : this(RunMode.Default, wildcardNames) { }
}
