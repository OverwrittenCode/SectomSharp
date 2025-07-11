using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    public async Task HandleGuildMemberUpdatedAsync(Cacheable<SocketGuildUser, ulong> oldPartialUser, SocketGuildUser newUser)
    {
        SocketGuildUser oldUser = await oldPartialUser.GetOrDownloadAsync();

        List<EmbedFieldBuilder> builders = new(6);
        AddIfChanged(builders, "Username", oldUser.Username, newUser.Username);
        AddIfChanged(builders, "Nickname", oldUser.Nickname, newUser.Nickname);
        AddIfChanged(builders, "Global Name", oldUser.GlobalName, newUser.GlobalName);
        AddIfChanged(builders, "Display Name", oldUser.DisplayName, newUser.DisplayName);
        if (oldUser.AvatarId != newUser.AvatarId)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Avatar", GetChangeEntry(oldUser.GetAvatarUrl(), newUser.GetAvatarUrl())));
        }

        if (oldUser.GuildAvatarId != newUser.GuildAvatarId)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Server Avatar", GetChangeEntry(oldUser.GetGuildAvatarUrl(), newUser.GetGuildAvatarUrl())));
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newUser.Guild, AuditLogType.Member);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(newUser.Guild, webhookClient, AuditLogType.Member, OperationType.Update, builders, newUser.Id, newUser.DisplayName, newUser.GetAvatarUrl());
    }
}
