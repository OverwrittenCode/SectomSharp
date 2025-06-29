using System.Collections.Immutable;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    public async Task HandleGuildUpdatedAsync(SocketGuild oldGuild, SocketGuild newGuild)
    {
        await using ApplicationDbContext db = await _dbFactory.CreateDbContextAsync();

        if (await db.AuditLogChannels.Where(channel => channel.GuildId == newGuild.Id && ((int)channel.Type & (int)(AuditLogType.Emoji | AuditLogType.Server)) != 0)
                    .Select(channel => new
                         {
                             channel.Type,
                             channel.WebhookUrl
                         }
                     )
                    .Take(2)
                    .ToArrayAsync() is not { } auditLogChannels)
        {
            return;
        }

        if (auditLogChannels.FirstOrDefault(channel => channel.Type.HasFlag(AuditLogType.Emoji))?.WebhookUrl is { } emojiChannelWebhookUrl)
        {
            await HandleGuildEmoteAsync(
                newGuild,
                new DiscordWebhookClient(emojiChannelWebhookUrl),
                (ImmutableArray<GuildEmote>)oldGuild.Emotes,
                (ImmutableArray<GuildEmote>)newGuild.Emotes
            );
        }

        if (auditLogChannels.FirstOrDefault(channel => channel.Type.HasFlag(AuditLogType.Server))?.WebhookUrl is not { } serverChannelWebhookUrl)
        {
            return;
        }

        var serverDiscordWebhookClient = new DiscordWebhookClient(serverChannelWebhookUrl);

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldGuild.Name, newGuild.Name), oldGuild.Name != newGuild.Name),
            new("Region", GetChangeEntry(oldGuild.PreferredLocale, newGuild.PreferredLocale), oldGuild.PreferredLocale != newGuild.PreferredLocale),
            new("Verification Level", GetChangeEntry(oldGuild.VerificationLevel, newGuild.VerificationLevel), oldGuild.VerificationLevel != newGuild.VerificationLevel),
            new(
                "Default Message Notifications",
                GetChangeEntry(oldGuild.DefaultMessageNotifications, newGuild.DefaultMessageNotifications),
                oldGuild.DefaultMessageNotifications != newGuild.DefaultMessageNotifications
            ),
            new("Afk Timeout (seconds)", GetChangeEntry(oldGuild.AFKTimeout, newGuild.AFKTimeout), oldGuild.AFKTimeout != newGuild.AFKTimeout),
            new("Icon", GetChangeEntry(oldGuild.IconUrl, newGuild.IconUrl), oldGuild.IconUrl != newGuild.IconUrl),
            new("Banner", GetChangeEntry(oldGuild.BannerUrl, newGuild.BannerUrl), oldGuild.BannerUrl != newGuild.BannerUrl),
            new("Splash", GetChangeEntry(oldGuild.SplashUrl, newGuild.SplashUrl), oldGuild.SplashUrl != newGuild.SplashUrl),
            new("Afk Channel", GetChangeEntry(oldGuild.AFKChannel?.Mention, newGuild.AFKChannel?.Mention), oldGuild.AFKChannel?.Id != newGuild.AFKChannel?.Id),
            new("System Channel", GetChangeEntry(oldGuild.SystemChannel?.Mention, newGuild.SystemChannel?.Mention), oldGuild.SystemChannel?.Id != newGuild.SystemChannel?.Id),
            new("Owner", GetChangeEntry(oldGuild.Owner?.Mention, newGuild.Owner?.Mention), oldGuild.Owner?.Id != newGuild.Owner?.Id),
            new(
                "Explicit Content Filter Level",
                GetChangeEntry(oldGuild.ExplicitContentFilter, newGuild.ExplicitContentFilter),
                oldGuild.ExplicitContentFilter != newGuild.ExplicitContentFilter
            ),
            new("Preferred Local", GetChangeEntry(oldGuild.PreferredLocale, newGuild.PreferredLocale), oldGuild.PreferredLocale != newGuild.PreferredLocale),
            new(
                "Preferred Culture",
                GetChangeEntry(oldGuild.PreferredCulture?.NativeName, newGuild.PreferredCulture?.NativeName),
                oldGuild.PreferredCulture?.Equals(newGuild.PreferredCulture) == false
            ),
            new("Enable Boost Progress Bar", $"Set to {newGuild.IsBoostProgressBarEnabled}", oldGuild.IsBoostProgressBarEnabled != newGuild.IsBoostProgressBarEnabled),
            new(
                "Safety Alerts Channel Id",
                GetChangeEntry(
                    oldGuild.SafetyAlertsChannel?.Id is { } oldGuildSafetyAlertsChannelId ? MentionUtils.MentionChannel(oldGuildSafetyAlertsChannelId) : null,
                    newGuild.SafetyAlertsChannel?.Id is { } newGuildSafetyAlertsChannelId ? MentionUtils.MentionChannel(newGuildSafetyAlertsChannelId) : null
                ),
                oldGuild.SafetyAlertsChannel != newGuild.SafetyAlertsChannel
            )
        ];

        await LogAsync(newGuild, serverDiscordWebhookClient, AuditLogType.Server, OperationType.Update, entries, newGuild.Name, newGuild.Name, newGuild.IconUrl);
    }
}
