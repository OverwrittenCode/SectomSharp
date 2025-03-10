using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Services;

internal sealed class CaseService
{
    public const int IdLength = 6;
    public const int MaxReasonLength = 250;

    /// <summary>
    ///     Generates a <see cref="MessageComponent" />.
    /// </summary>
    /// <inheritdoc cref="GenerateLogEmbeds" path="/param" />
    /// <returns>A <see cref="MessageComponent" />.</returns>
    /// <value>
    ///     If <see cref="Case.LogMessageUrl" /> is <see langword="string" />;
    ///     a component with the log message button.<br />
    ///     If <see cref="Case.LogMessageUrl" /> is <see langword="null" />;
    ///     an empty component.
    /// </value>
    public static MessageComponent GenerateLogMessageButton(Case @case)
    {
        var component = new ComponentBuilder();

        if (@case.LogMessageUrl is { } url)
        {
            component.AddRow(
                new ActionRowBuilder().AddComponent(
                    new ButtonBuilder
                    {
                        Style = ButtonStyle.Link,
                        Label = "View Log Message",
                        Url = url
                    }.Build()
                )
            );
        }

        return component.Build();
    }

    /// <summary>
    ///     Creates a new case, logs it, and notifies relevant parties.
    /// </summary>
    /// <inheritdoc cref="GenerateLogEmbeds" path="/param" />
    /// <param name="context">The interaction context.</param>
    /// <param name="logType">The log type.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="perpetratorId">The id of the responsible user.</param>
    /// <param name="targetId">The id of the targeted user.</param>
    /// <param name="channelId">The targeted channel.</param>
    /// <param name="expiresAt">When the case has expired.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="includeGuildCases">If <see cref="Guild.Cases" /> should be included.</param>
    /// <returns>
    ///     The current <see cref="Guild" /> entity.
    /// </returns>
    /// <value>
    ///     Includes <see cref="Guild.BotLogChannels" />.<br />
    ///     If <paramref name="includeGuildCases" /> is <see langword="true" />;
    ///     <see cref="Guild.Cases" /> will be included.
    /// </value>
    public static async Task<Guild> LogAsync(
        SocketInteractionContext context,
        BotLogType logType,
        OperationType operationType,
        ulong? targetId = null,
        ulong? channelId = null,
        DateTime? expiresAt = null,
        string? reason = null,
        bool includeGuildCases = false
    )
    {
        if (!context.Interaction.HasResponded)
        {
            await context.Interaction.DeferAsync();
        }

        ulong? perpetratorId = context.User.Id == context.Client.CurrentUser.Id ? null : context.User.Id;

        var command = (SocketSlashCommand)context.Interaction;
        List<string> commandMentionArguments = [command.CommandName];
        List<EmbedFieldBuilder> commandFields = [];

        IReadOnlyCollection<SocketSlashCommandDataOption> options = command.Data.Options;

        while (options.Any(x => x.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup))
        {
            if (options.ElementAtOrDefault(0) is not { } option)
            {
                break;
            }

            commandMentionArguments.Add(option.Name);

            if (option.Options is not { } optionData)
            {
                break;
            }

            options = optionData;
        }

        foreach (SocketSlashCommandDataOption option in options)
        {
            var value = option.Value switch
            {
                IMentionable mentionable => $"{mentionable.Mention} ({mentionable})",
                SocketEntity<ulong> entity => $"{entity.Id} ({entity})",
                _ => option.Value.ToString() ?? "Unknown"
            };

            commandFields.Add(
                new EmbedFieldBuilder
                {
                    Name = option.Name,
                    Value = value
                }
            );
        }

        var caseId = StringUtils.GenerateUniqueId();

        EmbedBuilder commandInputEmbedBuilder = new EmbedBuilder().WithDescription($"</{String.Join(" ", commandMentionArguments)}:{command.CommandId}>")
                                                                  .WithAuthor($"{logType}{operationType} | {caseId}")
                                                                  .WithColor(
                                                                       perpetratorId is null
                                                                           ? Color.Purple
                                                                           : operationType == OperationType.Update
                                                                               ? Color.Orange
                                                                               : Color.Red
                                                                   )
                                                                  .WithFields(commandFields)
                                                                  .WithTimestamp(DateTime.UtcNow);

        if (perpetratorId.HasValue)
        {
            commandInputEmbedBuilder.WithColor(operationType == OperationType.Update ? Color.Orange : Color.Red)
                                    .WithThumbnailUrl(context.User.GetDisplayAvatarUrl())
                                    .WithFooter($"Perpetrator: {perpetratorId}");
        }
        else
        {
            commandInputEmbedBuilder.WithColor(Color.Purple);
        }

        var @case = new Case
        {
            Id = caseId,
            PerpetratorId = perpetratorId,
            TargetId = targetId,
            ChannelId = channelId,
            GuildId = context.Guild.Id,
            LogType = logType,
            OperationType = operationType,
            ExpiresAt = expiresAt,
            Reason = reason,
            CommandInputEmbedBuilder = commandInputEmbedBuilder
        };

        Embed commandLogEmbed = commandInputEmbedBuilder.Build();
        Guild guildEntity;

        await using (var db = new ApplicationDbContext())
        {
            List<User> users = [];
            if (perpetratorId.HasValue && !await db.Users.AnyAsync(perpetrator => perpetrator.Id == perpetratorId.Value && perpetrator.GuildId == context.Guild.Id))
            {
                users.Add(
                    new User
                    {
                        Id = perpetratorId.Value,
                        GuildId = context.Guild.Id
                    }
                );
            }

            if (targetId.HasValue && !await db.Users.AnyAsync(target => target.Id == targetId.Value && target.GuildId == context.Guild.Id))
            {
                users.Add(
                    new User
                    {
                        Id = targetId.Value,
                        GuildId = context.Guild.Id
                    }
                );
            }

            if (channelId.HasValue && !await db.Channels.AnyAsync(channel => channel.Id == channelId.Value))
            {
                await db.Channels.AddAsync(
                    new Channel
                    {
                        Id = channelId.Value,
                        GuildId = context.Guild.Id
                    }
                );
            }

            if (users.Count > 0)
            {
                await db.Users.AddRangeAsync(users);
            }

            IQueryable<Guild> guildQuery = db.Guilds.Where(guild => guild.Id == context.Guild.Id).Include(guild => guild.BotLogChannels);

            if (includeGuildCases)
            {
                guildQuery = guildQuery.Include(guild => guild.Cases);
            }

            Guild? guild = await guildQuery.SingleOrDefaultAsync();

            if (guild is null)
            {
                guild = new Guild
                {
                    Id = @case.GuildId
                };
                await db.Guilds.AddAsync(guild);
            }
            else if (guild.BotLogChannels.FirstOrDefault(channel => channel.Type.HasFlag(logType)) is { } botLogChannel
                  && context.Guild.Channels.FirstOrDefault(c => c.Id == botLogChannel.Id) is ITextChannel logChannel
                  && context.Guild.CurrentUser.GetPermissions(logChannel).Has(ChannelPermission.SendMessages))
            {
                IUserMessage message = await logChannel.SendMessageAsync(embeds: [commandLogEmbed]);
                @case.LogMessageUrl = message.GetJumpUrl();
            }

            guildEntity = guild;

            await db.Cases.AddAsync(@case);
            await db.SaveChangesAsync();
        }

        bool? successfulDm = null;

        if (targetId.HasValue)
        {
            try
            {
                RestUser? restUser = await context.Client.Rest.GetUserAsync(targetId.Value);

                if (restUser is null)
                {
                    successfulDm = false;
                }
                else
                {
                    await restUser.SendMessageAsync(
                        embeds: [commandLogEmbed],
                        components: new ComponentBuilder().WithButton(
                                                               $"Sent from {context.Guild.Name}".Truncate(ButtonBuilder.MaxButtonLabelLength),
                                                               "notice",
                                                               ButtonStyle.Secondary,
                                                               disabled: true
                                                           )
                                                          .Build()
                    );
                }
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                successfulDm = false;
            }
        }

        await context.Interaction.FollowupAsync(successfulDm == false ? "Unable to DM user." : null, [commandLogEmbed], components: GenerateLogMessageButton(@case));

        return guildEntity;
    }
}
