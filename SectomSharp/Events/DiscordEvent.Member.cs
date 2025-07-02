using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    public async Task HandleGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> oldPartialUser, SocketGuildUser newUser)
    {
        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newUser.Guild, AuditLogType.Member);
        if (webhookClient is null)
        {
            return;
        }

        SocketGuildUser oldUser = await oldPartialUser.GetOrDownloadAsync();

        List<AuditLogEntry> entries =
        [
            new("Username", GetChangeEntry(oldUser.Username, newUser.Username), oldUser.Username != newUser.Username),
            new("Nickname", GetChangeEntry(oldUser.Nickname, newUser.Nickname), oldUser.Nickname != newUser.Nickname),
            new("Global Name", GetChangeEntry(oldUser.GlobalName, newUser.GlobalName), oldUser.GlobalName != newUser.GlobalName),
            new("Display Name", GetChangeEntry(oldUser.DisplayName, newUser.DisplayName), oldUser.DisplayName != newUser.DisplayName),
            new("Avatar", GetChangeEntry(oldUser.GetAvatarUrl(), newUser.GetAvatarUrl()), oldUser.AvatarId != newUser.AvatarId),
            new("Server Avatar", GetChangeEntry(oldUser.GetGuildAvatarUrl(), newUser.GetGuildAvatarUrl()), oldUser.GuildAvatarId != newUser.GuildAvatarId)
        ];

        await LogAsync(newUser.Guild, webhookClient, AuditLogType.Member, OperationType.Update, entries, newUser.Id.ToString(), newUser.DisplayName, newUser.GetAvatarUrl());
    }
}
