using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Hand out an infraction to a user on the server.")]
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    public async Task Warn([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        await DeferAsync();
        await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
        await db.Database.OpenConnectionAsync();
        await CaseUtils.LogAsync(db, Context, BotLogType.Warn, OperationType.Create, user.Id, reason: reason);

        List<(BotLogType LogType, uint Value, TimeSpan? Span)> thresholds;
        int currentWarnings;
        Stopwatch stopwatch;
        await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
        {
            cmd.CommandText = """
                              WITH
                                  selected_guild AS (
                                      SELECT "Id", "Configuration_Warning_IsDisabled"
                                      FROM "Guilds"
                                      WHERE "Id" = @guildId
                                  ),
                                  warning_count AS (
                                      SELECT
                                          CASE
                                              WHEN sg."Configuration_Warning_IsDisabled" = TRUE THEN 0
                                              ELSE (
                                                  SELECT COUNT(*)
                                                  FROM "Cases"
                                                  WHERE
                                                      "GuildId" = @guildId
                                                      AND "TargetId" = @targetId
                                                      AND "LogType" = @logType
                                                      AND "OperationType" = @operationType
                                              )
                                          END AS "CurrentWarningCount"
                                      FROM selected_guild sg
                                  ),
                                  warning_thresholds AS (
                                      SELECT
                                          wc."CurrentWarningCount",
                                          threshold."LogType", 
                                          threshold."Value", 
                                          threshold."Span"
                                      FROM "WarningThresholds" threshold
                                      JOIN selected_guild sg ON sg."Id" = threshold."GuildId"
                                      CROSS JOIN warning_count wc
                                      WHERE
                                          sg."Configuration_Warning_IsDisabled" = FALSE
                                          AND threshold."Value" <= wc."CurrentWarningCount"
                                      ORDER BY threshold."Value" DESC
                                      LIMIT 2
                                  )
                              SELECT * FROM warning_thresholds;
                              """;

            cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
            cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("targetId", user.Id));
            cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("logType", BotLogType.Warn));
            cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("operationType", OperationType.Create));

            thresholds = new List<(BotLogType LogType, uint Value, TimeSpan? Span)>(2);
            currentWarnings = 0;
            stopwatch = Stopwatch.StartNew();
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess))
            {
                while (await reader.ReadAsync())
                {
                    currentWarnings = reader.GetInt32(0);

                    var logType = (BotLogType)reader.GetInt32(1);
                    uint value = (uint)reader.GetInt32(2);

                    TimeSpan? span = null;
                    if (!reader.IsDBNull(3))
                    {
                        span = reader.GetFieldValue<TimeSpan>(3);
                    }

                    thresholds.Add((logType, value, span));
                }

                stopwatch.Stop();
            }
        }

        Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
        if (thresholds.Count == 0)
        {
            return;
        }

        (BotLogType LogType, uint Value, TimeSpan? Span) punishmentThreshold = thresholds.Count == 2 && thresholds[1].Value == currentWarnings ? thresholds[1] : thresholds[0];
        GuildPermissions botPermissions = Context.Guild.CurrentUser.GuildPermissions;
        switch (punishmentThreshold.LogType)
        {
            case BotLogType.Ban:
                {
                    if (!botPermissions.BanMembers)
                    {
                        await SendFailureMessageAsync(this, nameof(BotLogType.Ban), currentWarnings);
                        return;
                    }

                    if (punishmentThreshold.Span is not null)
                    {
                        throw new InvalidOperationException($"{nameof(BotLogType.Ban)} does not support a timespan.");
                    }

                    // It is better to log the case after the action,
                    // However in this case the action must be done first
                    // as the DM won't work if user is no longer in guild
                    await LogCaseAsync(db, this, punishmentThreshold, user, currentWarnings);
                    await user.BanAsync(options: DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
                }

                break;

            case BotLogType.Timeout:
                {
                    if (!botPermissions.ModerateMembers)
                    {
                        await SendFailureMessageAsync(this, nameof(BotLogType.Timeout), currentWarnings);
                        return;
                    }

                    if (!punishmentThreshold.Span.HasValue)
                    {
                        throw new InvalidOperationException($"{nameof(BotLogType.Timeout)} requires a timespan.");
                    }

                    await user.SetTimeOutAsync(punishmentThreshold.Span.Value);
                    await LogCaseAsync(db, this, punishmentThreshold, user, currentWarnings);
                }

                break;

            default:
                throw new InvalidEnumArgumentException(nameof(punishmentThreshold), (int)punishmentThreshold.LogType, typeof(BotLogType));
        }

        return;

        static async Task LogCaseAsync(
            ApplicationDbContext db,
            ModerationModule moderationModule,
            (BotLogType LogType, uint Value, TimeSpan? Span) punishmentThreshold,
            IGuildUser user,
            int currentWarnings
        )
            => await CaseUtils.LogAsync(
                db,
                moderationModule.Context,
                punishmentThreshold.LogType,
                OperationType.Create,
                user.Id,
                expiresAt: user.TimedOutUntil,
                reason: $"A configured threshold was matched for `{currentWarnings}` warnings."
            );

        static async Task SendFailureMessageAsync(ModerationModule moderationModule, string logTypeAction, int currentWarnings)
            => await moderationModule.FollowupAsync(
                $"Warning configuration is setup to {logTypeAction} a user on reaching `{currentWarnings}` warnings but I lack permission to do so. Please contact a server administrator to fix this."
            );
    }
}
