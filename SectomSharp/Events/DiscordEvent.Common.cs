using Discord;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static async Task LogAsync(
        IGuild guild,
        AuditLogType auditLogType,
        OperationType operationType,
        IEnumerable<AuditLogEntry> entries,
        string footerPrefix,
        string authorName,
        string? authorIconUrl = null,
        Color? colour = null
    )
    {
        DiscordWebhookClient webhookClient;

        await using (var db = new ApplicationDbContext())
        {
            ICollection<AuditLogChannel>? logChannels = await db
                .Guilds.Where(x => x.Id == guild.Id)
                .AsNoTracking()
                .Include(x => x.AuditLogChannels)
                .Select(x => x.AuditLogChannels)
                .FirstOrDefaultAsync();

            if (
                logChannels?.FirstOrDefault(channel => channel.AuditLogType.HasFlag(auditLogType))
                is not { } auditLogChannel
            )
            {
                return;
            }

            webhookClient = new(auditLogChannel.WebhookUrl);
        }

        try
        {
            EmbedBuilder embed = new EmbedBuilder()
                .WithAuthor(authorName, authorIconUrl)
                .WithColor(
                    colour
                        ?? operationType switch
                        {
                            OperationType.Create => Color.Green,
                            OperationType.Update => Color.Orange,
                            OperationType.Delete => Color.Red,
                        }
                )
                .WithFields(
                    entries
                        .Where(entry =>
                            entry.ShouldInclude
                            && !String.IsNullOrWhiteSpace(entry.Value?.ToString())
                        )
                        .Select(entry => new EmbedFieldBuilder
                        {
                            Name = entry.Key,
                            Value = entry
                                .Value?.ToString()
                                ?.Truncate(EmbedFieldBuilder.MaxFieldValueLength),
                        })
                )
                .WithFooter($"{footerPrefix} | {auditLogType}{operationType}")
                .WithCurrentTimestamp();

            IGuildUser bot = await guild.GetCurrentUserAsync();

            await webhookClient.SendMessageAsync(
                username: bot.Username,
                avatarUrl: bot.GetAvatarUrl(),
                embeds: [embed.Build()]
            );
        }
        finally
        {
            webhookClient.Dispose();
        }
    }

    private static string GetChangeEntry(object? before, object? after) =>
        $"{Format.Bold("Before:")} {before ?? "N/A"}\n{Format.Bold("After:")} {after ?? "N/A"}";

    private static (
        IEnumerable<GuildPermission> Added,
        IEnumerable<GuildPermission> Removed
    ) GetPermissionChanges(GuildPermissions before, GuildPermissions after)
    {
        List<GuildPermission> beforePerms = before.ToList();
        List<GuildPermission> afterPerms = after.ToList();
        return (afterPerms.Except(beforePerms), beforePerms.Except(afterPerms));
    }

    private record struct AuditLogEntry(string Key, object? Value, bool ShouldInclude = true);
}
