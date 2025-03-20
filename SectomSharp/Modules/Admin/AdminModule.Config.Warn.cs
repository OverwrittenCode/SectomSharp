using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
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

            private async Task AddPunishment(int threshold, TimeSpan? duration, string? reason, BotLogType punishment)
            {
                WarningThreshold warningThreshold = new()
                {
                    LogType = punishment,
                    Value = threshold,
                    Span = duration
                };

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);

                    if (guild is null)
                    {
                        await db.Guilds.AddAsync(
                            new Guild
                            {
                                Id = Context.Guild.Id,
                                Configuration = new Configuration
                                {
                                    Warning = new WarningConfiguration
                                    {
                                        Thresholds = [warningThreshold]
                                    }
                                }
                            }
                        );

                        await db.SaveChangesAsync();
                        await LogAsync(Context, reason);
                        return;
                    }

                    if ((guild.Configuration ??= new Configuration()).Warning.Thresholds.Exists(x => x.Value == threshold))
                    {
                        await RespondOrFollowUpAsync(AlreadyConfiguredMessage);
                        return;
                    }

                    guild.Configuration.Warning.Thresholds.Add(warningThreshold);
                    await db.SaveChangesAsync();
                }

                await LogAsync(Context, reason);
            }

            private async Task RemovePunishmentAsync(int threshold, string? reason, BotLogType punishment)
            {
                await DeferAsync();

                await using var db = new ApplicationDbContext();
                Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new Guild
                        {
                            Id = Context.Guild.Id
                        }
                    );
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NotConfiguredMessage);
                    return;
                }

                WarningThreshold? match = (guild.Configuration ??= new Configuration()).Warning.Thresholds.Find(x => x.Value == threshold && x.LogType == punishment);

                if (match is null)
                {
                    await RespondOrFollowUpAsync(NotConfiguredMessage);
                    return;
                }

                guild.Configuration.Warning.Thresholds.Remove(match);
                await db.SaveChangesAsync();

                await LogAsync(Context, reason);
            }

            private async Task SetIsDisabledAsync(bool isDisabled, string? reason)
            {
                await DeferAsync();

                Configuration disabledEntry = new()
                {
                    Warning = new WarningConfiguration
                    {
                        IsDisabled = isDisabled
                    }
                };

                await using var db = new ApplicationDbContext();

                Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);
                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new Guild
                        {
                            Id = Context.Guild.Id,
                            Configuration = disabledEntry
                        }
                    );

                    await db.SaveChangesAsync();
                    await LogAsync(Context, reason);
                    return;
                }

                if (guild.Configuration is not { } configuration)
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

            [SlashCmd("Remove a current timeout punishment configuration")]
            public async Task RemoveTimeoutPunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold, [ReasonMaxLength] string? reason = null)
                => await RemovePunishmentAsync(threshold, reason, BotLogType.Timeout);

            [SlashCmd("Remove a current ban punishment configuration")]
            public async Task RemoveBanPunishment([MinValue(MinThreshold)] [MaxValue(MaxThreshold)] int threshold, [ReasonMaxLength] string? reason = null)
                => await RemovePunishmentAsync(threshold, reason, BotLogType.Ban);

            [SlashCmd("Disable this configuration")]
            public async Task Disable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(true, reason);

            [SlashCmd("Enable this configuration")]
            public async Task Enable([ReasonMaxLength] string? reason = null) => await SetIsDisabledAsync(false, reason);

            [SlashCmd("View the configured warning thresholds")]
            public async Task ViewThresholds()
            {
                await DeferAsync();

                await using var db = new ApplicationDbContext();

                Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);

                if (guild is null)
                {
                    await db.Guilds.AddAsync(
                        new Guild
                        {
                            Id = Context.Guild.Id,
                            Configuration = new Configuration()
                        }
                    );
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                if (guild.Configuration is not { Warning: var warningConfiguration })
                {
                    guild.Configuration = new Configuration();
                    await db.SaveChangesAsync();
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                db.Entry(guild).State = EntityState.Detached;

                if (warningConfiguration.Thresholds.Count == 0)
                {
                    await RespondOrFollowUpAsync(NothingToView);
                    return;
                }

                IEnumerable<string> descriptionArray = warningConfiguration.Thresholds.OrderBy(threshold => threshold.Value).Select(threshold => threshold.Display());

                var embed = new EmbedBuilder
                {
                    Title = $"{Context.Guild.Name} Warning Thresholds",
                    Color = Constants.LightGold,
                    Description = String.Join("\n", descriptionArray)
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
