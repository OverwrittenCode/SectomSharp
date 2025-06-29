using System.ComponentModel;
using Discord;
using Discord.Webhook;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private static async Task LogAsync(
        IGuild guild,
        DiscordWebhookClient webhookClient,
        AuditLogType auditLogType,
        OperationType operationType,
        IEnumerable<AuditLogEntry> entries,
        string footerPrefix,
        string authorName,
        string? authorIconUrl = null,
        Color? colour = null
    )
    {
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

            if (embed.Fields.Count > 0)
            {
                IGuildUser bot = await guild.GetCurrentUserAsync();

                await webhookClient.SendMessageAsync(username: bot.Username, avatarUrl: bot.GetAvatarUrl(), embeds: [embed.Build()]);
            }
        }
        finally
        {
            webhookClient.Dispose();
        }
    }

    private static string GetChangeEntry<T>(T before, T after)
        => $"{Format.Bold("Before:")} {(before is null ? "N/A" : before)}\n{Format.Bold("After:")} {(after is null ? "N/A" : after)}";

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;

    public DiscordEvent(IDbContextFactory<ApplicationDbContext> dbFactory) => _dbFactory = dbFactory;

    private async Task<DiscordWebhookClient?> GetDiscordWebhookClientAsync(IGuild guild, AuditLogType auditLogType)
    {
        await using ApplicationDbContext db = await _dbFactory.CreateDbContextAsync();

        var result = await db.AuditLogChannels.Where(channel => channel.GuildId == guild.Id && channel.Type.HasFlag(auditLogType))
                             .Select(channel => new { channel.WebhookUrl })
                             .FirstOrDefaultAsync();

        return result is null ? null : new DiscordWebhookClient(result.WebhookUrl);
    }

    private record struct AuditLogEntry(string Key, object? Value, bool ShouldInclude = true);
}
