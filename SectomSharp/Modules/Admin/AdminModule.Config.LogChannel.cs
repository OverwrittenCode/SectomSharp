using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("log-channel", "Log Channel configuration")]
        public sealed class LogChannelModule : BaseModule<LogChannelModule>
        {
            private readonly ILoggerFactory _loggerFactory;

            /// <inheritdoc />
            public LogChannelModule(ILogger<LogChannelModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory, ILoggerFactory loggerFactory) : base(
                logger,
                dbContextFactory
            )
                => _loggerFactory = loggerFactory;

            /// <summary>
            ///     Represents the result of an audit log upsert.
            /// </summary>
            private enum AuditLogUpsertResult
            {
                /// <summary>
                ///     No rows were affected.
                /// </summary>
                NoChange = 0,

                /// <summary>
                ///     The flag has been updated.
                /// </summary>
                Updated = 1,

                /// <summary>
                ///     An audit log channel has been inserted with a temporary empty string for the webhook url.
                ///     A webhook url is required.
                /// </summary>
                InsertedAndNeedsWebhookUrl = 2
            }

            [SlashCmd("Add or modify a bot log channel configuration")]
            public async Task SetBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out SocketTextChannel channel, out BotLogType action, out string? reason);

                await DeferAsync();
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
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
                                                  RETURNING 1
                                              ),
                                              channel_upsert AS (
                                                  INSERT INTO "BotLogChannels" ("Id", "GuildId", "Type")
                                                  VALUES (@channelId, @guildId, @action)
                                                  ON CONFLICT ("Id") DO UPDATE SET "Type" = EXCLUDED."Type" | "BotLogChannels"."Type"
                                                  WHERE
                                                      EXISTS (SELECT 1 FROM guild_upsert)
                                                      OR ("BotLogChannels"."Type" & @action) = 0
                                                  RETURNING (xmax = 0) AS "is_insert"
                                              )
                                          SELECT is_insert FROM channel_upsert;
                                          """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                        stopwatch = Stopwatch.StartNew();
                        scalarResult = await cmd.ExecuteScalarAsync();
                        stopwatch.Stop();
                    }

                    Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                    if (scalarResult is bool isInsert)
                    {
                        if (isInsert)
                        {
                            await LogCreateAsync(db, Context, reason);
                        }
                        else
                        {
                            await LogUpdateAsync(db, Context, reason);
                        }

                        return;
                    }
                }

                await FollowupAsync(AlreadyConfiguredMessage);
            }

            [SlashCmd("Add or modify an audit log channel configuration")]
            [RequireBotPermission(GuildPermission.ViewAuditLog)]
            public async Task SetAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out SocketTextChannel channel, out AuditLogType action, out string? reason);

                if (!Context.Guild.CurrentUser.GetPermissions(channel).ManageWebhooks)
                {
                    await RespondAsync($"Bot requires channel permission {nameof(ChannelPermission.ManageWebhooks)} in <#{channel.Id}>", ephemeral: true);

                    return;
                }

                await DeferAsync();
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();
                    await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync();
                    var stopwatch = new Stopwatch();
                    AuditLogUpsertResult result;
                    try
                    {
                        await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                        {
                            cmd.Transaction = transaction.GetDbTransaction();

                            cmd.CommandText = """
                                              WITH
                                                  guild_upsert AS (
                                                      INSERT INTO "Guilds" ("Id")
                                                      VALUES (@guildId)
                                                      ON CONFLICT ("Id") DO NOTHING
                                                  ),
                                                  channel_upsert AS (
                                                      INSERT INTO "AuditLogChannels" ("Id", "GuildId", "Type", "WebhookUrl")
                                                      VALUES (@channelId, @guildId, @action, '')
                                                      ON CONFLICT ("Id") DO UPDATE SET "Type" = "AuditLogChannels"."Type" | EXCLUDED."Type"
                                                      WHERE ("AuditLogChannels"."Type" & @action) = 0
                                                      RETURNING
                                                          CASE
                                                              WHEN xmax = 0 THEN 2
                                                              WHEN "WebhookUrl" <> '' THEN 1
                                                              ELSE 0
                                                          END AS result_tag
                                                  )
                                              SELECT * FROM channel_upsert;
                                              """;
                            cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                            cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                            cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                            stopwatch.Start();
                            object? rawValue = await cmd.ExecuteScalarAsync();
                            stopwatch.Stop();
                            Debug.Assert(rawValue is not null);

                            result = (AuditLogUpsertResult)(int)rawValue;
                        }

                        if (result is AuditLogUpsertResult.InsertedAndNeedsWebhookUrl)
                        {
                            RestWebhook webhook = (await channel.GetWebhooksAsync()).FirstOrDefault(w => w.Creator.Id == Context.Guild.CurrentUser.Id)
                                               ?? await channel.CreateWebhookAsync(
                                                      Context.Guild.CurrentUser.DisplayName,
                                                      options: DiscordUtils.GetAuditReasonRequestOptions(Context, "Automated webhook creation")
                                                  );

                            string webhookUrl = $"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}";
                            await db.AuditLogChannels.Where(auditLogChannel => auditLogChannel.Id == channel.Id)
                                    .ExecuteUpdateAsync(builder => builder.SetProperty(c => c.WebhookUrl, webhookUrl));
                        }

                        await transaction.CommitAsync();
                    }
                    finally
                    {
                        stopwatch.Stop();
                        Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                    }

                    if (result != AuditLogUpsertResult.NoChange)
                    {
                        if (result == AuditLogUpsertResult.Updated)
                        {
                            await LogUpdateAsync(db, Context, reason, channel.Id);
                        }
                        else
                        {
                            await LogCreateAsync(db, Context, reason, channel.Id);
                        }

                        return;
                    }
                }

                await FollowupAsync(AlreadyConfiguredMessage);
            }

            [SlashCmd("Remove a bot log channel configuration")]
            public async Task RemoveBotLog([ComplexParameter] LogChannelOptions<BotLogType> options)
            {
                options.Deconstruct(out SocketTextChannel channel, out BotLogType action, out string? reason);

                await DeferAsync();
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();

                    object? scalarResult;
                    Stopwatch stopwatch;
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = """
                                          WITH
                                              deleted AS (
                                                  DELETE
                                                  FROM "BotLogChannels"
                                                  WHERE
                                                      "Id" = @channelId
                                                      AND "GuildId" = @guildId
                                                      AND "Type" = @action
                                                  RETURNING 1
                                              ),
                                              updated AS (
                                                  UPDATE "BotLogChannels"
                                                  SET "Type" = "Type" & ~@action
                                                  WHERE
                                                      NOT EXISTS (SELECT 1 FROM deleted)
                                                      AND "Id" = @channelId
                                                      AND "GuildId" = @guildId
                                                      AND "Type" & @action <> 0
                                                  RETURNING 1
                                              )
                                          SELECT EXISTS (
                                              SELECT 1 FROM deleted
                                              UNION ALL
                                              SELECT 1 FROM updated
                                          );
                                          """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                        stopwatch = Stopwatch.StartNew();
                        scalarResult = await cmd.ExecuteScalarAsync();
                        stopwatch.Stop();
                    }

                    Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                    if (scalarResult is true)
                    {
                        await LogDeleteAsync(db, Context, reason, channel.Id);
                        return;
                    }
                }

                await FollowupAsync(NotConfiguredMessage);
            }

            [SlashCmd("Remove an audit log channel configuration")]
            public async Task RemoveAuditLog([ComplexParameter] LogChannelOptions<AuditLogType> options)
            {
                options.Deconstruct(out SocketTextChannel channel, out AuditLogType action, out string? reason);

                await DeferAsync();
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();

                    object? scalarResult;
                    Stopwatch stopwatch;
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = """
                                          WITH
                                              deleted AS (
                                                  DELETE
                                                  FROM "BotLogChannels"
                                                  WHERE
                                                      "Id" = @channelId
                                                      AND "GuildId" = @guildId
                                                      AND "Type" = @action
                                                  RETURNING 1
                                              ),
                                              updated AS (
                                                  UPDATE "BotLogChannels"
                                                  SET "Type" = "Type" & ~@action
                                                  WHERE
                                                      NOT EXISTS (SELECT 1 FROM deleted)
                                                      AND "Id" = @channelId
                                                      AND "GuildId" = @guildId
                                                      AND "Type" & @action <> 0
                                                  RETURNING 1
                                              )
                                          SELECT EXISTS (
                                              SELECT 1 FROM deleted
                                              UNION ALL
                                              SELECT 1 FROM updated
                                          );
                                          """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("channelId", channel.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("action", action));

                        stopwatch = Stopwatch.StartNew();
                        scalarResult = await cmd.ExecuteScalarAsync();
                        stopwatch.Stop();
                    }

                    Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                    if (scalarResult is true)
                    {
                        await LogDeleteAsync(db, Context, reason, channel.Id);
                        return;
                    }
                }

                await FollowupAsync(NotConfiguredMessage);
            }

            [SlashCmd("View the bot log channel configuration")]
            public async Task ViewBotLog()
            {
                await DeferAsync();

                Embed[] embeds = [];

                Stopwatch stopwatch;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = $"""
                                           WITH flag_values AS (
                                               SELECT * FROM (VALUES
                                                   (1,  '{nameof(BotLogType.Warn)}'),
                                                   (2,  '{nameof(BotLogType.Ban)}'),
                                                   (4,  '{nameof(BotLogType.Softban)}'),
                                                   (8,  '{nameof(BotLogType.Timeout)}'),
                                                   (16, '{nameof(BotLogType.Configuration)}'),
                                                   (32, '{nameof(BotLogType.Kick)}'),
                                                   (64, '{nameof(BotLogType.Deafen)}'),
                                                   (128,'{nameof(BotLogType.Mute)}'),
                                                   (256,'{nameof(BotLogType.Nick)}'),
                                                   (512,'{nameof(BotLogType.Purge)}'),
                                                   (1024,'{nameof(BotLogType.ModNote)}')
                                               ) AS flags(value, name)
                                           ),
                                           flattened AS (
                                               SELECT 
                                                   c."Id",
                                                   flags.value AS flag_value,
                                                   flags.name AS flag_name,
                                                   row_number() OVER (ORDER BY c."Id") AS entry_rn
                                               FROM "BotLogChannels" c
                                               JOIN flag_values flags ON (c."Type" & flags.value) = flags.value
                                               WHERE c."GuildId" = @guildId
                                           ),
                                           paged AS (
                                               SELECT 
                                                   '<#' || f."Id" || '> **[' || f.flag_name || ']**' AS entry,
                                                   (f.entry_rn - 1) / @batchSize AS batch_index,
                                                   f.entry_rn
                                               FROM flattened f
                                               ORDER BY f.entry_rn
                                               LIMIT 1000
                                           )
                                           SELECT 
                                               string_agg(p.entry, E'\n') AS "Batch",
                                               count(*) OVER () AS "TotalPages"
                                           FROM paged p
                                           GROUP BY p.batch_index
                                           ORDER BY MIN(p.entry_rn)
                                           LIMIT 100;
                                           """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromInt32("batchSize", ButtonPaginationManager.ChunkSize));

                        int page = 0;
                        var embedBuilder = new EmbedBuilder
                        {
                            Title = $"{Context.Guild.Name} Bot Log Channels",
                            Color = Storage.LightGold
                        };
                        stopwatch = Stopwatch.StartNew();
                        await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection))
                        {
                            if (await reader.ReadAsync())
                            {
                                string batch = reader.GetString(0);
                                int totalPages = reader.GetInt32(1);

                                embeds = new Embed[totalPages];

                                embedBuilder.Description = batch;
                                if (totalPages > 1)
                                {
                                    embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page 1/{totalPages}" };
                                }

                                embeds[page++] = embedBuilder.Build();

                                while (await reader.ReadAsync())
                                {
                                    batch = reader.GetString(0);

                                    embedBuilder.Description = batch;
                                    embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page {page + 1}/{totalPages}" };
                                    embeds[page++] = embedBuilder.Build();
                                }
                            }

                            stopwatch.Stop();
                        }
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                switch (embeds.Length)
                {
                    case 0:
                        await FollowupAsync(NothingToView, ephemeral: true);
                        return;
                    case 1:
                        await FollowupAsync(embeds: embeds);
                        return;
                    default:
                        await new ButtonPaginationManager(_loggerFactory, Context) { Embeds = embeds }.InitAsync(Context);
                        return;
                }
            }

            [SlashCmd("View the audit log channel configuration")]
            public async Task ViewAuditLog()
            {
                await DeferAsync();

                Embed[] embeds = [];

                Stopwatch stopwatch;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = $"""
                                           WITH flag_values AS (
                                               SELECT * FROM (VALUES
                                                   (1,   '{nameof(AuditLogType.Server)}'),
                                                   (2,   '{nameof(AuditLogType.Member)}'),
                                                   (4,   '{nameof(AuditLogType.Message)}'),
                                                   (8,   '{nameof(AuditLogType.Emoji)}'),
                                                   (16,  '{nameof(AuditLogType.Sticker)}'),
                                                   (32,  '{nameof(AuditLogType.Channel)}'),
                                                   (64,  '{nameof(AuditLogType.Thread)}'),
                                                   (128, '{nameof(AuditLogType.Role)}')
                                               ) AS flags(value, name)
                                           ),
                                           flattened AS (
                                               SELECT 
                                                   c."Id",
                                                   flags.value AS flag_value,
                                                   flags.name AS flag_name,
                                                   row_number() OVER (ORDER BY c."Id") AS entry_rn
                                               FROM "AuditLogChannels" c
                                               JOIN flag_values flags ON (c."Type" & flags.value) = flags.value
                                               WHERE c."GuildId" = @guildId
                                           ),
                                           paged AS (
                                               SELECT 
                                                   '<#' || f."Id" || '> **[' || f.flag_name || ']**' AS entry,
                                                   (f.entry_rn - 1) / @batchSize AS batch_index,
                                                   f.entry_rn
                                               FROM flattened f
                                               ORDER BY f.entry_rn
                                               LIMIT 1000
                                           )
                                           SELECT 
                                               string_agg(p.entry, E'\n') AS "Batch",
                                               count(*) OVER () AS "TotalPages"
                                           FROM paged p
                                           GROUP BY p.batch_index
                                           ORDER BY MIN(p.entry_rn)
                                           LIMIT 100;
                                           """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromInt32("batchSize", ButtonPaginationManager.ChunkSize));

                        int page = 0;
                        var embedBuilder = new EmbedBuilder
                        {
                            Title = $"{Context.Guild.Name} Audit Log Channels",
                            Color = Storage.LightGold
                        };
                        stopwatch = Stopwatch.StartNew();
                        await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection))
                        {
                            if (await reader.ReadAsync())
                            {
                                string batch = reader.GetString(0);
                                int totalPages = reader.GetInt32(1);

                                embeds = new Embed[totalPages];

                                embedBuilder.Description = batch;
                                if (totalPages > 1)
                                {
                                    embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page 1/{totalPages}" };
                                }

                                embeds[page++] = embedBuilder.Build();

                                while (await reader.ReadAsync())
                                {
                                    batch = reader.GetString(0);

                                    embedBuilder.Description = batch;
                                    embedBuilder.Footer = new EmbedFooterBuilder { Text = $"Page {page + 1}/{totalPages}" };
                                    embeds[page++] = embedBuilder.Build();
                                }
                            }

                            stopwatch.Stop();
                        }
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                switch (embeds.Length)
                {
                    case 0:
                        await FollowupAsync(NothingToView, ephemeral: true);
                        return;
                    case 1:
                        await FollowupAsync(embeds: embeds);
                        return;
                    default:
                        await new ButtonPaginationManager(_loggerFactory, Context) { Embeds = embeds }.InitAsync(Context);
                        return;
                }
            }

            public readonly record struct LogChannelOptions<T>(SocketTextChannel Channel, T Action, [ReasonMaxLength] string? Reason = null)
                where T : struct, Enum;
        }
    }
}
