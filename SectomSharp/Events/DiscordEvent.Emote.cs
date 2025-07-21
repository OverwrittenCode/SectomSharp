using System.Collections.Immutable;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
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
                GuildEmote emote = newGuildEmotes[i];
                await HandleGuildEmoteAlteredAsync(newGuild, discordWebhookClient, OperationType.Create, emote);
            }

            return;
        }

        if (oldGuildEmotes.Length > newGuildEmotes.Length)
        {
            HashSet<GuildEmote> newEmotesSet = newGuildEmotes.ToHashSet();
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (GuildEmote emote in oldGuildEmotes)
            {
                if (!newEmotesSet.Contains(emote))
                {
                    await HandleGuildEmoteAlteredAsync(newGuild, discordWebhookClient, OperationType.Delete, emote);
                }
            }
            return;
        }

        for (int i = 0; i < oldGuildEmotes.Length; i++)
        {
            GuildEmote oldEmote = oldGuildEmotes[i];
            GuildEmote newEmote = newGuildEmotes[i];
            if (oldEmote.Name != newEmote.Name)
            {
                await LogAsync(
                    newGuild,
                    discordWebhookClient,
                    AuditLogType.Emoji,
                    OperationType.Update,
                    [EmbedFieldBuilderFactory.Create("Name", GetChangeEntry(oldEmote.Name, newEmote.Name))],
                    newEmote.Id,
                    newEmote.Name
                );
            }
        }
    }

    private static Task HandleGuildEmoteAlteredAsync(SocketGuild newGuild, DiscordWebhookClient discordWebhookClient, OperationType operationType, GuildEmote emote)
        => LogAsync(
            newGuild,
            discordWebhookClient,
            AuditLogType.Emoji,
            operationType,
            [
                EmbedFieldBuilderFactory.Create("Id", emote.Id),
                EmbedFieldBuilderFactory.Create("Name", emote.Name),
                EmbedFieldBuilderFactory.Create("Animated", emote.Animated),
                EmbedFieldBuilderFactory.Create("Url", emote.Url)
            ],
            emote.Id,
            emote.Name
        );
}
