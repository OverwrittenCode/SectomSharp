using Discord;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    private static string GetRoleDisplayName(SocketRole role) => role.Emoji is null ? role.Name : $"{role.Emoji} {role.Name}";

    private static async Task HandleRoleAlteredAsync(SocketRole role, OperationType operationType)
    {
        if (await GetDiscordWebhookClientAsync(role.Guild, AuditLogType.Role) is not { } discordWebhookClient)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Position", role.Position),
            new("Hoisted", role.IsHoisted),
            new("Managed", role.IsManaged),
            new("Hex Code", role.Color.ToHyperlinkedColourPicker()),
            new("Permissions", String.Join(", ", role.Permissions.ToList()))
        ];

        if (role.GetIconUrl() is { } iconUrl)
        {
            entries.Add(new AuditLogEntry("Icon", iconUrl));
        }

        await LogAsync(role.Guild, discordWebhookClient, AuditLogType.Role, operationType, entries, role.Id.ToString(), GetRoleDisplayName(role), role.GetIconUrl(), role.Color);
    }

    public static async Task HandleRoleCreatedAsync(SocketRole role) => await HandleRoleAlteredAsync(role, OperationType.Create);

    public static async Task HandleRoleDeletedAsync(SocketRole role) => await HandleRoleAlteredAsync(role, OperationType.Delete);

    public static async Task HandleRoleUpdateAsync(SocketRole oldRole, SocketRole newRole)
    {
        if (oldRole.Position != newRole.Position || await GetDiscordWebhookClientAsync(newRole.Guild, AuditLogType.Role) is not { } discordWebhookClient)
        {
            return;
        }

        List<AuditLogEntry> entries =
        [
            new("Name", GetChangeEntry(oldRole.Name, newRole.Name), oldRole.Name != newRole.Name),
            new("Emoji", GetChangeEntry(oldRole.Emoji?.ToString(), newRole.Emoji?.ToString()), oldRole.Emoji?.ToString() != newRole.Emoji?.ToString()),
            new("Icon", GetChangeEntry(oldRole.GetIconUrl(), newRole.GetIconUrl()), oldRole.Icon != newRole.Icon),
            new("Hoisted", $"Set to {newRole.IsHoisted}", oldRole.IsHoisted != newRole.IsHoisted),
            new("Mentionable", $"Set to {newRole.IsMentionable}", oldRole.IsMentionable != newRole.IsMentionable),
            new("Hex Code", GetChangeEntry(oldRole.Color.ToHyperlinkedColourPicker(), newRole.Color.ToHyperlinkedColourPicker()), oldRole.Color != newRole.Color)
        ];

        if (oldRole.Permissions.RawValue != newRole.Permissions.RawValue)
        {
            var beforeSet = new HashSet<GuildPermission>(oldRole.Permissions.ToList());
            var afterSet = new HashSet<GuildPermission>(newRole.Permissions.ToList());

            if (String.Join(", ", afterSet.Except(beforeSet)) is { Length: > 0 } addedPermissions)
            {
                entries.Add(new AuditLogEntry("Added Permissions", addedPermissions));
            }

            if (String.Join(", ", beforeSet.Except(afterSet)) is { Length: > 0 } removedPermissions)
            {
                entries.Add(new AuditLogEntry("Removed Permissions", removedPermissions));
            }
        }

        await LogAsync(
            newRole.Guild,
            discordWebhookClient,
            AuditLogType.Role,
            OperationType.Update,
            entries,
            newRole.Id.ToString(),
            GetRoleDisplayName(newRole),
            newRole.GetIconUrl(),
            newRole.Color
        );
    }
}
