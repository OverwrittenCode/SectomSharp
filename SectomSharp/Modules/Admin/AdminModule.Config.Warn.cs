using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Services;

namespace SectomSharp.Modules.Admin;

public partial class AdminModule
{
    public partial class ConfigModule
    {
        [Group("warn", "Warning configuration")]
        public sealed class WarnModule : BaseModule
        {
            private const int MinThreshold = 1;
            private const int MaxThreshold = 20;

            private async Task AddPunishment(
                int threshold,
                TimeSpan? duration,
                string? reason,
                BotLogType punishment
            )
            {
                WarningThreshold warningThreshold =
                    new()
                    {
                        LogType = punishment,
                        Value = threshold,
                        Span = duration,
                    };

                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db.Guilds.FindAsync(Context.Guild.Id);

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(
                            new()
                            {
                                Id = Context.Guild.Id,
                                Configuration = new()
                                {
                                    Warning = new() { Thresholds = [warningThreshold] },
                                },
                            }
                        );

                        await db.SaveChangesAsync();
                        await LogAsync(Context, reason);
                        return;
                    }

                    if (
                        (guild.Configuration ??= new()).Warning.Thresholds.Exists(x =>
                            x.Value == threshold
                        )
                    )
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    guild.Configuration.Warning.Thresholds.Add(warningThreshold);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            private async Task RemovePunishmentAsync(
                int threshold,
                string? reason,
                BotLogType punishment
            )
            {
                await DeferAsync();

                using (var db = new ApplicationDbContext())
                {
                    var guild = await db.Guilds.FindAsync(Context.Guild.Id);

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(new() { Id = Context.Guild.Id });
                        await db.SaveChangesAsync();
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    WarningThreshold? match = (
                        guild.Configuration ??= new()
                    ).Warning.Thresholds.Find(x => x.Value == threshold && x.LogType == punishment);

                    if (match is null)
                    {
                        await RespondOrFollowUpAsync(NotConfiguredMessage);
                        return;
                    }

                    guild.Configuration.Warning.Thresholds.Remove(match);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            [SlashCommand(
                "add-timeout-punishment",
                "Add a timeout punishment on reaching a number of warnings"
            )]
            public async Task AddTimeoutPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold,
                [Summary(description: TimespanDescription)] [TimeoutRange] TimeSpan duration,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await AddPunishment(threshold, duration, reason, BotLogType.Timeout);

            [SlashCommand(
                "add-ban-punishment",
                "Add a ban punishment on reaching a number of warnings"
            )]
            public async Task AddBanPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await AddPunishment(threshold, null, reason, BotLogType.Ban);

            [SlashCommand(
                "remove-timeout-punishment",
                "Remove a current timeout punishment configuration"
            )]
            public async Task RemoveTimeoutPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await RemovePunishmentAsync(threshold, reason, BotLogType.Timeout);

            [SlashCommand("remove-ban-punishment", "Remove a current ban punishment configuration")]
            public async Task RemoveBanPunishment(
                [MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold,
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await RemovePunishmentAsync(threshold, reason, BotLogType.Ban);
        }
    }
}
