using Discord;

namespace SectomSharp.Extensions;

internal static class BuilderExtensions
{
    /// <summary>
    ///     Clones a given collection of message components with all components disabled.
    /// </summary>
    /// <param name="components">The collection of message components.</param>
    /// <returns>A <see cref="ComponentBuilder" /> containing the cloned components with all components disabled.</returns>
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
