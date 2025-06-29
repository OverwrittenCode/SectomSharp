using System.Data.Common;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        [Group("warn", "Warning configuration")]
        public sealed class WarnModule : DisableableModule<WarnModule, WarningConfiguration>
        {
            private const int MinThreshold = 1;
            private const int MaxThreshold = 20;

            /// <inheritdoc />
            public WarnModule(ILogger<WarnModule> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : base(logger, dbContextFactory) { }

            private async Task AddPunishment(int threshold, TimeSpan? duration, string? reason, BotLogType punishment)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                await db.Database.OpenConnectionAsync();
                await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

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
                cmd.Parameters.Add(NpgsqlParameterFactory.FromInt32("threshold", threshold));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("logType", punishment));
                cmd.Parameters.Add(NpgsqlParameterFactory.FromTimeSpan("duration", duration));

                if (await cmd.ExecuteScalarAsync() is null)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("Add a timeout punishment on reaching a number of warnings")]
            public async Task AddTimeoutPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold,
                [Summary(description: TimespanDescription)] [TimeoutRange] TimeSpan duration,
                [ReasonMaxLength] string? reason = null
            )
                => await AddPunishment(threshold, duration, reason, BotLogType.Timeout);

            [SlashCmd("Add a ban punishment on reaching a number of warnings")]
            public async Task AddBanPunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold, [ReasonMaxLength] string? reason = null)
                => await AddPunishment(threshold, null, reason, BotLogType.Ban);

            [SlashCmd("Remove a current punishment configuration")]
            public async Task RemovePunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold, [ReasonMaxLength] string? reason = null)
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
                int affectedRows = await db.WarningThresholds.Where(warningThreshold => warningThreshold.GuildId == Context.Guild.Id && warningThreshold.Value == threshold)
                                           .ExecuteDeleteAsync();

                if (affectedRows == 0)
                {
                    await RespondOrFollowupAsync(AlreadyConfiguredMessage);
                    return;
                }

                await LogAsync(db, Context, reason);
            }

            [SlashCmd("View the configured warning thresholds")]
            public async Task ViewThresholds()
            {
                await DeferAsync();
                await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();

                var result = await db.Guilds.Where(guild => guild.Id == Context.Guild.Id)
                                     .Select(guild => new
                                          {
                                              guild.Configuration.Warning.IsDisabled,
                                              Thresholds = guild.WarningThresholds.OrderBy(threshold => threshold.Value)
                                             .Select(threshold => new
                                                  {
                                                      threshold.Value,
                                                      threshold.LogType,
                                                      threshold.Span
                                                  }
                                              )
                                          }
                                      )
                                     .FirstOrDefaultAsync();

                if (result?.Thresholds.Any() != true)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                EmbedBuilder embedBuilder = GetConfigurationEmbedBuilder(result.IsDisabled);

                embedBuilder.WithTitle($"{Context.Guild.Name} Warning Thresholds")
                            .WithDescription(
                                 String.Join(
                                     '\n',
                                     result.Thresholds.Select(threshold =>
                                         {
                                             string ordinalSuffix = threshold.Value % 100 is >= 11 and <= 13
                                                 ? "th"
                                                 : (threshold.Value % 10) switch
                                                 {
                                                     1 => "st",
                                                     2 => "nd",
                                                     3 => "rd",
                                                     _ => "th"
                                                 };

                                             string strikePosition = threshold.Value + ordinalSuffix;

                                             string durationText = threshold.Span.HasValue
                                                 ? threshold.Span.Value switch
                                                 {
                                                     { Days: var d and > 0 } => $"{d} day",
                                                     { Hours: var h and > 0 } => $"{h} hour",
                                                     { Minutes: var m and > 0 } => $"{m} minute",
                                                     _ => $"{threshold.Span.Value.Seconds} second"
                                                 }
                                                 : "";

                                             return $"- {strikePosition} Strike: {Format.Bold($"{durationText} {threshold.LogType}")}";
                                         }
                                     )
                                 )
                             );

                await RespondOrFollowupAsync(embeds: [embedBuilder.Build()]);
            }
        }
    }
}
