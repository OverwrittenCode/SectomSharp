using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Discord;
using Discord.Webhook;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private static async Task LogAsync<T>(
        IGuild guild,
        DiscordWebhookClient webhookClient,
        AuditLogType auditLogType,
        OperationType operationType,
        List<EmbedFieldBuilder> embedFieldBuilders,
        T footerPrefix,
        string authorName,
        string? authorIconUrl = null,
        Color? colour = null
    )
    {
        Debug.Assert(embedFieldBuilders.Count > 0);

        var embedBuilder = new EmbedBuilder
        {
            Author = new EmbedAuthorBuilder
            {
                Name = authorName,
                IconUrl = authorIconUrl
            },
            Color = colour
                 ?? operationType switch
                    {
                        OperationType.Create => Color.Green,
                        OperationType.Update => Color.Orange,
                        OperationType.Delete => Color.Red,
                        _ => throw new InvalidEnumArgumentException(nameof(operationType), (int)operationType, typeof(OperationType))
                    },
            Fields = embedFieldBuilders,
            Footer = new EmbedFooterBuilder { Text = $"{footerPrefix} | {auditLogType}{operationType}" },
            Timestamp = DateTimeOffset.UtcNow
        };

        IGuildUser bot = await guild.GetCurrentUserAsync();
        await webhookClient.SendMessageAsync(username: bot.Username, avatarUrl: bot.GetAvatarUrl(), embeds: [embedBuilder.Build()]);
    }

    private static string GetChangeEntry<T>(T before, T after)
        => $"""
            **Before:** {(before is null ? "N/A" : before)}
            **After:** {(after is null ? "N/A" : after)}
            """;

    private static void AddIfChanged(List<EmbedFieldBuilder> builders, [ConstantExpected] string key, bool before, bool after)
    {
        if (before != after)
        {
            builders.Add(EmbedFieldBuilderFactory.Create(key, after ? "Set to True" : "Set to False"));
        }
    }

    private static void AddIfChanged<T>(List<EmbedFieldBuilder> builders, [ConstantExpected] string key, T before, T after)
    {
        if (!EqualityComparer<T>.Default.Equals(before, after))
        {
            builders.Add(EmbedFieldBuilderFactory.CreateTruncated(key, GetChangeEntry(before, after)));
        }
    }

    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ILogger<DiscordEvent> _logger;

    public DiscordEvent(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<DiscordEvent> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    [MustDisposeResource]
    private async Task<DiscordWebhookClient?> GetDiscordWebhookClientAsync(ulong guildId, AuditLogType auditLogType)
    {
        await using ApplicationDbContext db = await _dbFactory.CreateDbContextAsync();

        IQueryable<AuditLogChannel> queryable = db.AuditLogChannels.Where(channel => channel.GuildId == guildId && channel.Type.HasFlag(auditLogType));
        string? webhookUrl = await queryable.Select(channel => channel.WebhookUrl).FirstOrDefaultAsync();

        if (webhookUrl is null)
        {
            return null;
        }

        try
        {
            return new DiscordWebhookClient(webhookUrl);
        }
        catch (InvalidOperationException ex) when (ex.Message == "Could not find a webhook with the supplied credentials.")
        {
            await queryable.ExecuteDeleteAsync();
            return null;
        }
    }
}
