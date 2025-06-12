using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public static partial class DiscordEvent
{
    private static async Task HandleGuildStickerAlteredAsync(SocketCustomSticker sticker, OperationType operationType)
    {
        if (await GetDiscordWebhookClientAsync(sticker.Guild, AuditLogType.Sticker) is not { } webhookClient)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Id", sticker.Id),
            new("Name", sticker.Name),
            new("Description", sticker.Description),
            new("Format", sticker.Format)
        ];

        await LogAsync(sticker.Guild, webhookClient, AuditLogType.Sticker, operationType, entries, sticker.Guild.Id.ToString(), sticker.Name);
    }

    public static async Task HandleGuildStickerCreatedAsync(SocketCustomSticker sticker) => await HandleGuildStickerAlteredAsync(sticker, OperationType.Create);

    public static async Task HandleGuildStickerDeletedAsync(SocketCustomSticker sticker) => await HandleGuildStickerAlteredAsync(sticker, OperationType.Delete);

    public static async Task HandleGuildStickerUpdatedAsync(SocketCustomSticker oldSticker, SocketCustomSticker newSticker)
    {
        if (await GetDiscordWebhookClientAsync(newSticker.Guild, AuditLogType.Sticker) is not { } webhookClient)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldSticker.Name, newSticker.Name), oldSticker.Name != newSticker.Name),
            new("Description", GetChangeEntry(oldSticker.Description, newSticker.Description), oldSticker.Description != newSticker.Description)
        ];

        await LogAsync(newSticker.Guild, webhookClient, AuditLogType.Sticker, OperationType.Update, entries, newSticker.Id.ToString(), newSticker.Name);
    }
}
