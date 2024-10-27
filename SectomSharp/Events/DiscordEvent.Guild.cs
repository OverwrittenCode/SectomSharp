using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
#pragma warning disable CA1822
    public async Task HandleGuildUpdatedAsync(SocketGuild oldGuild, SocketGuild newGuild)
#pragma warning restore CA1822
    {
        List<AuditLogEntry> entries =
        [
            new(
                "Name",
                GetChangeEntry(oldGuild.Name, newGuild.Name),
                oldGuild.Name != newGuild.Name
            ),
            new(
                "Region",
                GetChangeEntry(oldGuild.PreferredLocale, newGuild.PreferredLocale),
                oldGuild.PreferredLocale != newGuild.PreferredLocale
            ),
            new(
                "Verification Level",
                GetChangeEntry(oldGuild.VerificationLevel, newGuild.VerificationLevel),
                oldGuild.VerificationLevel != newGuild.VerificationLevel
            ),
            new(
                "Default Message Notifications",
                GetChangeEntry(
                    oldGuild.DefaultMessageNotifications,
                    newGuild.DefaultMessageNotifications
                ),
                oldGuild.DefaultMessageNotifications != newGuild.DefaultMessageNotifications
            ),
            new(
                "Afk Timeout (seconds)",
                GetChangeEntry(oldGuild.AFKTimeout, newGuild.AFKTimeout),
                oldGuild.AFKTimeout != newGuild.AFKTimeout
            ),
            new(
                "Icon",
                GetChangeEntry(oldGuild.IconUrl, newGuild.IconUrl),
                oldGuild.IconUrl != newGuild.IconUrl
            ),
            new(
                "Banner",
                GetChangeEntry(oldGuild.BannerUrl, newGuild.BannerUrl),
                oldGuild.BannerUrl != newGuild.BannerUrl
            ),
            new(
                "Splash",
                GetChangeEntry(oldGuild.SplashUrl, newGuild.SplashUrl),
                oldGuild.SplashUrl != newGuild.SplashUrl
            ),
            new(
                "Afk Channel",
                GetChangeEntry(oldGuild.AFKChannel.Mention, newGuild.AFKChannel.Mention),
                oldGuild.AFKChannel.Id != newGuild.AFKChannel.Id
            ),
            new(
                "System Channel",
                GetChangeEntry(oldGuild.SystemChannel.Mention, newGuild.SystemChannel.Mention),
                oldGuild.SystemChannel.Id != newGuild.SystemChannel.Id
            ),
            new(
                "Owner",
                GetChangeEntry(oldGuild.Owner.Mention, newGuild.Owner.Mention),
                oldGuild.Owner.Id != newGuild.Owner.Id
            ),
            new(
                "Explicit Content Filter Level",
                GetChangeEntry(oldGuild.ExplicitContentFilter, newGuild.ExplicitContentFilter),
                oldGuild.ExplicitContentFilter != newGuild.ExplicitContentFilter
            ),
            new(
                "Preferred Local",
                GetChangeEntry(oldGuild.PreferredLocale, newGuild.PreferredLocale),
                oldGuild.PreferredLocale != newGuild.PreferredLocale
            ),
            new(
                "Preferred Culture",
                GetChangeEntry(
                    oldGuild.PreferredCulture.NativeName,
                    newGuild.PreferredCulture.NativeName
                ),
                !oldGuild.PreferredCulture.Equals(newGuild.PreferredCulture)
            ),
            new(
                "Enable Boost Progress Bar",
                $"Set to {newGuild.IsBoostProgressBarEnabled}",
                oldGuild.IsBoostProgressBarEnabled != newGuild.IsBoostProgressBarEnabled
            ),
            new(
                "Safety Alerts Channel Id",
                GetChangeEntry(
                    MentionUtils.MentionChannel(oldGuild.SafetyAlertsChannel.Id),
                    MentionUtils.MentionChannel(newGuild.SafetyAlertsChannel.Id)
                ),
                oldGuild.SafetyAlertsChannel != newGuild.SafetyAlertsChannel
            ),
        ];

        await LogAsync(
            newGuild,
            AuditLogType.Server,
            OperationType.Update,
            entries,
            newGuild.Name,
            newGuild.Name,
            newGuild.IconUrl
        );
    }
}
