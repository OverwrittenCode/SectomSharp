using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.CompositeTypes;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Utils;

internal static class CaseUtils
{
    private static readonly Func<ApplicationDbContext, ulong, BotLogType, Task<ulong?>> GetGuildWithMatchedLogChannels =
        EF.CompileAsyncQuery((ApplicationDbContext context, ulong guildId, BotLogType logType) => context.Guilds.Where(g => g.Id == guildId)
                                                                                                         .Select(g => (ulong?)g.BotLogChannels
                                                                                                                    .Where(channel => channel.Type.HasFlag(logType))
                                                                                                                    .Select(channel => channel.Id)
                                                                                                                    .FirstOrDefault()
                                                                                                          )
                                                                                                         .FirstOrDefault()
        );

    /// <summary>
    ///     Generates a <see cref="MessageComponent" />.
    /// </summary>
    /// <param name="logMessageUrl">The logMessageUrl.</param>
    /// <returns>A component with a log message button if <paramref name="logMessageUrl" /> is not <c>null</c>; otherwise, an empty component.</returns>
    public static MessageComponent GenerateLogMessageButton(string? logMessageUrl)
    {
        var component = new ComponentBuilder();

        if (logMessageUrl is not null)
        {
            component.AddRow(
                new ActionRowBuilder().AddComponent(
                    new ButtonBuilder
                    {
                        Style = ButtonStyle.Link,
                        Label = "View Log Message",
                        Url = logMessageUrl
                    }.Build()
                )
            );
        }

        return component.Build();
    }

    /// <summary>
    ///     Creates a new case, logs it, and notifies relevant parties.
    /// </summary>
    /// <param name="dbFactory">The db factory.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="logType">The log type.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="targetId">The id of the targeted user.</param>
    /// <param name="channelId">The targeted channel.</param>
    /// <param name="expiresAt">When the case has expired.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>The guild log context if the guild exists.</returns>
    public static async Task LogAsync(
        IDbContextFactory<ApplicationDbContext> dbFactory,
        SocketInteractionContext context,
        BotLogType logType,
        OperationType operationType,
        ulong? targetId = null,
        ulong? channelId = null,
        DateTimeOffset? expiresAt = null,
        string? reason = null
    )
    {
        await using ApplicationDbContext db = await dbFactory.CreateDbContextAsync();
        await LogAsync(db, context, logType, operationType, targetId, channelId, expiresAt, reason);
    }

    /// <summary>
    ///     Creates a new case, logs it, and notifies relevant parties.
    /// </summary>
    /// <param name="db">The db instance.</param>
    /// <param name="context">The interaction context.</param>
    /// <param name="logType">The log type.</param>
    /// <param name="operationType">The operation type.</param>
    /// <param name="targetId">The id of the targeted user.</param>
    /// <param name="channelId">The targeted channel.</param>
    /// <param name="expiresAt">When the case has expired.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>The guild log context if the guild exists.</returns>
    public static async Task LogAsync(
        ApplicationDbContext db,
        SocketInteractionContext context,
        BotLogType logType,
        OperationType operationType,
        ulong? targetId = null,
        ulong? channelId = null,
        DateTimeOffset? expiresAt = null,
        string? reason = null
    )
    {
        ulong? perpetratorId = context.User.Id == context.Client.CurrentUser.Id ? null : context.User.Id;

        var command = (SocketSlashCommand)context.Interaction;
        var commandMentionArguments = new List<string>(3) { command.CommandName };

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

        List<EmbedFieldBuilder> commandFields = new(options.Count);
        var compositeEmbedFields = new CompositeEmbedField[options.Count];
        int index = 0;
        foreach (SocketSlashCommandDataOption option in options)
        {
            string value = option.Value switch
            {
                IMentionable mentionable => $"{mentionable.Mention} ({mentionable})",
                SocketEntity<ulong> entity => $"{entity.Id} ({entity})",
                _ => option.Value.ToString() ?? "Unknown"
            };

            EmbedFieldBuilder embedFieldBuilder = EmbedFieldBuilderFactory.Create(option.Name, value);
            commandFields.Add(embedFieldBuilder);
            compositeEmbedFields[index++] = new CompositeEmbedField(option.Name, value);
        }

        string caseId = StringUtils.GenerateUniqueId();

        string description = $"</{String.Join(' ', commandMentionArguments)}:{command.CommandId}>";
        var commandInputEmbedBuilder = new EmbedBuilder
        {
            Description = description,
            Author = new EmbedAuthorBuilder { Name = $"{logType}{operationType} | {caseId}" },
            Fields = commandFields,
            Timestamp = DateTimeOffset.UtcNow
        };

        string? displayAvatarUrl = null;
        Color color;
        if (perpetratorId.HasValue)
        {
            color = operationType == OperationType.Update ? Color.Orange : Color.Red;
            displayAvatarUrl = context.User.GetDisplayAvatarUrl();
            commandInputEmbedBuilder.ThumbnailUrl = displayAvatarUrl;
            commandInputEmbedBuilder.Footer = new EmbedFooterBuilder { Text = $"Perpetrator: {perpetratorId.Value}" };
        }
        else
        {
            color = Color.Purple;
        }

        commandInputEmbedBuilder.Color = color;
        Embed commandLogEmbed = commandInputEmbedBuilder.Build();

        ulong guildId = context.Guild.Id;
        string? logMessageUrl = null;
        ulong? botLogChannelId = await GetGuildWithMatchedLogChannels(db, guildId, logType);

        if (botLogChannelId.HasValue
         && context.Guild.Channels.FirstOrDefault(c => c.Id == botLogChannelId) is ITextChannel logChannel
         && context.Guild.CurrentUser.GetPermissions(logChannel).Has(ChannelPermission.SendMessages))
        {
            IUserMessage userMessage = await logChannel.SendMessageAsync(embeds: [commandLogEmbed]);
            logMessageUrl = userMessage.GetJumpUrl();
        }

        await db.Database.ExecuteSqlRawAsync(
            """
            WITH
                guild_upsert AS (
                    INSERT INTO "Guilds" ("Id")
                    SELECT @guildId
                    WHERE @guildNotExists
                    ON CONFLICT ("Id") DO NOTHING
                ),
                channel_upsert AS (
                    INSERT INTO "Channels" ("GuildId", "Id")
                    SELECT @guildId, @channelId
                    WHERE @channelIdHasValue
                    ON CONFLICT ("Id") DO NOTHING
                ),
                users_upsert AS (
                    INSERT INTO "Users" ("GuildId", "Id")
                    SELECT users."GuildId", users."Id"
                    FROM (VALUES
                        (@guildId, @perpetratorId),
                        (@guildId, @targetId)
                    ) AS users("GuildId", "Id")
                    WHERE users."Id" IS NOT NULL
                    ON CONFLICT ("GuildId", "Id") DO NOTHING
                )
            INSERT INTO "Cases" (
                "GuildId",
                "Id", 
                "PerpetratorId",
                "TargetId",
                "ChannelId",
                "LogType",
                "OperationType", 
                "ExpiresAt",
                "Reason",
                "LogMessageUrl",
                "Fields",
                "Color",
                "Description",
                "PerpetratorAvatarUrl"
            )
            VALUES (
                @guildId,
                @caseId,
                @perpetratorId,
                @targetId,
                @caseChannelId,
                @logType,
                @operationType,
                @expiresAt,
                @reason,
                @logMessageUrl,
                @fields,
                @color,
                @description,
                @perpetratorAvatarUrl
            );
            """,
            NpgsqlParameterFactory.FromSnowflakeId("guildId", guildId),
            NpgsqlParameterFactory.FromBoolean("guildNotExists", !botLogChannelId.HasValue),
            NpgsqlParameterFactory.FromSnowflakeId("channelId", channelId),
            NpgsqlParameterFactory.FromBoolean("channelIdHasValue", channelId.HasValue),
            NpgsqlParameterFactory.FromSnowflakeId("perpetratorId", perpetratorId),
            NpgsqlParameterFactory.FromSnowflakeId("targetId", targetId),
            NpgsqlParameterFactory.FromVarchar("caseId", caseId),
            NpgsqlParameterFactory.FromSnowflakeId("caseChannelId", channelId),
            NpgsqlParameterFactory.FromEnum32("logType", logType),
            NpgsqlParameterFactory.FromEnum32("operationType", operationType),
            NpgsqlParameterFactory.FromDateTimeOffset("expiresAt", expiresAt),
            NpgsqlParameterFactory.FromVarchar("reason", reason),
            NpgsqlParameterFactory.FromVarchar("logMessageUrl", logMessageUrl),
            NpgsqlParameterFactory.FromCompositeArray("fields", compositeEmbedFields, $"{CompositeEmbedField.PgName}[]"),
            NpgsqlParameterFactory.FromNonNegativeInt32("color", color.RawValue),
            NpgsqlParameterFactory.FromVarchar("description", description),
            NpgsqlParameterFactory.FromVarchar("perpetratorAvatarUrl", displayAvatarUrl)
        );

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
                        components: new ComponentBuilder
                        {
                            ActionRows =
                            [
                                new ActionRowBuilder
                                {
                                    Components =
                                    [
                                        new ButtonBuilder(
                                            $"Sent from {context.Guild.Name}".Truncate(ButtonBuilder.MaxButtonLabelLength),
                                            "notice",
                                            ButtonStyle.Secondary,
                                            isDisabled: true
                                        ).Build()
                                    ]
                                }
                            ]
                        }.Build()
                    );
                }
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                successfulDm = false;
            }
        }

        await context.Interaction.FollowupAsync(successfulDm == false ? "Unable to DM user." : null, [commandLogEmbed], components: GenerateLogMessageButton(logMessageUrl));
    }
}
