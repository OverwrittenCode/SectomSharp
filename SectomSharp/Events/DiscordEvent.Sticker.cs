using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
#pragma warning disable CA1822
    private async Task HandleGuildStickerAlteredAsync(SocketCustomSticker sticker, OperationType operationType)
#pragma warning restore CA1822
    {
        List<AuditLogEntry> entries =
        [
            new("Id", sticker.Id),
            new("Name", sticker.Name),
            new("Description", sticker.Description),
            new("Format", sticker.Format)
        ];

        await LogAsync(sticker.Guild, AuditLogType.Sticker, operationType, entries, sticker.Guild.Id.ToString(), sticker.Name);
    }

    public async Task HandleGuildStickerCreatedAsync(SocketCustomSticker sticker) => await HandleGuildStickerAlteredAsync(sticker, OperationType.Create);

    public async Task HandleGuildStickerDeletedAsync(SocketCustomSticker sticker) => await HandleGuildStickerAlteredAsync(sticker, OperationType.Delete);

#pragma warning disable CA1822
    public async Task HandleGuildStickerUpdatedAsync(SocketCustomSticker oldSticker, SocketCustomSticker newSticker)
#pragma warning restore CA1822
    {
        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldSticker.Name, newSticker.Name), oldSticker.Name != newSticker.Name),
            new("Description", GetChangeEntry(oldSticker.Description, newSticker.Description), oldSticker.Description != newSticker.Description)
        ];

        await LogAsync(newSticker.Guild, AuditLogType.Sticker, OperationType.Update, entries, newSticker.Id.ToString(), newSticker.Name);
    }
}
