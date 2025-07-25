using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;
using MentionUtils = SectomSharp.Utils.MentionUtils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    public async Task HandleGuildUpdatedAsync(SocketGuild oldGuild, SocketGuild newGuild)
    {
        AuditLogGuildUpdatedResult[] auditLogChannels;
        await using (ApplicationDbContext db = await _dbFactory.CreateDbContextAsync())
        {
            auditLogChannels = await db.AuditLogChannels
                                       .Where(channel => channel.GuildId == newGuild.Id && ((int)channel.Type & (int)(AuditLogType.Emoji | AuditLogType.Server)) != 0)
                                       .Select(channel => new AuditLogGuildUpdatedResult(channel.Type, channel.WebhookUrl))
                                       .Take(2)
                                       .ToArrayAsync();
        }

        if (auditLogChannels.Length == 0)
        {
            return;
        }

        if (auditLogChannels.FirstOrDefault(channel => channel.Type.HasFlag(AuditLogType.Emoji))?.WebhookUrl is { } emojiChannelWebhookUrl)
        {
            using var webhookClient = new DiscordWebhookClient(emojiChannelWebhookUrl);
            await HandleGuildEmoteAsync(newGuild, webhookClient, [..oldGuild.Emotes], [..newGuild.Emotes]);
        }

        if (auditLogChannels.FirstOrDefault(channel => channel.Type.HasFlag(AuditLogType.Server))?.WebhookUrl is not { } serverChannelWebhookUrl)
        {
            return;
        }

        List<EmbedFieldBuilder> builders = new(16);
        AddIfChanged(builders, "Name", oldGuild.Name, newGuild.Name);
        AddIfChanged(builders, "Region", oldGuild.PreferredLocale, newGuild.PreferredLocale);
        AddIfChanged(builders, "Verification Level", oldGuild.VerificationLevel, newGuild.VerificationLevel);
        AddIfChanged(builders, "Default Message Notifications", oldGuild.DefaultMessageNotifications, newGuild.DefaultMessageNotifications);
        AddIfChanged(builders, "Afk Timeout (seconds)", oldGuild.AFKTimeout, newGuild.AFKTimeout);
        AddIfChanged(builders, "Icon", oldGuild.IconUrl, newGuild.IconUrl);
        AddIfChanged(builders, "Banner", oldGuild.BannerUrl, newGuild.BannerUrl);
        AddIfChanged(builders, "Splash", oldGuild.SplashUrl, newGuild.SplashUrl);
        if (oldGuild.AFKChannel?.Id != newGuild.AFKChannel?.Id)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Afk Channel", GetChangeEntry(oldGuild.AFKChannel?.Mention, newGuild.AFKChannel?.Mention)));
        }

        if (oldGuild.SystemChannel?.Id != newGuild.SystemChannel?.Id)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("System Channel", GetChangeEntry(oldGuild.SystemChannel?.Mention, newGuild.SystemChannel?.Mention)));
        }

        if (oldGuild.OwnerId != newGuild.OwnerId)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Owner", GetChangeEntry(oldGuild.Owner?.Mention, newGuild.Owner?.Mention)));
        }

        AddIfChanged(builders, "Explicit Content Filter Level", oldGuild.ExplicitContentFilter, newGuild.ExplicitContentFilter);
        AddIfChanged(builders, "Preferred Local", oldGuild.PreferredLocale, newGuild.PreferredLocale);
        if (oldGuild.PreferredCulture?.Equals(newGuild.PreferredCulture) == false)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Preferred Culture", GetChangeEntry(oldGuild.PreferredCulture?.NativeName, newGuild.PreferredCulture?.NativeName)));
        }

        AddIfChanged(builders, "Enable Boost Progress Bar", oldGuild.IsBoostProgressBarEnabled, newGuild.IsBoostProgressBarEnabled);
        if (oldGuild.SafetyAlertsChannel != newGuild.SafetyAlertsChannel)
        {
            builders.Add(
                EmbedFieldBuilderFactory.Create(
                    "Safety Alerts Channel",
                    GetChangeEntry(
                        oldGuild.SafetyAlertsChannel is { Id: var oldGuildSafetyAlertsChannelId } ? MentionUtils.MentionChannel(oldGuildSafetyAlertsChannelId) : null,
                        newGuild.SafetyAlertsChannel is { Id: var newGuildSafetyAlertsChannelId } ? MentionUtils.MentionChannel(newGuildSafetyAlertsChannelId) : null
                    )
                )
            );
        }

        if (builders.Count == 0)
        {
            return;
        }

        using var serverChannelWebhookClient = new DiscordWebhookClient(serverChannelWebhookUrl);
        await LogAsync(newGuild, serverChannelWebhookClient, AuditLogType.Server, OperationType.Update, builders, newGuild.Name, newGuild.Name, newGuild.IconUrl);
    }

    public sealed record AuditLogGuildUpdatedResult(AuditLogType Type, string WebhookUrl);
}
