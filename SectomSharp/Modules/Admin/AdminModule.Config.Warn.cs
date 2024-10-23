using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Services;
using SectomSharp.Utils;

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

            public async Task SetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();

                Configuration disabledEntry = new() { Warning = new() { IsDisabled = isDisabled } };

                using var db = new ApplicationDbContext();

                var guild = await db.Guilds.FindAsync(Context.Guild.Id);
                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new() { Id = Context.Guild.Id, Configuration = disabledEntry }
                    );

                    await db.SaveChangesAsync();
                    await LogAsync(Context, reason);
                    return;
                }

                if (guild.Configuration is not Configuration configuration)
                {
                    guild.Configuration = disabledEntry;

                    await db.SaveChangesAsync();
                    await LogAsync(Context, reason);
                    return;
                }

                if (configuration.Warning.IsDisabled == isDisabled)
                {
                    await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                    return;
                }

                configuration.Warning.IsDisabled = isDisabled;
                await db.SaveChangesAsync();
                await LogAsync(Context, reason);
            }

            [SlashCommand("disable", "Disable this configuration")]
            public async Task Disable(
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await SetIsDisabledAsync(true, reason);

            [SlashCommand("enable", "Enable this configuration")]
            public async Task Enable(
                [MaxLength(CaseService.MaxReasonLength)] string? reason = null
            ) => await SetIsDisabledAsync(false, reason);

            [SlashCommand("view-thresholds", "View the configured warning thresholds")]
            public async Task ViewThresholds()
            {
                async Task SendErrorAsync() => await RespondOrFollowUpAsync("Nothing to view yet.");

                await DeferAsync();

                using var db = new ApplicationDbContext();

                var guild = await db.Guilds.FindAsync(Context.Guild.Id);

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new() { Id = Context.Guild.Id, Configuration = new() }
                    );
                    await db.SaveChangesAsync();
                    await SendErrorAsync();
                    return;
                }

                if (guild.Configuration is not Configuration { Warning: var warningConfiguration })
                {
                    guild.Configuration = new();
                    await db.SaveChangesAsync();
                    await SendErrorAsync();
                    return;
                }

                db.Entry(guild).State = EntityState.Detached;

                if (warningConfiguration.Thresholds.Count == 0)
                {
                    await SendErrorAsync();
                    return;
                }

                var descriptionArray = warningConfiguration
                    .Thresholds.OrderBy(threshold => threshold.Value)
                    .Select(threshold =>
                    {
                        var ordinalSuffix =
                            (threshold.Value % 100 is >= 11 and <= 13)
                                ? "th"
                                : (threshold.Value % 10) switch
                                {
                                    1 => "st",
                                    2 => "nd",
                                    3 => "rd",
                                    _ => "th",
                                };

                        var strikePosition = threshold.Value + ordinalSuffix;

                        var durationText = threshold.Span is not TimeSpan timeSpan
                            ? ""
                            : timeSpan switch
                            {
                                { Days: var d and > 0 } => $"{d} day",
                                { Hours: var h and > 0 } => $"{h} hour",
                                { Minutes: var m and > 0 } => $"{m} minute",
                                _ => $"{timeSpan.Seconds} second",
                            };

                        return $"- {strikePosition} Strike: {Format.Bold($"{durationText} {threshold.LogType}")}";
                    });

                var embed = new EmbedBuilder()
                {
                    Title = $"{Context.Guild.Name} Warning Thresholds",
                    Color = Constants.LightGold,
                    Description = String.Join("\n", descriptionArray),
                };

                if (warningConfiguration.IsDisabled)
                {
                    embed.WithFooter("Module is currently disabled: /config warn enable");
                }

                await RespondOrFollowUpAsync(embeds: [embed.Build()]);
            }
        }
    }
}
