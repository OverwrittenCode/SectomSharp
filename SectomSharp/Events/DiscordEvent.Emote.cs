using System.Collections.Immutable;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public static partial class DiscordEvent
{
    private static async Task HandleGuildEmoteAsync(
        SocketGuild newGuild,
        DiscordWebhookClient discordWebhookClient,
        ImmutableArray<GuildEmote> oldGuildEmotes,
        ImmutableArray<GuildEmote> newGuildEmotes
    )
    {
        if (oldGuildEmotes.Length < newGuildEmotes.Length)
        {
            // collection is sorted, ascending, based on creation date
            // so we know where the new items are located
            int addedCount = newGuildEmotes.Length - oldGuildEmotes.Length;
            for (int i = newGuildEmotes.Length - addedCount; i < newGuildEmotes.Length; i++)
            {
                await HandleGuildEmoteAddedAsync(newGuild, discordWebhookClient, newGuildEmotes[i]);
            }

            return;
        }

        if (oldGuildEmotes.Length > newGuildEmotes.Length)
        {
            foreach (GuildEmote emote in new HashSet<GuildEmote>(oldGuildEmotes).Except(new HashSet<GuildEmote>(newGuildEmotes)))
            {
                await HandleGuildEmoteRemovedAsync(newGuild, discordWebhookClient, emote);
            }

            return;
        }

        for (int i = 0; i < oldGuildEmotes.Length; i++)
        {
            GuildEmote oldEmote = oldGuildEmotes[i];
            GuildEmote newEmote = newGuildEmotes[i];
            if (oldEmote.Name != newEmote.Name)
            {
                await HandleGuildEmoteUpdatedAsync(newGuild, discordWebhookClient, oldEmote, newEmote);
            }
        }
    }

    private static async Task HandleGuildEmoteAlteredAsync(SocketGuild newGuild, DiscordWebhookClient discordWebhookClient, OperationType operationType, GuildEmote emote)
    {
        List<AuditLogEntry> entries =
        [
            new("Id", emote.Id),
            new("Name", emote.Name),
            new("Animated", emote.Animated),
            new("Url", emote.Url)
        ];

        await LogAsync(newGuild, discordWebhookClient, AuditLogType.Emoji, operationType, entries, emote.Id.ToString(), emote.Name);
    }

    private static async Task HandleGuildEmoteAddedAsync(SocketGuild newGuild, DiscordWebhookClient discordWebhookClient, GuildEmote emote)
        => await HandleGuildEmoteAlteredAsync(newGuild, discordWebhookClient, OperationType.Create, emote);

    private static async Task HandleGuildEmoteRemovedAsync(SocketGuild newGuild, DiscordWebhookClient discordWebhookClient, GuildEmote emote)
        => await HandleGuildEmoteAlteredAsync(newGuild, discordWebhookClient, OperationType.Delete, emote);

    private static async Task HandleGuildEmoteUpdatedAsync(SocketGuild newGuild, DiscordWebhookClient discordWebhookClient, GuildEmote oldEmote, GuildEmote newEmote)
    {
        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldEmote.Name, newEmote.Name))
        ];

        await LogAsync(newGuild, discordWebhookClient, AuditLogType.Emoji, OperationType.Update, entries, newEmote.Id.ToString(), newEmote.Name);
    }
}
