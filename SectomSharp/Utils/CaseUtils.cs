using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Utils;

internal static class CaseUtils
{
    /// <summary>
    ///     Generates a <see cref="MessageComponent" />.
    /// </summary>
    /// <param name="case">The case.</param>
    /// <returns>A component with a log message button if <see cref="Case.LogMessageUrl" /> is not <c>null</c>; otherwise, an empty component.</returns>
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
    /// <param name="context">The interaction context.</param>
    /// <param name="logType">The log type.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="targetId">The id of the targeted user.</param>
    /// <param name="channelId">The targeted channel.</param>
    /// <param name="expiresAt">When the case has expired.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="includeGuildCases">If <see cref="Guild.Cases" /> should be included.</param>
    /// <returns>
    ///     The current <see cref="Guild" /> entity with <see cref="Guild.BotLogChannels" /> included.<br />
    ///     If <paramref name="includeGuildCases" /> is <c>true</c>, <see cref="Guild.Cases" /> will be included.
    /// </returns>
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
            string value = option.Value switch
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

        string caseId = StringUtils.GenerateUniqueId();

        EmbedBuilder commandInputEmbedBuilder = new EmbedBuilder().WithDescription($"</{String.Join(' ', commandMentionArguments)}:{command.CommandId}>")
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
            if (perpetratorId is { } perpetratorIdValue && !await db.Users.AnyAsync(perpetrator => perpetrator.Id == perpetratorIdValue && perpetrator.GuildId == context.Guild.Id))
            {
                users.Add(
                    new User
                    {
                        Id = perpetratorIdValue,
                        GuildId = context.Guild.Id
                    }
                );
            }

            if (targetId is { } targetIdValue && !await db.Users.AnyAsync(target => target.Id == targetIdValue && target.GuildId == context.Guild.Id))
            {
                users.Add(
                    new User
                    {
                        Id = targetIdValue,
                        GuildId = context.Guild.Id
                    }
                );
            }

            if (channelId is { } channelIdValue && !await db.Channels.AnyAsync(channel => channel.Id == channelIdValue))
            {
                await db.Channels.AddAsync(
                    new Channel
                    {
                        Id = channelIdValue,
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
                IUserMessage userMessage = await logChannel.SendMessageAsync(embeds: [commandLogEmbed]);
                @case.LogMessageUrl = userMessage.GetJumpUrl();
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
