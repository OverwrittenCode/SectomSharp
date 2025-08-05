using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Extensions;
using SectomSharp.Utils;
using StrongInteractions.Generated;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        public sealed partial class SuggestionModule
        {
            [SlashCmd("Add a panel to group components into an embed")]
            public async Task AddPanel(
                [Summary(description: "The embed title")] [MaxLength(SuggestionPanelConfiguration.MaxNameLength)] string name,
                [Summary(description: "The embed description")] [MaxLength(SuggestionPanelConfiguration.MaxDescriptionLength)] string description,
                [Summary(description: "The embed color")] Color? color = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                color ??= Storage.LightGold;
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      WITH
                                          guild_upsert AS (
                                              INSERT INTO "Guilds" ("Id")
                                              VALUES (@guildId)
                                              ON CONFLICT ("Id") DO NOTHING
                                          ),
                                          inserted AS (
                                              INSERT INTO "SuggestionPanels" ("GuildId", "Name", "Description", "Color")
                                              VALUES (@guildId, @name, @description, @color)
                                              ON CONFLICT DO NOTHING
                                              RETURNING 1
                                          )
                                      SELECT 1 FROM inserted;
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("name", name));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("description", description));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromNonNegativeInt32("color", color.Value.RawValue));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogCreateAsync(db, Context, reason);
            }

            [SlashCmd("Remove a panel by name")]
            public async Task RemovePanel([MaxLength(SuggestionPanelConfiguration.MaxNameLength)] string name, [ReasonMaxLength] string? reason = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                int affectedRows = await db.SuggestionPanels.Where(p => p.GuildId == Context.Guild.Id && p.Name == name).ExecuteDeleteAsync();
                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogDeleteAsync(db, Context, reason);
            }

            [SlashCmd("Modify a panel")]
            public async Task ModifyPanel(
                [Summary(description: "The current embed title")] [MaxLength(SuggestionPanelConfiguration.MaxNameLength)] string name,
                [Summary(description: "The new embed title")] [MaxLength(SuggestionPanelConfiguration.MaxNameLength)] string? newName = null,
                [Summary(description: "The new embed description")] [MaxLength(SuggestionPanelConfiguration.MaxDescriptionLength)] string? newDescription = null,
                [Summary(description: "The new embed color")] Color? newColor = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                if (name == newName)
                {
                    await RespondAsync(AlreadyConfiguredMessage);
                    return;
                }

                bool isRenaming = newName is not null;
                if (!isRenaming && newDescription is null && !newColor.HasValue)
                {
                    await RespondAsync(AtLeastOneMessage);
                    return;
                }

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();

                ulong guildId = Context.Guild.Id;
                // ReSharper disable once AccessToDisposedClosure
                int affectedRows = await db.SuggestionPanels
                                           .Where(panel => panel.GuildId == guildId
                                                        && panel.Name == name
                                                        && (isRenaming
                                                               ? !db.SuggestionPanels.Any(other => other.GuildId == guildId && other.Name == newName)
                                                               : (newDescription != null && newDescription != panel.Description)
                                                              || (newColor.HasValue && newColor.Value != panel.Color))
                                            )
                                           .ExecuteUpdateAsync(builder =>
                                                {
                                                    if (newName is not null)
                                                    {
                                                        builder.SetProperty(panel => panel.Name, newName);
                                                    }

                                                    if (newDescription is not null)
                                                    {
                                                        builder.SetProperty(panel => panel.Description, newDescription);
                                                    }

                                                    if (newColor.HasValue)
                                                    {
                                                        builder.SetProperty(panel => panel.Color, newColor.Value);
                                                    }
                                                }
                                            );

                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogUpdateAsync(db, Context, reason);
            }

            [SlashCmd("Add a component to a given panel")]
            public async Task AddComponent(
                [Summary(description: "The panel name to add the component to")] [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string panelName,
                [Summary(description: "The select menu choice label")] [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string componentName,
                [Summary(description: "The select menu choice description")] [MaxLength(SuggestionComponentConfiguration.MaxDescriptionLength)] string description,
                [Summary(description: "The select menu choice emoji")] [MaxLength(SuggestionComponentConfiguration.MaxIEmoteLength)] IEmote? emote = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      INSERT INTO "SuggestionComponents" ("GuildId", "PanelId", "Name", "Description", "Emote")
                                      SELECT @guildId, p."Id", @componentName, @description, @emote
                                      FROM "SuggestionPanels" p
                                      WHERE
                                          p."GuildId" = @guildId
                                          AND p."Name" = @panelName
                                      ON CONFLICT ("GuildId", "PanelId", "Name") DO NOTHING
                                      RETURNING 1;
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("panelName", panelName));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("componentName", componentName));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("description", description));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("emote", emote?.ToString()));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync("Either this component already exists or the panel name has not been configured yet.");
                    return;
                }

                await LogCreateAsync(db, Context, reason);
            }

            [SlashCmd("Remove a component from a panel")]
            public async Task RemoveComponent(
                [MaxLength(SuggestionPanelConfiguration.MaxNameLength)] string panelName,
                [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string componentName,
                [ReasonMaxLength] string? reason = null
            )
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                object? scalarResult;
                Stopwatch stopwatch;
                await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = """
                                      DELETE
                                      FROM "SuggestionComponents"
                                      WHERE
                                          "GuildId" = @guildId
                                          AND "PanelId" = (
                                              SELECT "Id"
                                              FROM "SuggestionPanels"
                                              WHERE
                                                  "GuildId" = @guildId
                                                  AND "Name" = @panelName
                                          )
                                          AND "Name" = @componentName
                                      RETURNING 1;
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("panelName", panelName));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromVarchar("componentName", componentName));

                    stopwatch = Stopwatch.StartNew();
                    scalarResult = await cmd.ExecuteScalarAsync();
                    stopwatch.Stop();
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                if (scalarResult is null)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogDeleteAsync(db, Context, reason);
            }

            [SlashCmd("Modify a component")]
            public async Task ModifyComponent(
                [Summary(description: "The panel name the component belongs to")] [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string panelName,
                [Summary(description: "The current select menu choice label")] [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string componentName,
                [Summary(description: "The new select menu choice label")] [MaxLength(SuggestionComponentConfiguration.MaxNameLength)] string? newComponentName = null,
                [Summary(description: "The new select menu choice description")] [MaxLength(SuggestionComponentConfiguration.MaxDescriptionLength)] string? newDescription = null,
                [Summary(description: "The new select menu choice emoji")] [MaxLength(SuggestionComponentConfiguration.MaxIEmoteLength)] IEmote? newEmote = null,
                [ReasonMaxLength] string? reason = null
            )
            {
                if (componentName == newComponentName)
                {
                    await RespondAsync(AlreadyConfiguredMessage);
                    return;
                }

                bool isRenaming = newComponentName is not null;
                if (!isRenaming && newDescription is null && newEmote is null)
                {
                    await RespondAsync(AtLeastOneMessage);
                    return;
                }

                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                ulong guildId = Context.Guild.Id;
                // ReSharper disable once AccessToDisposedClosure
                int affectedRows = await db.SuggestionComponents.Where(component => component.Panel.GuildId == guildId
                                                                                 && component.Panel.Name == panelName
                                                                                 && (isRenaming
                                                                                        ? !db.SuggestionComponents.Any(other => other.GuildId == guildId
                                                                                                                        && other.Name == newComponentName
                                                                                        )
                                                                                        : (newDescription != null && newDescription != component.Description)
                                                                                       || (newEmote != null && newEmote != component.Emote))
                                            )
                                           .ExecuteUpdateAsync(builder =>
                                                {
                                                    if (newComponentName is not null)
                                                    {
                                                        builder.SetProperty(component => component.Name, newComponentName);
                                                    }

                                                    if (newDescription is not null)
                                                    {
                                                        builder.SetProperty(component => component.Description, newDescription);
                                                    }

                                                    if (newEmote is not null)
                                                    {
                                                        builder.SetProperty(component => component.Emote, newEmote);
                                                    }
                                                }
                                            );

                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogUpdateAsync(db, Context, reason);
            }

            [SlashCmd("Send a panel to the current or a specified text channel")]
            public async Task SendPanel(
                string name,
                [Summary(description: "Where suggestions should be posted to")] SocketTextChannel postChannel,
                [Summary(description: "Where to send this panel to")] SocketTextChannel? channel = null
            )
            {
                if (channel is null)
                {
                    if (Context.Channel is not SocketTextChannel textChannel)
                    {
                        await RespondAsync("This channel is not a text channel.");
                        return;
                    }

                    channel = textChannel;
                }

                if (!Context.Guild.CurrentUser.GetPermissions(channel).SendMessages)
                {
                    await FollowupAsync("I do not have the required permissions to send messages in this channel.");
                    return;
                }

                await DeferAsync();
                SendPanelResult? result;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    result = await db.SuggestionPanels.Where(panel => panel.GuildId == Context.Guild.Id && panel.Name == name)
                                     .Select(panel => new SendPanelResult(
                                                 panel.Description,
                                                 panel.Color,
                                                 panel.Components.Select(component => new SelectMenuOptionBuilder(
                                                                             component.Name,
                                                                             "_",
                                                                             component.Description,
                                                                             component.Emote,
                                                                             false
                                                                         )
                                                       )
                                                      .ToList()
                                             )
                                      )
                                     .FirstOrDefaultAsync();
                }

                if (result is null)
                {
                    await FollowupAsync(NotConfiguredMessage);
                    return;
                }

                HashSet<ulong> emoteIds = Context.Guild.Emotes.Select(e => e.Id).ToHashSet();
                foreach (SelectMenuOptionBuilder selectMenuOptionBuilder in result.Components)
                {
                    if (selectMenuOptionBuilder.Emote is not Emote { Id: var emoteId } || emoteIds.Contains(emoteId))
                    {
                        continue;
                    }

                    await FollowupAsync($"Invalid emoji found on {selectMenuOptionBuilder.Label}: custom emojis must belong to this guild");
                    return;
                }

                try
                {
                    string? url = (await channel.SendMessageAsync(
                        embeds:
                        [
                            new EmbedBuilder
                            {
                                Title = name,
                                Description = result.Description,
                                Color = result.Color
                            }.Build()
                        ],
                        components: new ComponentBuilder
                        {
                            ActionRows =
                            [
                                new ActionRowBuilder
                                {
                                    Components =
                                    [
                                        new SelectMenuBuilder(StrongInteractionIds.SuggestionPanelSelectMenu(postChannel.Id), result.Components)
                                    ]
                                }
                            ]
                        }.Build()
                    )).GetJumpUrl();

                    var button = ButtonBuilder.CreateLinkButton("View Panel Message", url);
                    await FollowupAsync("Panel successfully sent.", components: new ComponentBuilder { ActionRows = [new ActionRowBuilder { Components = [button] }] }.Build());
                }
                catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions)
                {
                    await FollowupAsync("I do not have the required permissions to send messages in this channel.");
                }
                catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.InvalidFormBody && ex.Message.Contains("INVALID_EMOJI"))
                {
                    await FollowupAsync("Invalid emoji: custom emojis must belong to this guild. Please try again to see which component contains this emoji.");
                }
            }

            [SlashCmd("View the configured panels")]
            public async Task ViewPanels()
            {
                await DeferAsync();
                ViewPanelsResult? result;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    const int truncationLength = 100;
#pragma warning disable CA1845
                    result = await db.SuggestionPanels.Where(panel => panel.GuildId == Context.Guild.Id)
                                     .GroupBy(panel => panel.Guild.Configuration.Suggestion.IsDisabled)
                                     .Select(grouping => (ViewPanelsResult?)new ViewPanelsResult(
                                                 grouping.Key,
                                                 String.Join(
                                                     "\n",
                                                     grouping.Select(panel => "**"
                                                                            + panel.Name
                                                                            + "**: "
                                                                            + (panel.Description.Length > truncationLength
                                                                                  ? panel.Description.Substring(0, truncationLength) + "..."
                                                                                  : panel.Description)
                                                     )
                                                 )
                                             )
                                      )
                                     .FirstOrDefaultAsync();
#pragma warning restore CA1845
                }

                if (result is null)
                {
                    await FollowupAsync(NothingToView, ephemeral: true);
                    return;
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"{Context.Guild.Name} Suggestion Panels",
                    Color = Storage.LightGold,
                    Description = result.PanelsText
                };

                if (result.IsDisabled)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder { Text = "Module is currently disabled" };
                }

                await FollowupAsync(embeds: [embedBuilder.Build()]);
            }

            [SlashCmd("View the components from a specified panel")]
            public async Task ViewComponents(string panelName)
            {
                await DeferAsync();
                ViewComponentsResult? result;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    result = await db.SuggestionComponents.Where(component => component.GuildId == Context.Guild.Id && component.Panel.Name == panelName)
                                     .GroupBy(component => component.Guild.Configuration.Suggestion.IsDisabled)
                                     .Select(grouping => new ViewComponentsResult(
                                                 grouping.Key,
                                                 String.Join("\n", grouping.Select(component => "**" + component.Name + "**: " + component.Description))
                                             )
                                      )
                                     .FirstOrDefaultAsync();
                }

                if (result is null)
                {
                    await FollowupAsync(NothingToView, ephemeral: true);
                    return;
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"{Context.Guild.Name} Suggestion Components",
                    Color = Storage.LightGold,
                    Description = result.ComponentsText
                };

                if (result.IsDisabled)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder { Text = "Module is currently disabled" };
                }

                await FollowupAsync(embeds: [embedBuilder.Build()]);
            }

            private sealed record SendPanelResult(string Description, Color Color, List<SelectMenuOptionBuilder> Components);
            private sealed record ViewPanelsResult(bool IsDisabled, string PanelsText);
            private sealed record ViewComponentsResult(bool IsDisabled, string ComponentsText);
        }
    }
}
