using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Utils;
using StrongInteractions.Generated;

namespace SectomSharp.Managers.Pagination.SelectMenu;

/// <summary>
///     Manages select menu-based pagination for the help menu.
/// </summary>
internal sealed class HelpSelectMenuPaginationManager : InstanceManager<HelpSelectMenuPaginationManager>
{
    private static HelpCommandData _helpData;

    public static void Initialize(InteractionService interactionService)
    {
        IGrouping<CategoryAttribute, CommandInfo>[] groupedCommands = interactionService.SlashCommands.Select(cmd => new CommandInfo(
                                                                                                                  (CategoryAttribute)
                                                                                                                  cmd.Module.Attributes.First(attr => attr is CategoryAttribute),
                                                                                                                  Storage.CommandInfoFullNameMap[cmd],
                                                                                                                  cmd
                                                                                                              )
                                                                                         )
                                                                                        .GroupBy(info => info.Category)
                                                                                        .ToArray();

        Embed mainEmbed = new EmbedBuilder
        {
            Title = "Help Menu",
            Color = Storage.LightGold,
            Description = "This is the main menu. Select a command category below."
        }.Build();
        List<SelectMenuOptionBuilder> categoryOptions = groupedCommands
                                                       .Select(group => new SelectMenuOptionBuilder(
                                                                   group.Key.Name,
                                                                   group.Key.Name,
                                                                   $"The {group.Key.Name} category",
                                                                   group.Key.Emoji
                                                               )
                                                        )
                                                       .ToList();
        Dictionary<string, CategoryData> categoryMappings = groupedCommands.ToDictionary(
            group => group.Key.Name,
            group =>
            {
                Embed categoryEmbed = new EmbedBuilder
                {
                    Title = $"Help Menu | {group.Key.Name} Commands",
                    Color = Storage.LightGold,
                    Description = "Select a command below"
                }.Build();

                List<SelectMenuOptionBuilder> commandOptions = group.Select(info => new SelectMenuOptionBuilder(info.Name, info.Name, info.SlashCommand.Description)).ToList();

                Dictionary<string, Embed[]> commandEmbeds = group.ToDictionary(
                    info => info.Name,
                    info => new[]
                    {
                        new EmbedBuilder
                        {
                            Title = "Help Menu | Command Info",
                            Color = Storage.LightGold,
                            Description = $"""
                                           **Name:** {info.Name}
                                           **Description**: {info.SlashCommand.Description}
                                           """
                        }.Build()
                    }
                );

                return new CategoryData([categoryEmbed], commandOptions, commandEmbeds);
            }
        );

        _helpData = new HelpCommandData([mainEmbed], categoryOptions, categoryMappings);
    }

    public static async Task OnHit(SocketMessageComponent context, ulong id, HelpSelectMenuType type, string[] values)
    {
        if (await TryAcquireSessionAndDeferAsync(context, id) is not { } instance)
        {
            return;
        }

        try
        {
            string value = values[0];
            CategoryData categoryData;
            switch (type)
            {
                case HelpSelectMenuType.Category:

                    instance._currentCategory = value;
                    categoryData = _helpData.CategoryMappings[value];

                    if (!instance.TryExtend())
                    {
                        return;
                    }

                    await instance.ModifyMessageAsync(properties =>
                        {
                            properties.Embeds = categoryData.Embeds;
                            properties.Components = instance.GenerateMessageComponent(categoryData.CommandOptions);
                        }
                    );

                    instance.TryReleaseSession();
                    break;
                case HelpSelectMenuType.Command:
                    categoryData = _helpData.CategoryMappings[instance._currentCategory];

                    if (!instance.TryExtend())
                    {
                        return;
                    }

                    await instance.ModifyMessageAsync(properties =>
                        {
                            properties.Embeds = categoryData.CommandEmbeds[value];
                            properties.Components = instance.GenerateMessageComponent(categoryData.CommandOptions);
                        }
                    );

                    instance.TryReleaseSession();
                    break;
            }
        }
        catch (Exception ex)
        {
            await instance.TryCompleteAndThrowAsync(ex);
        }
    }

    private readonly string _commandId;

    private readonly ActionRowBuilder _categorySelectMenu;
    private string _currentCategory;

    /// <inheritdoc />
    public HelpSelectMenuPaginationManager(ILoggerFactory loggerFactory, SocketInteractionContext context) : base(loggerFactory, context)
    {
        string categoryId = StrongInteractionIds.HelpSelectMenu(InteractionId, HelpSelectMenuType.Category);
        _commandId = StrongInteractionIds.HelpSelectMenu(InteractionId, HelpSelectMenuType.Command);
        _currentCategory = "";
        _categorySelectMenu = new ActionRowBuilder().AddComponent(new SelectMenuBuilder(categoryId, _helpData.CategoryOptions, "Choose a category").Build());
    }

    private MessageComponent GenerateMessageComponent(List<SelectMenuOptionBuilder> commandOptions)
        => new ComponentBuilder
        {
            ActionRows =
            [
                _categorySelectMenu,
                new ActionRowBuilder { Components = [new SelectMenuBuilder(_commandId, commandOptions, "Choose a command").Build()] }
            ]
        }.Build();

    /// <inheritdoc />
    protected override Task<RestFollowupMessage> FollowupWithInitialResponseAsync(SocketInteractionContext context)
        => context.Interaction.FollowupAsync(embeds: _helpData.MainEmbeds, components: new ComponentBuilder { ActionRows = [_categorySelectMenu] }.Build(), ephemeral: IsEphemeral);

    private readonly record struct CommandInfo(CategoryAttribute Category, string Name, SlashCommandInfo SlashCommand);
    private readonly record struct HelpCommandData(Embed[] MainEmbeds, List<SelectMenuOptionBuilder> CategoryOptions, Dictionary<string, CategoryData> CategoryMappings);
    private readonly record struct CategoryData(Embed[] Embeds, List<SelectMenuOptionBuilder> CommandOptions, Dictionary<string, Embed[]> CommandEmbeds);
}
