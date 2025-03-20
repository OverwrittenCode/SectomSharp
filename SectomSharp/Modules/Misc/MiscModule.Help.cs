using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Models;
using SectomSharp.Managers.Pagination.SelectMenu;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Misc;

[SuppressMessage("ReSharper", "EntityNameCapturedOnly.Global")]
public partial class MiscModule
{
    [SlashCmd("Displays a help menu")]
    public async Task Help()
    {
        IEnumerable<IGrouping<CategoryAttribute, SlashCommandInfo>> groupedItems =
            _commands.SlashCommands.GroupBy(cmd => (CategoryAttribute)cmd.Module.Attributes.First(attr => attr is CategoryAttribute));

        var categoryConfig = new CategoryConfig<CategoryAttribute>
        {
            CustomIdPrefix = nameof(HelpSelectMenu),
            GetName = c => c.Name,
            GetValue = c => c.Name.ToLowerInvariant(),
            GetCustomIdWildcards = c => [c.Name],
            GetDescription = c => $"The {c.Name} category",
            GetEmote = c => c.Emoji
        };

        var pageConfig = new PageConfig<SlashCommandInfo>
        {
            GetLabel = cmd => cmd.Name,
            GetValue = cmd => cmd.Name,
            GetDescription = cmd => cmd.Description
        };

        var menuConfig = new NestedMenuConfig
        {
            EmbedTitle = "Help Menu",
            EmbedColour = Constants.LightGold
        };

        await new SelectMenuPaginationBuilder("Select a category").WithEphemeral()
                                                                  .WithResponseType(SelectMenuResponse.Update)
                                                                  .AddNestedMenu(groupedItems, categoryConfig, pageConfig, menuConfig)
                                                                  .Build()
                                                                  .Init(Context);
    }

    [RegexComponentInteraction(nameof(HelpSelectMenu), nameof(id), nameof(category))]
    public async Task HelpSelectMenu([SelectMenuPaginationInstanceId] string id, string category, string[] values)
    {
        SlashCommandInfo command = _commands.SlashCommands.First(command => command.Name == values[0]);

        await RespondOrFollowUpAsync(
            embeds:
            [
                new EmbedBuilder
                {
                    Color = Constants.LightGold,
                    Description = $"""
                                   {Format.Bold("Name")}: {command.Name}
                                   {Format.Bold("Description")}: {command.Description}
                                   {Format.Bold("Category")}: {category}
                                   """
                }.Build()
            ],
            ephemeral: true
        );
    }
}
