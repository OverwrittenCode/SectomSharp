using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Models;
using SectomSharp.Managers.Pagination.SelectMenu;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Displays a help menu")]
    public async Task Help()
    {
        var groupedItems = _commands.SlashCommands.GroupBy(cmd =>
            (CategoryAttribute)cmd.Module.Attributes.First(attr => attr is CategoryAttribute)
        );

        var categoryConfig = new CategoryConfig<CategoryAttribute>
        {
            GetName = c => c.Name,
            GetValue = c => c.Name.ToLowerInvariant(),
            CustomIdPrefix = nameof(HelpSelectMenu),
            GetCustomIdWildcards = c => [c.Name],
            GetDescription = c => $"The {c.Name} category",
            GetEmote = c => c.Emoji,
        };

        var pageConfig = new PageConfig<SlashCommandInfo>
        {
            GetLabel = cmd => cmd.Name,
            GetValue = cmd => cmd.Name,
            GetDescription = cmd => cmd.Description,
        };

        var menuConfig = new NestedMenuConfig
        {
            EmbedTitle = "Help Menu",
            EmbedColour = Constants.LightGold,
        };

        await new SelectMenuPaginationBuilder("Select a category")
            .WithTimeout(15)
            .WithEphemeral()
            .WithResponseType(SelectMenuResponse.Update)
            .AddNestedMenu(groupedItems, categoryConfig, pageConfig, menuConfig)
            .Build()
            .Init(Context);
    }

    [RegexComponentInteraction(nameof(HelpSelectMenu), "id", "category")]
    public async Task HelpSelectMenu(
        [SelectMenuPaginationInstanceId] string _,
        string category,
        string[] values
    )
    {
        SlashCommandInfo command = _commands.SlashCommands.First(command =>
            command.Name == values[0]
        );

        await RespondAsync(
            embeds:
            [
                new EmbedBuilder()
                {
                    Color = Constants.LightGold,
                    Description = $"""
                    {Format.Bold("Name")}: {command.Name}
                    {Format.Bold("Description")}: {command.Description}
                    {Format.Bold("Category")}: {category}
                    """,
                }.Build(),
            ],
            ephemeral: true
        );
    }
}
