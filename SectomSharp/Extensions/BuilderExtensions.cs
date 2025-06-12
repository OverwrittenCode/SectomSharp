using Discord;
using JetBrains.Annotations;
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
    [PublicAPI]
    public static SelectMenuBuilder WithComponentId(this SelectMenuBuilder builder, string prefix, params object[] values)
        => builder.WithCustomId(StringUtils.GenerateComponentId(prefix, values));

    /// <inheritdoc cref="StringUtils.GenerateComponentIdRegex{T}(global::System.String[])" path="/typeparam" />
    /// <inheritdoc cref="WithComponentId(SelectMenuBuilder, String, global::System.Object[])" />
    public static SelectMenuBuilder WithComponentId<T>(this SelectMenuBuilder builder, params object[] values) => builder.WithCustomId(StringUtils.GenerateComponentId<T>(values));

    /// <inheritdoc cref="WithComponentId(SelectMenuBuilder, String, global::System.Object[])" />
    [PublicAPI]
    public static ButtonBuilder WithComponentId(this ButtonBuilder builder, string prefix, params object[] values)
        => builder.WithCustomId(StringUtils.GenerateComponentId(prefix, values));

    /// <inheritdoc cref="WithComponentId{T}(SelectMenuBuilder, global::System.Object[])" />
    public static ButtonBuilder WithComponentId<T>(this ButtonBuilder builder, params object[] values) => builder.WithCustomId(StringUtils.GenerateComponentId<T>(values));

    public static ComponentBuilder FromComponentsWithAllDisabled(this IReadOnlyCollection<IMessageComponent> components)
    {
        var builder = new ComponentBuilder();
        for (int i = 0; i != components.Count; i++)
        {
            IMessageComponent component = components.ElementAt(i);
            AddComponent(component, i);
        }

        return builder;

        void AddComponent(IMessageComponent component, int row)
        {
            switch (component)
            {
                case ButtonComponent button:
                    builder.WithButton(button.Label, button.CustomId, button.Style, button.Emote, button.Url, true, row);
                    break;

                case ActionRowComponent actionRow:
                    foreach (IMessageComponent messageComponent in actionRow.Components)
                    {
                        AddComponent(messageComponent, row);
                    }

                    break;
                case SelectMenuComponent menu:
                    builder.WithSelectMenu(
                        menu.CustomId,
                        menu.Options?.Select(x => new SelectMenuOptionBuilder(x.Label, x.Value, x.Description, x.Emote, x.IsDefault)).ToList(),
                        menu.Placeholder,
                        menu.MinValues,
                        menu.MaxValues,
                        true,
                        row
                    );
                    break;
            }
        }
    }
}
