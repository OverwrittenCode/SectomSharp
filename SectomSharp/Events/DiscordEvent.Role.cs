using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static string GetRoleDisplayName(SocketRole role) =>
        role.Emoji is null ? role.Name : $"{role.Emoji} {role.Name}";

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

        if (role.GetIconUrl() is string iconURL)
        {
            entries.Add(new("Icon", iconURL));
        }

        await LogAsync(
            role.Guild,
            AuditLogType.Role,
            operationType,
            entries,
            footerPrefix: role.Id.ToString(),
            authorName: GetRoleDisplayName(role),
            authorIconURL: role.GetIconUrl(),
            colour: role.Color
        );
    }

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
            var (added, removed) = GetPermissionChanges(before.Permissions, after.Permissions);

            if (added.Any())
            {
                entries.Add(new("Added Permissions", String.Join(", ", added)));
            }

            if (removed.Any())
            {
                entries.Add(new("Removed Permissions", String.Join(", ", removed)));
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
}
