using Discord;
using Discord.Interactions;
using Discord.Net;
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
    ///     Generates two log embeds from a given <see cref="Case"/>.
    /// </summary>
    /// <param name="case">The case.</param>
    /// <param name="serverLogEmbed">A complete log containing all the information.</param>
    /// <param name="dmLogEmbed">
    ///     A partial log that omits the target field and
    ///     omits the perpetrator field for anonymity.
    /// </param>
    public static void GenerateLogEmbeds(Case @case, out Embed serverLogEmbed, out Embed dmLogEmbed)
    {
        var timestampKvp = new Dictionary<string, object>()
        {
            {
                "Created At",
                TimestampTag.FormatFromDateTime(@case.CreatedAt, TimestampTagStyles.Relative)
            },
        };

        if (@case.UpdatedAt is DateTime updatedAt)
        {
            timestampKvp["Updated At"] = TimestampTag.FormatFromDateTime(
                updatedAt,
                TimestampTagStyles.Relative
            );
        }

        if (@case.ExpiresAt is DateTime expiresAt)
        {
            timestampKvp["Expires At"] = TimestampTag.FormatFromDateTime(
                expiresAt,
                TimestampTagStyles.Relative
            );
        }

        EmbedBuilder GetBaseEmbed() =>
            new EmbedBuilder()
                .WithTimestamp(@case.CreatedAt)
                .WithColor(
                    @case.PerpetratorId is null ? Color.Purple
                    : @case.OperationType == OperationType.Update ? Color.Orange
                    : Color.Red
                )
                .AddIndentedField("Timestamps", timestampKvp);

        var serverLogEmbedBuilder = GetBaseEmbed();
        var dmLogEmbedBuilder = GetBaseEmbed();

        dmLogEmbedBuilder.Description += " against you";

        if (@case.TargetId is ulong targetId)
        {
            serverLogEmbedBuilder.AddIndentedField(
                "Target",
                new()
                {
                    { "User ID", DiscordUtils.GetHyperlinkedUserProfile(targetId) },
                    { "User Mention", MentionUtils.MentionUser(targetId) },
                }
            );
        }

        var title = $"CASE {@case.Id}";
        var actionDisplayText = Format.Code($"{@case.LogType}{@case.OperationType}");
        var channelMention = MentionUtils.MentionChannel(@case.ChannelId);
        Color colour;

        if (@case.PerpetratorId is ulong perpetratorId)
        {
            colour = @case.OperationType == OperationType.Update ? Color.Orange : Color.Red;

            var dmDescription = $"{actionDisplayText} was invoked";

            dmLogEmbedBuilder.WithDescription(dmDescription);

            serverLogEmbedBuilder
                .WithDescription(
                    $"{dmDescription} by {MentionUtils.MentionUser(perpetratorId)} in {channelMention}"
                )
                .AddIndentedField(
                    "Perpetrator",
                    new()
                    {
                        { "User ID", DiscordUtils.GetHyperlinkedUserProfile(perpetratorId) },
                        { "User Mention", MentionUtils.MentionUser(perpetratorId) },
                    }
                );
        }
        else
        {
            var dmDescription = $"I invoked {actionDisplayText}";

            dmLogEmbedBuilder.WithDescription(dmDescription);
            serverLogEmbedBuilder.WithDescription($"{dmDescription} in {channelMention}");

            colour = Color.Purple;
            title = $"[AUTO] {title}";
        }

        dmLogEmbedBuilder.Color = serverLogEmbedBuilder.Color = colour;
        dmLogEmbedBuilder.Title = serverLogEmbedBuilder.Title = title;

        if (@case.Reason is string reason)
        {
            serverLogEmbedBuilder.AddField("Reason", reason);
            dmLogEmbedBuilder.AddField("Reason", reason);
        }

        serverLogEmbed = serverLogEmbedBuilder.Build();
        dmLogEmbed = dmLogEmbedBuilder.Build();
    }

    /// <summary>
    ///     Generates a <see cref="MessageComponent"/>.
    /// </summary>
    /// <inheritdoc cref="GenerateLogEmbedTemplate(Case)" path="/param"/>
    /// <returns>A <see cref="MessageComponent"/>.</returns>
    /// <value>
    ///     If <see cref="Case.LogMessageId"/> is <see langword="ulong"/>;
    ///         a component with the log message button.<br/>
    ///     If <see cref="Case.LogMessageId"/> is <see langword="null"/>;
    ///         an empty component.
    /// </value>
    public static MessageComponent GenerateLogMessageButton(Case @case)
    {
        var component = new ComponentBuilder();

        if (@case.LogMessageId is ulong logMessageId)
        {
            component.AddRow(
                new ActionRowBuilder().AddComponent(
                    new ButtonBuilder()
                    {
                        Style = ButtonStyle.Link,
                        Label = "View Log Message",
                        Url = DiscordUtils.GetMessageURL(
                            @case.GuildId,
                            @case.ChannelId,
                            logMessageId
                        ),
                    }.Build()
                )
            );
        }

        return component.Build();
    }

    /// <summary>
    ///     Creates a new case, logs it, and notifies relevant parties.
    /// </summary>
    /// <inheritdoc cref="GenerateLogEmbedTemplate(Case)" path="/param"/>
    /// <param name="context">The interactionContext interactionContext.</param>
    /// <param name="logType">The log type.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="targetId">The targeted user.</param>
    /// <param name="expiresAt">When the case has expired.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="includeGuildCases">If <see cref="Guild.Cases"/> should be included.</param>
    /// <returns>
    ///     The current <see cref="Guild"/> entity.
    /// </returns>
    /// <value>
    ///     Includes <see cref="Guild.BotLogChannels"/>.<br/>
    ///     If <paramref name="includeGuildCases"/> is <see langword="true"/>;
    ///     <see cref="Guild.Cases"/> will be included.
    /// </value>
    public static async Task<Guild> LogAsync(
        SocketInteractionContext context,
        BotLogType logType,
        OperationType operationType,
        ulong? perpetratorId = null,
        ulong? targetId = null,
        DateTime? expiresAt = null,
        string? reason = null,
        bool includeGuildCases = false
    )
    {
        if (!context.Interaction.HasResponded)
        {
            await context.Interaction.DeferAsync();
        }

        var perpetratorKey = perpetratorId ??= context.User.Id;

        var @case = new Case()
        {
            Id = StringUtils.GenerateUniqueId(IdLength),
            PerpetratorId = perpetratorKey,
            TargetId = targetId,
            ChannelId = context.Channel.Id,
            GuildId = context.Guild.Id,
            LogType = logType,
            OperationType = operationType,
            ExpiresAt = expiresAt,
            Reason = reason,
        };

        GenerateLogEmbeds(@case, out var serverLogEmbed, out var dmLogEmbed);

        Guild guildEntity;

        using (var db = new ApplicationDbContext())
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
                users.Add(new() { Id = perpetratorKey, GuildId = context.Guild.Id });
            }

            if (
                targetId is ulong targetKey
                && (
                    await db
                        .Users.Where(target =>
                            target.Id == targetKey && target.GuildId == context.Guild.Id
                        )
                        .SingleOrDefaultAsync()
                    is null
                )
            )
            {
                users.Add(new() { Id = targetKey, GuildId = context.Guild.Id });
            }

            if (users.Count > 0)
            {
                await db.Users.AddRangeAsync(users);
            }

            if (await db.Channels.FindAsync(context.Channel.Id) is null)
            {
                await db.Channels.AddAsync(
                    new() { Id = context.Channel.Id, GuildId = context.Guild.Id }
                );
            }

            IQueryable<Guild> guildQuery = db
                .Guilds.Where(guild => guild.Id == context.Guild.Id)
                .Include(guild => guild.BotLogChannels);

            if (includeGuildCases)
            {
                guildQuery = guildQuery.Include(guild => guild.Cases);
            }

            var guild = await guildQuery.SingleOrDefaultAsync();

            if (guild is null)
            {
                guild = new() { Id = @case.GuildId };
                await db.Guilds.AddAsync(guild);
            }
            else if (
                guild.BotLogChannels.FirstOrDefault(channel =>
                    channel.BotLogType == logType
                    && (channel.OperationType is null || channel.OperationType == operationType)
                )
                    is BotLogChannel botLogChannel
                && context.Guild.Channels.FirstOrDefault(c => c.Id == botLogChannel.Id)
                    is ITextChannel logChannel
                && context
                    .Guild.CurrentUser.GetPermissions(logChannel)
                    .Has(ChannelPermission.SendMessages)
            )
            {
                var component = new ComponentBuilder().WithButton(
                    $"Sent from {context.Guild.Name}".Truncate(ButtonBuilder.MaxButtonLabelLength)
                );

                var message = await logChannel.SendMessageAsync(embeds: [serverLogEmbed]);
                @case.LogMessageId = message.Id;
            }

            guildEntity = guild;

            await db.Cases.AddAsync(@case);
            await db.SaveChangesAsync();
        }

        bool? successfulDm = null;

        if (targetId is not null)
        {
            try
            {
                await context
                    .Guild.Users.Single(user => user.Id == targetId)
                    .SendMessageAsync(embeds: [dmLogEmbed]);
            }
            catch (HttpException ex)
                when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                successfulDm = false;
            }
        }

        var successEmbed = new EmbedBuilder()
        {
            Description = serverLogEmbed.Description,
            Color = serverLogEmbed.Color,
            Footer = new() { Text = serverLogEmbed.Footer?.Text },
        };

        if (successfulDm == false)
        {
            successEmbed.AddField("DM Status", "Unable to DM user.");
        }

        await context.Interaction.FollowupAsync(
            embeds: [successEmbed.Build()],
            components: GenerateLogMessageButton(@case)
        );

        return guildEntity;
    }
}
