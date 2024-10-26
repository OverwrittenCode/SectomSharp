using Discord;
using SectomSharp.Utils;

namespace SectomSharp.Extensions;

internal static class BuilderExtensions
{
    /// <summary>
    ///     Sets the field CustomId related to a component.
    /// </summary>
    /// <param name="prefix">The prefix of the component id.</param>
    /// <param name="values">The arguments of the wildcards.</param>
    /// <returns>The current builder.</returns>
    /// <remarks>
    ///     See <seealso cref="StringUtils.GenerateComponentIdRegex(String, global::System.String[])" />
    ///     for generating the corresponding regex.
    /// </remarks>
    public static SelectMenuBuilder WithComponentId(
        this SelectMenuBuilder builder,
        string prefix,
        params object[] values
    ) => builder.WithCustomId(StringUtils.GenerateComponentId(prefix, values));

    /// <inheritdoc cref="StringUtils.GenerateComponentIdRegex{T}(global::System.String[])" path="/typeparam" />
    /// <inheritdoc cref="WithComponentId(SelectMenuBuilder, String, global::System.Object[])" />
    public static SelectMenuBuilder WithComponentId<T>(
        this SelectMenuBuilder builder,
        params object[] values
    ) => builder.WithCustomId(StringUtils.GenerateComponentId<T>(values));

    /// <inheritdoc cref="WithComponentId(SelectMenuBuilder, String, global::System.Object[])" />
    public static ButtonBuilder WithComponentId(
        this ButtonBuilder builder,
        string prefix,
        params object[] values
    ) => builder.WithCustomId(StringUtils.GenerateComponentId(prefix, values));

    /// <inheritdoc cref="WithComponentId{T}(SelectMenuBuilder, global::System.Object[])" />
    public static ButtonBuilder WithComponentId<T>(
        this ButtonBuilder builder,
        params object[] values
    ) => builder.WithCustomId(StringUtils.GenerateComponentId<T>(values));
}
