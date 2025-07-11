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
        var actionRows = new List<ActionRowBuilder>(components.Count);
        foreach (IMessageComponent component in components)
        {
            switch (component)
            {
                case ButtonComponent button:
                    actionRows.Add(new ActionRowBuilder { Components = [DisableButton(button)] });
                    break;

                case SelectMenuComponent menu:
                    actionRows.Add(new ActionRowBuilder { Components = [DisableSelectMenu(menu)] });
                    break;

                case ActionRowComponent actionRow:
                    IReadOnlyCollection<IMessageComponent> actionRowComponents = actionRow.Components;
                    var list = new List<IMessageComponent>(actionRowComponents.Count);
                    foreach (IMessageComponent messageComponent in actionRowComponents)
                    {
                        switch (messageComponent)
                        {
                            case ButtonComponent buttonComponent:
                                list.Add(DisableButton(buttonComponent));
                                break;
                            case SelectMenuComponent selectMenuComponent:
                                list.Add(DisableSelectMenu(selectMenuComponent));
                                break;
                            default:
                                throw new NotSupportedException($"Components of type {component.Type} is not supported.");
                        }
                    }

                    actionRows.Add(new ActionRowBuilder { Components = list });
                    break;
                default:
                    throw new NotSupportedException($"Components of type {component.Type} is not supported.");
            }
        }

        return new ComponentBuilder { ActionRows = actionRows };

        static ButtonComponent DisableButton(ButtonComponent button) => new ButtonBuilder(button.Label, button.CustomId, button.Style, button.Url, button.Emote, true).Build();

        static SelectMenuComponent DisableSelectMenu(SelectMenuComponent menu)
            => new SelectMenuBuilder(
                menu.CustomId,
                menu.Options.Select(o => new SelectMenuOptionBuilder(o.Label, o.Value, o.Description, o.Emote, o.IsDefault)).ToList(),
                menu.Placeholder,
                menu.MaxValues,
                menu.MinValues,
                true,
                menu.Type,
                menu.ChannelTypes.ToList(),
                menu.DefaultValues.ToList()
            ).Build();
    }
}
