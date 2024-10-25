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
            guildEntity.Configuration is
            {
                Warning: not null
                    and { IsDisabled: false, Thresholds: var thresholds and { Count: > 0 } }
            }
        )
        {
            var count = guildEntity.Cases.Count(@case =>
                @case.GuildId == guildEntity.Id
                && @case.TargetId == user.Id
                && @case is { LogType: BotLogType.Warn, OperationType: OperationType.Create }
            );

            WarningThreshold[] orderedThresholds = thresholds
                .Where(threshold => count <= threshold.Value)
                .OrderByDescending(threshold => threshold.Value)
                .Take(2)
                .ToArray();

            if (orderedThresholds.Length == 0)
            {
                return;
            }

            WarningThreshold punishmentThreshold =
                orderedThresholds.Length == 2 && orderedThresholds[1].Value == count
                    ? orderedThresholds[1]
                    : orderedThresholds[0];

            var warningDisplayText = $"{Format.Code(count.ToString())} warnings";

            async Task SendFailureMessageAsync() =>
                await RespondOrFollowUpAsync(
                    $"Warning configuration is setup to {punishmentThreshold.LogType} a user on reaching {warningDisplayText} but I lack permission to do so. Please contact a server administrator to fix this."
                );

            async Task LogCaseAsync() =>
                await CaseService.LogAsync(
                    Context,
                    punishmentThreshold.LogType,
                    OperationType.Create,
                    targetId: user.Id,
                    expiresAt: user.TimedOutUntil?.Date,
                    reason: $"A configured threshold was matched for {warningDisplayText}."
                );

            GuildPermissions botPermissions = Context.Guild.CurrentUser.GuildPermissions;

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

                        if (punishmentThreshold.Span is not { } span)
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
