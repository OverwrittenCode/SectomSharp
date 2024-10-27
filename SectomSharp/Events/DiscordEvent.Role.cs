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
    public async Task HandleRoleUpdateAsync(SocketRole oldRole, SocketRole newRole)
#pragma warning restore CA1822 // Mark members as static
    {
        if (oldRole.Position != newRole.Position)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldRole.Name, newRole.Name), oldRole.Name != newRole.Name),
            new(
                "Emoji",
                GetChangeEntry(oldRole.Emoji?.ToString(), newRole.Emoji?.ToString()),
                oldRole.Emoji?.ToString() != newRole.Emoji?.ToString()
            ),
            new(
                "Icon",
                GetChangeEntry(oldRole.GetIconUrl(), newRole.GetIconUrl()),
                oldRole.Icon != newRole.Icon
            ),
            new("Hoisted", $"Set to {newRole.IsHoisted}", oldRole.IsHoisted != newRole.IsHoisted),
            new(
                "Mentionable",
                $"Set to {newRole.IsMentionable}",
                oldRole.IsMentionable != newRole.IsMentionable
            ),
            new(
                "Hex Code",
                GetChangeEntry(
                    oldRole.Color.ToHyperlinkedColourPicker(),
                    newRole.Color.ToHyperlinkedColourPicker()
                ),
                oldRole.Color != newRole.Color
            ),
        ];

        if (oldRole.Permissions.RawValue != newRole.Permissions.RawValue)
        {
            (IEnumerable<GuildPermission> added, IEnumerable<GuildPermission> removed) =
                GetPermissionChanges(oldRole.Permissions, newRole.Permissions);

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
            newRole.Guild,
            AuditLogType.Role,
            OperationType.Update,
            entries,
            newRole.Id.ToString(),
            GetRoleDisplayName(newRole),
            newRole.GetIconUrl(),
            newRole.Color
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
