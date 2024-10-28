﻿using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
#pragma warning disable CA1822
    public async Task HandleGuildMemberUpdatedAsync(
        Cacheable<SocketGuildUser, ulong> oldPartialUser,
        SocketGuildUser newUser
    )
#pragma warning restore CA1822
    {
        SocketGuildUser oldUser = await oldPartialUser.GetOrDownloadAsync();

        List<AuditLogEntry> entries =
        [
            new(
                "Username",
                GetChangeEntry(oldUser.Username, newUser.Username),
                oldUser.Username != newUser.Username
            ),
            new(
                "Nickname",
                GetChangeEntry(oldUser.Nickname, newUser.Nickname),
                oldUser.Nickname != newUser.Nickname
            ),
            new(
                "Global Name",
                GetChangeEntry(oldUser.GlobalName, newUser.GlobalName),
                oldUser.GlobalName != newUser.GlobalName
            ),
            new(
                "Display Name",
                GetChangeEntry(oldUser.DisplayName, newUser.DisplayName),
                oldUser.DisplayName != newUser.DisplayName
            ),
            new(
                "Avatar",
                GetChangeEntry(oldUser.GetAvatarUrl(), newUser.GetAvatarUrl()),
                oldUser.AvatarId != newUser.AvatarId
            ),
            new(
                "Server Avatar",
                GetChangeEntry(oldUser.GetGuildAvatarUrl(), newUser.GetGuildAvatarUrl()),
                oldUser.GuildAvatarId != newUser.GuildAvatarId
            )
        ];

        await LogAsync(
            newUser.Guild,
            AuditLogType.Member,
            OperationType.Update,
            entries,
            newUser.Id.ToString(),
            newUser.DisplayName,
            newUser.GetAvatarUrl()
        );
    }
}