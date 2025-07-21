using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Net;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Graphics;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private static readonly Func<ILogger, string, string, string, IDisposable?> MessageReceivedScopeCallback =
        LoggerMessage.DefineScope<string, string, string>("Event=MessageReceived, User={Username}, Guild={GuildName}, Channel={ChannelName}");

    private static readonly RequestOptions AutoRoleRequestOptions = new() { AuditLogReason = "Configured auto-role" };

    public async Task HandleMessageReceivedAsync(SocketMessage msg)
    {
        if (msg is not SocketUserMessage { Author: SocketGuildUser { IsBot: false } author, Channel: IGuildChannel { Guild: { } guild } channel } message)
        {
            return;
        }

        long[] roleIds = author.Roles.Select(r => (long)r.Id).ToArray();
        uint newLevel = 0;
        uint currentXp = 0;
        uint requiredXp = 0;
        uint rank = 0;
        ulong? roleId = null;
        bool hasRows;
        Stopwatch stopwatch;
        await using (ApplicationDbContext db = await _dbFactory.CreateDbContextAsync())
        {
            await db.Database.OpenConnectionAsync();
            await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = """
                                  WITH
                                      leveling_data AS (
                                          SELECT
                                              g."Id" AS "GuildId",
                                              @userId AS "UserId",
                                              COALESCE(ra."MaxRoleCooldown", g."Configuration_Leveling_GlobalCooldown") AS "Cooldown",
                                              GREATEST(
                                                  CASE
                                                      WHEN g."Configuration_Leveling_AccumulateMultipliers" THEN COALESCE(ra."SumMultiplier", 0)
                                                      ELSE COALESCE(ra."MaxMultiplier", 0)
                                                  END,
                                                  g."Configuration_Leveling_GlobalMultiplier"
                                              ) AS "Multiplier",
                                              COALESCE(u."Level_CurrentXp", 0) AS "CurrentXp",
                                              u."Level_UpdatedAt" AS "UpdatedAt"
                                          FROM "Guilds" g
                                          LEFT JOIN "Users" u ON u."GuildId" = g."Id" AND u."Id" = @userId
                                          LEFT JOIN (
                                              SELECT
                                                  r."GuildId",
                                                  MAX(r."Cooldown") AS "MaxRoleCooldown",
                                                  SUM(COALESCE(r."Multiplier", 0)) AS "SumMultiplier",
                                                  MAX(COALESCE(r."Multiplier", 0)) AS "MaxMultiplier"
                                              FROM "LevelingRoles" r
                                              WHERE r."Id" = ANY(@roleIds)
                                              GROUP BY r."GuildId"
                                          ) ra ON ra."GuildId" = g."Id"
                                          WHERE
                                              g."Id" = @guildId
                                              AND NOT g."Configuration_Leveling_IsDisabled"
                                      ),
                                      xp_calculation AS (
                                          SELECT
                                              ld.*,
                                              get_level(ld."CurrentXp") AS "CurrentLevel",
                                              get_xp_gain(ld."Multiplier") AS "XpGain"
                                          FROM leveling_data ld
                                          WHERE
                                              ld."UpdatedAt" IS NULL
                                              OR @now - ld."UpdatedAt" >= INTERVAL '1 second' * ld."Cooldown"
                                      ),
                                      updated_xp AS (
                                          SELECT
                                              xc.*,
                                              xc."CurrentXp" + xc."XpGain" AS "NewXp"
                                          FROM xp_calculation xc
                                      ),
                                      level_check AS (
                                          SELECT
                                              ux.*,
                                              CASE
                                                  WHEN ux."NewXp" >= get_required_xp(ux."CurrentLevel") THEN ux."CurrentLevel" + 1
                                                  ELSE ux."CurrentLevel"
                                              END AS "NewLevel"
                                          FROM updated_xp ux
                                      ),
                                      user_update AS (
                                          INSERT INTO "Users" ("Id", "GuildId", "Level_CurrentXp", "Level_UpdatedAt")
                                          SELECT lc."UserId", lc."GuildId", lc."NewXp", @now
                                          FROM level_check lc
                                          ON CONFLICT ("GuildId", "Id") DO UPDATE SET
                                              "Level_CurrentXp" = EXCLUDED."Level_CurrentXp",
                                              "Level_UpdatedAt" = EXCLUDED."Level_UpdatedAt"
                                          RETURNING "GuildId", "Id"
                                      ),
                                      user_rank AS (
                                          SELECT
                                              u."Id",
                                              u."GuildId",
                                              u."Level_CurrentXp",
                                              ROW_NUMBER() OVER (
                                                  PARTITION BY u."GuildId" 
                                                  ORDER BY u."Level_CurrentXp" DESC
                                              ) AS "Rank"
                                          FROM "Users" u
                                          WHERE
                                              u."GuildId" = @guildId
                                              AND u."Level_CurrentXp" > 0
                                      )
                                  SELECT
                                      lc."NewLevel",
                                      lc."NewXp" AS "CurrentXp",
                                      get_required_xp(lc."NewLevel") AS "RequiredXp",
                                      COALESCE(ur."Rank", 1) AS "Rank",
                                      lr."Id" AS "NewRoleId"
                                  FROM level_check lc
                                  LEFT JOIN user_rank ur ON ur."GuildId" = lc."GuildId" AND ur."Id" = lc."UserId"
                                  LEFT JOIN LATERAL (
                                      SELECT r."Id"
                                      FROM "LevelingRoles" r
                                      WHERE
                                          r."GuildId" = lc."GuildId"
                                          AND r."Level" <= lc."NewLevel"
                                      ORDER BY r."Level" DESC
                                      LIMIT 1
                                  ) lr ON true
                                  WHERE
                                      lc."NewLevel" > lc."CurrentLevel"
                                      AND EXISTS (
                                          SELECT 1 FROM user_update uu
                                          WHERE
                                              uu."GuildId" = lc."GuildId"
                                              AND uu."Id" = lc."UserId"
                                      )
                                  """;
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", guild.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("userId", author.Id));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromInt64Array("roleIds", roleIds));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromDateTimeOffset("now", message.CreatedAt));
                await cmd.PrepareAsync();

                stopwatch = Stopwatch.StartNew();
                await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                {
                    hasRows = await reader.ReadAsync();
                    if (hasRows)
                    {
                        newLevel = (uint)reader.GetInt32(0);
                        currentXp = (uint)reader.GetInt32(1);
                        requiredXp = (uint)reader.GetInt32(2);
                        rank = (uint)reader.GetInt32(3);

                        if (!reader.IsDBNull(4))
                        {
                            roleId = (ulong?)reader.GetInt64(4);
                        }
                    }

                    stopwatch.Stop();
                }
            }
        }

        using (MessageReceivedScopeCallback(_logger, author.Username, guild.Name, channel.Name))
        {
            _logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
        }

        if (!hasRows)
        {
            return;
        }

        IGuildUser bot = await guild.GetCurrentUserAsync();
        if (roleId is { } autoRoleId
         && bot.GuildPermissions.Has(GuildPermission.ManageRoles)
         && guild.Roles.FirstOrDefault(r => r.Id == autoRoleId) is { } role
         && bot.Hierarchy > role.Position
         && !roleIds.Contains((long)autoRoleId))
        {
            try
            {
                await author.AddRoleAsync(autoRoleId, AutoRoleRequestOptions);
            }
            catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions) { }
        }

        if (!bot.GetPermissions(channel).Has(ChannelPermission.SendMessages))
        {
            return;
        }

        var rankCardBuilder = new RankCardBuilder
        {
            User = author,
            Level = newLevel,
            Rank = rank,
            CurrentXp = currentXp,
            RequiredXp = requiredXp
        };
        byte[] imageBytes = await rankCardBuilder.BuildAsync();
        using var stream = new MemoryStream(imageBytes);
        try
        {
            await msg.Channel.SendFileAsync(stream, "RankCard.png", $"<@{author.Id}> has ranked up to **Level {newLevel}** 🎉");
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions) { }
    }

    public async Task HandleMessageDeletedAsync(Cacheable<IMessage, ulong> partialMessage, Cacheable<IMessageChannel, ulong> _)
    {
        if (partialMessage is not { Value: { Author.IsBot: false, Channel: IGuildChannel { Guild: { } guild } } message })
        {
            return;
        }

        var builders = new List<EmbedFieldBuilder>(8)
        {
            EmbedFieldBuilderFactory.Create("Channel Id", message.Channel.Id),
            EmbedFieldBuilderFactory.Create("Author", $"<@{message.Author.Id}>"),
            EmbedFieldBuilderFactory.Create("Created At", message.Timestamp.GetRelativeTimestamp()),
            EmbedFieldBuilderFactory.CreateTruncated("Content", message.Content)
        };

        if (message.MentionedChannelIds.Count > 0)
        {
            builders.Add(EmbedFieldBuilderFactory.CreateTruncated("Mentioned Channels", String.Join(", ", message.MentionedChannelIds.Select(id => $"<#{id}>"))));
        }

        if (message.MentionedRoleIds.Count > 0)
        {
            builders.Add(EmbedFieldBuilderFactory.CreateTruncated("Mentioned Roles", String.Join(", ", message.MentionedRoleIds.Select(id => $"<@&{id}>"))));
        }

        if (message.MentionedUserIds.Count > 0)
        {
            builders.Add(EmbedFieldBuilderFactory.CreateTruncated("Mentioned Users", String.Join(", ", message.MentionedUserIds.Select(id => $"<@{id}>"))));
        }

        if (message.MentionedEveryone)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Mentioned Everyone", message.MentionedEveryone));
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guild.Id, AuditLogType.Message);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(guild, webhookClient, AuditLogType.Message, OperationType.Delete, builders, message.Id, message.Author.Username, message.Author.GetDisplayAvatarUrl());
    }

    public async Task HandleMessageUpdatedAsync(Cacheable<IMessage, ulong> oldPartialMessage, SocketMessage newMessage, ISocketMessageChannel _)
    {
        if (oldPartialMessage is not { Value: { Author.IsBot: false, Channel: SocketGuildChannel { Guild: { } guild } } oldMessage })
        {
            return;
        }

        if (oldMessage.Content == newMessage.Content)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(guild.Id, AuditLogType.Message);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(
            guild,
            webhookClient,
            AuditLogType.Message,
            OperationType.Update,
            [EmbedFieldBuilderFactory.CreateTruncated("Content", GetChangeEntry(oldMessage.Content, newMessage.Content))],
            newMessage.Id,
            newMessage.Author.Username,
            newMessage.Author.GetDisplayAvatarUrl()
        );
    }
}
