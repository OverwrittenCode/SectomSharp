using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
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
    ///     Generates two log embeds from a given <see cref="Case" />.
    /// </summary>
    /// <param name="case">The case.</param>
    /// <returns>A tuple with the necessary embed information.</returns>
    public static (EmbedBuilder DMLog, EmbedBuilder ServerLog) GenerateLogEmbeds(Case @case)
    {
        Dictionary<string, object> kvp = [];

        if (@case.UpdatedAt is { } updatedAt)
        {
            kvp["Modified"] = TimestampTag.FormatFromDateTime(
                updatedAt,
                TimestampTagStyles.Relative
            );
        }

        if (@case.ExpiresAt is { } expiresAt)
        {
            kvp["Expiry"] = TimestampTag.FormatFromDateTime(expiresAt, TimestampTagStyles.Relative);
        }

        if (@case.ChannelId is { } channelId)
        {
            kvp["Channel"] = MentionUtils.MentionChannel(channelId);
        }

        Color colour =
            @case.PerpetratorId is null ? Color.Purple
            : @case.OperationType == OperationType.Update ? Color.Orange
            : Color.Red;

        EmbedBuilder dmLog = GetEmbed();

        if (@case.PerpetratorId is { } perpetratorId)
        {
            kvp["Perpetrator"] = MentionUtils.MentionUser(perpetratorId);
        }

        if (@case.TargetId is { } targetId)
        {
            kvp["Target"] = MentionUtils.MentionUser(targetId);
        }

        EmbedBuilder serverLog = GetEmbed();

        return (dmLog, serverLog);

        EmbedBuilder GetEmbed() =>
            new EmbedBuilder()
                .WithColor(colour)
                .WithDescription(
                    String.Join(
                        "\n",
                        kvp.Concat([new("Reason", @case.Reason ?? "No reason provided")])
                            .Select(pair => $"{Format.Bold($"{pair.Key}:")} {pair.Value}")
                    )
                )
                .WithFooter($"{@case.Id} | {@case.LogType}{@case.OperationType}")
                .WithTimestamp(@case.CreatedAt);
    }

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
                        Style = ButtonStyle.Link, Label = "View Log Message", Url = url
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
        ulong? perpetratorId = null,
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

        var perpetratorKey = perpetratorId ?? context.User.Id;

        var @case = new Case
        {
            Id = StringUtils.GenerateUniqueId(),
            PerpetratorId = perpetratorKey,
            TargetId = targetId,
            ChannelId = channelId,
            GuildId = context.Guild.Id,
            LogType = logType,
            OperationType = operationType,
            ExpiresAt = expiresAt,
            Reason = reason
        };

        (EmbedBuilder dmLogEmbed, EmbedBuilder serverLogEmbed) = GenerateLogEmbeds(@case);

        Guild guildEntity;

        await using (var db = new ApplicationDbContext())
        {
            List<User> users = [];

            if (
                await db
                    .Users.Where(perpetrator =>
                        perpetrator.Id == perpetratorKey && perpetrator.GuildId == context.Guild.Id
                    )
                    .SingleOrDefaultAsync()
                is null
            )
            {
                users.Add(new()
                {
                    Id = perpetratorKey, GuildId = context.Guild.Id
                });
            }

            if (
                targetId is { } targetKey1
                && await db
                        .Users.Where(target =>
                            target.Id == targetKey1 && target.GuildId == context.Guild.Id
                        )
                        .SingleOrDefaultAsync()
                    is null
            )
            {
                users.Add(new()
                {
                    Id = targetKey1, GuildId = context.Guild.Id
                });
            }

            if (users.Count > 0)
            {
                await db.Users.AddRangeAsync(users);
            }

            IQueryable<Guild> guildQuery = db
                .Guilds.Where(guild => guild.Id == context.Guild.Id)
                .Include(guild => guild.BotLogChannels);

            if (includeGuildCases)
            {
                guildQuery = guildQuery.Include(guild => guild.Cases);
            }

            Guild? guild = await guildQuery.SingleOrDefaultAsync();

            if (guild is null)
            {
                guild = new()
                {
                    Id = @case.GuildId
                };
                await db.Guilds.AddAsync(guild);
            }
            else if (
                guild.BotLogChannels.FirstOrDefault(channel => channel.BotLogType.HasFlag(logType))
                    is { } botLogChannel
                && context.Guild.Channels.FirstOrDefault(c => c.Id == botLogChannel.Id)
                    is ITextChannel logChannel
                && context
                    .Guild.CurrentUser.GetPermissions(logChannel)
                    .Has(ChannelPermission.SendMessages)
            )
            {
                IUserMessage message = await logChannel.SendMessageAsync(
                    embeds: [serverLogEmbed.Build()]
                );
                @case.LogMessageUrl = message.GetJumpUrl();
            }

            guildEntity = guild;

            await db.Cases.AddAsync(@case);
            await db.SaveChangesAsync();
        }

        bool? successfulDm = null;

        if (targetId is { } targetKey2)
        {
            ComponentBuilder component = new ComponentBuilder().WithButton(
                $"Sent from {context.Guild.Name}".Truncate(ButtonBuilder.MaxButtonLabelLength),
                "notice",
                ButtonStyle.Secondary,
                disabled: true
            );

            try
            {
                RestUser? restUser = await context.Client.Rest.GetUserAsync(targetKey2);

                if (restUser is null)
                {
                    successfulDm = false;
                }
                else
                {
                    await restUser.SendMessageAsync(
                        embeds: [dmLogEmbed.Build()],
                        components: component.Build()
                    );
                }
            }
            catch (HttpException ex)
                when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                successfulDm = false;
            }
        }

        if (successfulDm == false)
        {
            serverLogEmbed.AddField("DM Status", "Unable to DM user.");
        }

        await context.Interaction.FollowupAsync(
            embeds: [serverLogEmbed.Build()],
            components: GenerateLogMessageButton(@case)
        );

        return guildEntity;
    }
}
