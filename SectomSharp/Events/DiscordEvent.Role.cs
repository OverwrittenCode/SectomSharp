using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static string GetRoleDisplayName(SocketRole role) =>
        role.Emoji is null ? role.Name : $"{role.Emoji} {role.Name}";

    public async Task HandleRoleCreatedAsync(SocketRole role) =>
        await HandleRoleAlteredAsync(role, OperationType.Create);

    public async Task HandleRoleDeletedAsync(SocketRole role) =>
        await HandleRoleAlteredAsync(role, OperationType.Delete);

#pragma warning disable CA1822 // Mark members as static
    public async Task HandleRoleUpdateAsync(SocketRole before, SocketRole after)
#pragma warning restore CA1822 // Mark members as static
    {
        if (before.Position != after.Position)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(before.Name, after.Name), before.Name != after.Name),
            new(
                "Emoji",
                GetChangeEntry(before.Emoji?.ToString(), after.Emoji?.ToString()),
                before.Emoji?.ToString() != after.Emoji?.ToString()
            ),
            new(
                "Icon",
                GetChangeEntry(before.GetIconUrl(), after.GetIconUrl()),
                before.Icon != after.Icon
            ),
            new("Hoisted", $"Set to {after.IsHoisted}", before.IsHoisted != after.IsHoisted),
            new(
                "Mentionable",
                $"Set to {after.IsMentionable}",
                before.IsMentionable != after.IsMentionable
            ),
            new(
                "Hex Code",
                GetChangeEntry(
                    before.Color.ToHyperlinkedColourPicker(),
                    after.Color.ToHyperlinkedColourPicker()
                ),
                before.Color != after.Color
            ),
        ];

        if (before.Permissions.RawValue != after.Permissions.RawValue)
        {
            (IEnumerable<GuildPermission> added, IEnumerable<GuildPermission> removed) =
                GetPermissionChanges(before.Permissions, after.Permissions);

            List<GuildPermission> guildPermissions = added.ToList();
            if (guildPermissions.Count != 0)
            {
                entries.Add(new("Added Permissions", String.Join(", ", guildPermissions)));
            }

            List<GuildPermission> permissions = removed.ToList();
            if (permissions.Count != 0)
            {
                entries.Add(new("Removed Permissions", String.Join(", ", permissions)));
            }
        }

        await LogAsync(
            after.Guild,
            AuditLogType.Role,
            OperationType.Update,
            entries,
            after.Id.ToString(),
            GetRoleDisplayName(after),
            after.GetIconUrl(),
            after.Color
        );
    }

#pragma warning disable CA1822 // Mark members as static
    private async Task HandleRoleAlteredAsync(SocketRole role, OperationType operationType)
#pragma warning restore CA1822 // Mark members as static
    {
        List<AuditLogEntry> entries =
        [
            new("Position", role.Position),
            new("Hoisted", role.IsHoisted),
            new("Managed", role.IsManaged),
            new("Hex Code", role.Color.ToHyperlinkedColourPicker()),
            new("Permissions", String.Join(", ", role.Permissions.ToList())),
        ];

        if (role.GetIconUrl() is { } iconUrl)
        {
            entries.Add(new("Icon", iconUrl));
        }

        await LogAsync(
            role.Guild,
            AuditLogType.Role,
            operationType,
            entries,
            role.Id.ToString(),
            GetRoleDisplayName(role),
            role.GetIconUrl(),
            role.Color
        );
    }
}
