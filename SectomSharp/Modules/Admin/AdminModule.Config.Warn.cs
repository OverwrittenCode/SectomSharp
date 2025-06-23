using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;

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
            public WarnModule(ILogger<WarnModule> logger) : base(logger) { }

            private async Task AddPunishment(int threshold, TimeSpan? duration, string? reason, BotLogType punishment)
            {
                var warningThreshold = new WarningThreshold
                {
                    LogType = punishment,
                    Value = threshold,
                    Span = duration
                };

                await DeferAsync();

                await using (var db = new ApplicationDbContext())
                {
                    Guild guild = await EnsureGuildAsync(db);

                    if (guild.Configuration.Warning.Thresholds.Exists(x => x.Value == threshold))
                    {
                        await RespondOrFollowupAsync(AlreadyConfiguredMessage);
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
                    await RespondOrFollowupAsync(NotConfiguredMessage);
                    return;
                }

                WarningThreshold? match = guild.Configuration.Warning.Thresholds.Find(x => x.Value == threshold && x.LogType == punishment);

                if (match is null)
                {
                    await RespondOrFollowupAsync(NotConfiguredMessage);
                    return;
                }

                guild.Configuration.Warning.Thresholds.Remove(match);
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

            [SlashCmd("View the configured warning thresholds")]
            public async Task ViewThresholds()
            {
                if (await TryGetConfigurationViewAsync() is not var (warningConfiguration, embedBuilder))
                {
                    return;
                }

                if (warningConfiguration.Thresholds.Count == 0)
                {
                    await RespondOrFollowupAsync(NothingToView);
                    return;
                }

                embedBuilder.WithTitle($"{Context.Guild.Name} Warning Thresholds")
                            .WithDescription(String.Join('\n', warningConfiguration.Thresholds.OrderBy(threshold => threshold.Value).Select(threshold => threshold.Display())));

                await RespondOrFollowupAsync(embeds: [embedBuilder.Build()]);
            }
        }
    }
}
