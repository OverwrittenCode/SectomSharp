using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private static string GetRoleDisplayName(SocketRole role) => role.Emoji is null ? role.Name : $"{role.Emoji} {role.Name}";

    private async Task HandleRoleAlteredAsync(SocketRole role, OperationType operationType)
    {
        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(role.Guild.Id, AuditLogType.Role);
        if (webhookClient is null)
        {
            return;
        }

        List<EmbedFieldBuilder> builders = new(6)
        {
            EmbedFieldBuilderFactory.Create("Position", role.Position),
            EmbedFieldBuilderFactory.Create("Hoisted", role.IsHoisted),
            EmbedFieldBuilderFactory.Create("Managed", role.IsManaged),
            EmbedFieldBuilderFactory.Create("Hex Code", role.Color.ToHyperlinkedColourPicker()),
            EmbedFieldBuilderFactory.CreateTruncated("Permissions", String.Join(", ", role.Permissions.ToList()))
        };

        if (role.GetIconUrl() is { } iconUrl)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Icon", iconUrl));
        }

        await LogAsync(role.Guild, webhookClient, AuditLogType.Role, operationType, builders, role.Id, GetRoleDisplayName(role), role.GetIconUrl(), role.Color);
    }

    public Task HandleRoleCreatedAsync(SocketRole role) => HandleRoleAlteredAsync(role, OperationType.Create);

    public Task HandleRoleDeletedAsync(SocketRole role) => HandleRoleAlteredAsync(role, OperationType.Delete);

    public async Task HandleRoleUpdateAsync(SocketRole oldRole, SocketRole newRole)
    {
        if (oldRole.Position != newRole.Position)
        {
            return;
        }

        List<EmbedFieldBuilder> builders = new(8);
        AddIfChanged(builders, "Name", oldRole.Name, newRole.Name);
        AddIfChanged(builders, "Emoji", oldRole.Emoji, newRole.Emoji);
        if (oldRole.Icon != newRole.Icon)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Icon", GetChangeEntry(oldRole.GetIconUrl(), newRole.GetIconUrl())));
        }

        AddIfChanged(builders, "Hoisted", oldRole.IsHoisted, newRole.IsHoisted);
        AddIfChanged(builders, "Mentionable", oldRole.IsMentionable, newRole.IsMentionable);
        if (oldRole.Color != newRole.Color)
        {
            builders.Add(EmbedFieldBuilderFactory.Create("Hex Code", GetChangeEntry(oldRole.Color.ToHyperlinkedColourPicker(), newRole.Color.ToHyperlinkedColourPicker())));
        }

        if (oldRole.Permissions.RawValue != newRole.Permissions.RawValue)
        {
            var beforeSet = new HashSet<GuildPermission>(oldRole.Permissions.ToList());
            var afterSet = new HashSet<GuildPermission>(newRole.Permissions.ToList());

            if (String.Join(", ", afterSet.Except(beforeSet)) is { Length: > 0 } addedPermissions)
            {
                builders.Add(EmbedFieldBuilderFactory.CreateTruncated("Added Permissions", addedPermissions));
            }

            if (String.Join(", ", beforeSet.Except(afterSet)) is { Length: > 0 } removedPermissions)
            {
                builders.Add(EmbedFieldBuilderFactory.CreateTruncated("Removed Permissions", removedPermissions));
            }
        }

        if (builders.Count == 0)
        {
            return;
        }

        using DiscordWebhookClient? webhookClient = await GetDiscordWebhookClientAsync(newRole.Guild.Id, AuditLogType.Role);
        if (webhookClient is null)
        {
            return;
        }

        await LogAsync(
            newRole.Guild,
            webhookClient,
            AuditLogType.Role,
            OperationType.Update,
            builders,
            newRole.Id,
            GetRoleDisplayName(newRole),
            newRole.GetIconUrl(),
            newRole.Color
        );
    }
}
