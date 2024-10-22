using Discord;
using Discord.Interactions;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Services;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    [SlashCommand("warn", "Hand out an infraction to a user on the server.")]
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    public async Task Warn(
        [DoHierarchyCheck] IGuildUser user,
        [MaxLength(CaseService.MaxReasonLength)] string? reason = null
    )
    {
        await DeferAsync();
        Guild guildEntity = await CaseService.LogAsync(
            Context,
            BotLogType.Warn,
            OperationType.Create,
            targetId: user.Id,
            reason: reason,
            includeGuildCases: true
        );

        if (
            guildEntity.Configuration is Configuration
            {
                Warning: WarningConfiguration
                    and {
                        IsDisabled: false,
                        GeometricDurationMultiplier: var multiplier,
                        Thresholds: var thresholds and { Count: > 0 }
                    }
            }
        )
        {
            var count = guildEntity
                .Cases.Where(@case =>
                    /// Include GuildId to fully utilise the index in <see cref="Data.Configurations.CaseConfiguration"/>.
                    @case.GuildId == guildEntity.Id
                    && @case.TargetId == user.Id
                    && @case.LogType == BotLogType.Warn
                    && @case.OperationType == OperationType.Create
                )
                .Count();

            var orderedThresholds = thresholds
                .Where(threshold => count <= threshold.Value)
                .OrderByDescending(threshold => threshold.Value)
                .Take(2)
                .ToArray();

            if (orderedThresholds.Length == 0)
            {
                return;
            }

            var punishmentThreshold =
                orderedThresholds.Length == 2 && orderedThresholds[1].Value == count
                    ? orderedThresholds[1]
                    : orderedThresholds[0];

            var repeatedOffences =
                orderedThresholds.Length == 2 && orderedThresholds[0].Value != count
                    ? count - orderedThresholds[1].Value
                    : 0;

            TimeSpan? duration = null;

            if (orderedThresholds[0].Span is TimeSpan baseSpan)
            {
                duration = baseSpan * Math.Pow(multiplier, repeatedOffences);
            }

            var autoReason = $"A configured threshold was matched for [{count}] warnings.";

            var botPermissions = Context.Guild.CurrentUser.GuildPermissions;

            async Task SendFailureMessageAsync() =>
                await RespondOrFollowUpAsync(
                    $"Warning configuration is setup to {punishmentThreshold.LogType} a user on reaching [{count}] warnings but I lack permission to do so. Please contact a server administrator to fix this."
                );

            async Task LogCaseAsync() =>
                await CaseService.LogAsync(
                    Context,
                    punishmentThreshold.LogType,
                    OperationType.Create,
                    perpetratorId: Context.Client.CurrentUser.Id,
                    targetId: user.Id,
                    expiresAt: punishmentThreshold.Span is TimeSpan span
                        ? DateTime.Now.Add(span)
                        : null,
                    reason: autoReason
                );

            switch (punishmentThreshold.LogType)
            {
                case BotLogType.Ban:
                    {
                        if (!botPermissions.BanMembers)
                        {
                            await SendFailureMessageAsync();
                            return;
                        }

                        if (punishmentThreshold.Span is not null)
                        {
                            throw new InvalidOperationException(
                                $"{punishmentThreshold.LogType} does not support a timespan."
                            );
                        }

                        // It is better to log the case after the action,
                        // However in this case the action must be done first
                        // as the DM won't work if user is no longer in guild
                        await LogCaseAsync();
                        await user.BanAsync(
                            options: DiscordUtils.GetAuditReasonRequestOptions(Context, reason)
                        );
                    }

                    break;

                case BotLogType.Timeout:
                    {
                        if (!botPermissions.ModerateMembers)
                        {
                            await SendFailureMessageAsync();
                            return;
                        }

                        if (punishmentThreshold.Span is not TimeSpan span)
                        {
                            throw new InvalidOperationException(
                                $"{punishmentThreshold.LogType} requires a timespan."
                            );
                        }

                        await user.SetTimeOutAsync(span);
                        await LogCaseAsync();
                    }

                    break;

                default:
                    throw new InvalidOperationException(
                        $"{punishmentThreshold.LogType} is not supported."
                    );
            }
        }
    }
}
