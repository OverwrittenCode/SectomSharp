using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("warn", "Warning configuration")]
        public sealed class WarnModule : DisableableModule<WarnModule>, IDisableableModule<WarnModule>
        {
            private const uint MinThreshold = 1;
            private const uint MaxThreshold = 20;

            /// <inheritdoc />
            public static string DisableColumnName => "Configuration_Warning_IsDisabled";

            /// <inheritdoc />
            public WarnModule(ILogger<WarnModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task AddPunishment(uint threshold, TimeSpan? duration, string? reason, BotLogType punishment)
            {
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
                                              INSERT INTO "WarningThresholds" ("GuildId", "Value", "LogType", "Span")
                                              VALUES (@guildId, @threshold, @logType, @duration)
                                              ON CONFLICT DO NOTHING
                                              RETURNING 1
                                          )
                                      SELECT 1 FROM inserted;
                                      """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromNonNegativeInt32("threshold", threshold));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("logType", punishment));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromTimeSpan("duration", duration));

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

            [SlashCmd("Add a timeout punishment on reaching a number of warnings")]
            public Task AddTimeoutPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] uint threshold,
                [Summary(description: TimespanDescription)] [TimeoutRange] TimeSpan duration,
                [ReasonMaxLength] string? reason = null
            )
                => AddPunishment(threshold, duration, reason, BotLogType.Timeout);

            [SlashCmd("Add a ban punishment on reaching a number of warnings")]
            public Task AddBanPunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] uint threshold, [ReasonMaxLength] string? reason = null)
                => AddPunishment(threshold, null, reason, BotLogType.Ban);

            [SlashCmd("Remove a current punishment configuration")]
            public async Task RemovePunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] uint threshold, [ReasonMaxLength] string? reason = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                int affectedRows = await db.WarningThresholds.Where(warningThreshold => warningThreshold.GuildId == Context.Guild.Id && warningThreshold.Value == threshold)
                                           .ExecuteDeleteAsync();

                if (affectedRows == 0)
                {
                    await FollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogDeleteAsync(db, Context, reason);
            }

            [SlashCmd("View the configured warning thresholds")]
            public async Task ViewThresholds()
            {
                await DeferAsync();

                string? thresholdsText = null;
                bool isDisabled = false;

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
                                               ) AS f(value, name)
                                           )
                                           SELECT
                                               g."Configuration_Warning_IsDisabled" AS "IsDisabled",
                                               string_agg(
                                                   '- ' || t."Value" ||
                                                   CASE
                                                       WHEN t."Value" % 100 BETWEEN 11 AND 13 THEN 'th'
                                                       WHEN t."Value" % 10 = 1 THEN 'st'
                                                       WHEN t."Value" % 10 = 2 THEN 'nd'
                                                       WHEN t."Value" % 10 = 3 THEN 'rd'
                                                       ELSE 'th'
                                                   END ||
                                                   ' Strike: **' ||
                                                   COALESCE(
                                                       CASE
                                                           WHEN t."Span" IS NULL THEN NULL
                                                           WHEN date_part('day', t."Span") > 0 THEN date_part('day', t."Span")::int || ' day'
                                                           WHEN date_part('hour', t."Span") > 0 THEN date_part('hour', t."Span")::int || ' hour'
                                                           WHEN date_part('minute', t."Span") > 0 THEN date_part('minute', t."Span")::int || ' minute'
                                                           ELSE date_part('second', t."Span")::int || ' second'
                                                       END || ' ',
                                                       ''
                                                   ) ||
                                                   COALESCE(f.name, t."LogType"::text) ||
                                                   '**',
                                                   E'\n'
                                               ORDER BY t."Value") AS "Thresholds"
                                           FROM "Guilds" g
                                           LEFT JOIN "WarningThresholds" t ON t."GuildId" = g."Id"
                                           LEFT JOIN flag_values f ON f.value = t."LogType"
                                           WHERE g."Id" = @guildId
                                           GROUP BY g."Configuration_Warning_IsDisabled";
                                           """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));

                        stopwatch = Stopwatch.StartNew();
                        await using (DbDataReader reader =
                                     await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.CloseConnection))
                        {
                            if (await reader.ReadAsync())
                            {
                                isDisabled = reader.GetBoolean(0);
                                thresholdsText = reader.IsDBNull(1) ? null : reader.GetString(1);
                            }

                            stopwatch.Stop();
                        }
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                if (String.IsNullOrWhiteSpace(thresholdsText))
                {
                    await FollowupAsync(NothingToView, ephemeral: true);
                    return;
                }

                var embedBuilder = new EmbedBuilder
                {
                    Title = $"{Context.Guild.Name} Warning Thresholds",
                    Color = Storage.LightGold,
                    Description = thresholdsText
                };

                if (isDisabled)
                {
                    embedBuilder.Footer = new EmbedFooterBuilder { Text = "Module is currently disabled" };
                }

                await FollowupAsync(embeds: [embedBuilder.Build()]);
            }
        }
    }
}
