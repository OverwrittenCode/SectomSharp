using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private async Task HandleGuildStickerAlteredAsync(SocketCustomSticker sticker, OperationType operationType)
    {
        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(sticker.Guild, AuditLogType.Sticker);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(
            sticker.Guild,
            webhookClient,
            AuditLogType.Sticker,
            operationType,
            [
                EmbedFieldBuilderFactory.Create("Id", sticker.Id),
                EmbedFieldBuilderFactory.Create("Name", sticker.Name),
                EmbedFieldBuilderFactory.Create("Description", sticker.Description),
                EmbedFieldBuilderFactory.Create("Format", sticker.Format)
            ],
            sticker.Guild.Id,
            sticker.Name
        );
    }

    public Task HandleGuildStickerCreatedAsync(SocketCustomSticker sticker) => HandleGuildStickerAlteredAsync(sticker, OperationType.Create);

    public Task HandleGuildStickerDeletedAsync(SocketCustomSticker sticker) => HandleGuildStickerAlteredAsync(sticker, OperationType.Delete);

    public async Task HandleGuildStickerUpdatedAsync(SocketCustomSticker oldSticker, SocketCustomSticker newSticker)
    {
        List<EmbedFieldBuilder> builders = new(2);
        AddIfChanged(builders, "Name", oldSticker.Name, newSticker.Name);
        AddIfChanged(builders, "Description", oldSticker.Description, newSticker.Description);
        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newSticker.Guild, AuditLogType.Sticker);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(newSticker.Guild, webhookClient, AuditLogType.Sticker, OperationType.Update, builders, newSticker.Id, newSticker.Name);
    }
}
