using System.ComponentModel;
using Discord;
using Discord.Interactions;
using SectomSharp.Attributes;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [SlashCmd("Hand out an infraction to a user on the server.")]
    [DefaultMemberPermissions(GuildPermission.KickMembers)]
    public async Task Warn([DoHierarchyCheck] IGuildUser user, [ReasonMaxLength] string? reason = null)
    {
        await DeferAsync();
        Guild guild = await CaseUtils.LogAsync(Context, BotLogType.Warn, OperationType.Create, user.Id, reason: reason, includeGuildCases: true);

        if (guild.Configuration is { Warning: { IsDisabled: false, Thresholds: { Count: > 0 } thresholds } })
        {
            int count = guild.Cases.Count(@case
                => @case.GuildId == guild.Id && @case.TargetId == user.Id && @case is { LogType: BotLogType.Warn, OperationType: OperationType.Create }
            );

            WarningThreshold[] orderedThresholds = thresholds.Where(threshold => count <= threshold.Value).OrderByDescending(threshold => threshold.Value).Take(2).ToArray();

            if (orderedThresholds.Length == 0)
            {
                return;
            }

            WarningThreshold punishmentThreshold = orderedThresholds.Length == 2 && orderedThresholds[1].Value == count ? orderedThresholds[1] : orderedThresholds[0];

            string warningDisplayText = $"{Format.Code(count.ToString())} warnings";

            async Task SendFailureMessageAsync()
                => await RespondOrFollowupAsync(
                    $"Warning configuration is setup to {punishmentThreshold.LogType} a user on reaching {warningDisplayText} but I lack permission to do so. Please contact a server administrator to fix this."
                );

            async Task LogCaseAsync()
                => await CaseUtils.LogAsync(
                    Context,
                    punishmentThreshold.LogType,
                    OperationType.Create,
                    user.Id,
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
                            throw new InvalidOperationException($"{punishmentThreshold.LogType} does not support a timespan.");
                        }

                        // It is better to log the case after the action,
                        // However in this case the action must be done first
                        // as the DM won't work if user is no longer in guild
                        await LogCaseAsync();
                        await user.BanAsync(options: DiscordUtils.GetAuditReasonRequestOptions(Context, reason));
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
                            throw new InvalidOperationException($"{punishmentThreshold.LogType} requires a timespan.");
                        }

                        await user.SetTimeOutAsync(span);
                        await LogCaseAsync();
                    }

                    break;

                default:
                    throw new InvalidEnumArgumentException(nameof(punishmentThreshold), (int)punishmentThreshold.LogType, typeof(BotLogType));
            }
        }
    }
}
