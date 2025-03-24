using System.ComponentModel;
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
            ICollection<AuditLogChannel>? logChannels = await db.Guilds.Where(x => x.Id == guild.Id)
                                                                .AsNoTracking()
                                                                .Include(x => x.AuditLogChannels)
                                                                .Select(x => x.AuditLogChannels)
                                                                .FirstOrDefaultAsync();

            if (logChannels?.FirstOrDefault(channel => channel.Type.HasFlag(auditLogType)) is not { } auditLogChannel)
            {
                return;
            }

            webhookClient = new DiscordWebhookClient(auditLogChannel.WebhookUrl);
        }

        try
        {
            EmbedBuilder embed = new EmbedBuilder().WithAuthor(authorName, authorIconUrl)
                                                   .WithColor(
                                                        colour
                                                     ?? operationType switch
                                                        {
                                                            OperationType.Create => Color.Green,
                                                            OperationType.Update => Color.Orange,
                                                            OperationType.Delete => Color.Red,
                                                            _ => throw new InvalidEnumArgumentException(nameof(operationType), (int)operationType, typeof(OperationType))
                                                        }
                                                    )
                                                   .WithFields(
                                                        from entry in entries
                                                        where entry.ShouldInclude
                                                        let fieldValue = entry.Value?.ToString()?.Truncate(EmbedFieldBuilder.MaxFieldValueLength)
                                                        where !String.IsNullOrEmpty(fieldValue)
                                                        select new EmbedFieldBuilder
                                                        {
                                                            Name = entry.Key,
                                                            Value = fieldValue
                                                        }
                                                    )
                                                   .WithFooter($"{footerPrefix} | {auditLogType}{operationType}")
                                                   .WithCurrentTimestamp();

            IGuildUser bot = await guild.GetCurrentUserAsync();

            await webhookClient.SendMessageAsync(username: bot.Username, avatarUrl: bot.GetAvatarUrl(), embeds: [embed.Build()]);
        }
        finally
        {
            webhookClient.Dispose();
        }
    }

    private static string GetChangeEntry<T>(T before, T after)
        => $"{Format.Bold("Before:")} {(before is null ? "N/A" : before)}\n{Format.Bold("After:")} {(after is null ? "N/A" : after)}";

    private record struct AuditLogEntry(string Key, object? Value, bool ShouldInclude = true);
}
