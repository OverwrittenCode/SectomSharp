using Discord;
using Discord.Rest;
using SectomSharp.Attributes;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    private static readonly (GuildPermission permission, string sentenceCaseDisplay)[] DangerousGuildPermissionsArray = new[]
        {
            GuildPermission.Administrator,
            GuildPermission.ManageGuild,
            GuildPermission.ManageWebhooks,
            GuildPermission.ManageRoles,
            GuildPermission.ManageChannels,
            GuildPermission.ManageEvents,
            GuildPermission.ManageEmojisAndStickers,
            GuildPermission.BanMembers,
            GuildPermission.KickMembers,
            GuildPermission.ModerateMembers,
            GuildPermission.ManageMessages,
            GuildPermission.MentionEveryone
        }.Select(p => (p, StringUtils.PascalCaseToSentenceCase(p.ToString())))
         .ToArray();

    private static readonly Dictionary<UserProperties, string> AllBadges = Enum.GetValues<UserProperties>()
                                                                               .Skip(1)
                                                                               .ToDictionary(
                                                                                    prop => prop,
                                                                                    prop => prop switch
                                                                                    {
                                                                                        UserProperties.Staff => "<:Staff:1301356970850258985>",
                                                                                        UserProperties.Partner => "<:Partner:1301356982758015056>",
                                                                                        UserProperties.HypeSquadEvents => "<:HypeSquadEvents:1301356855683059804>",
                                                                                        UserProperties.BugHunterLevel1 => "<:BugHunterLevel1:1301357035601789040>",
                                                                                        UserProperties.HypeSquadBravery => "<:HypeSquadBravery:1301356944606494772>",
                                                                                        UserProperties.HypeSquadBrilliance => "<:HypeSquadBrilliance:1301356870526832760>",
                                                                                        UserProperties.HypeSquadBalance => "<:HypeSquadBalance:1301356957168570448>",
                                                                                        UserProperties.EarlySupporter => "<:EarlySupporter:1301357010553671680>",
                                                                                        UserProperties.BugHunterLevel2 => "<:BugHunterLevel2:1301357023476322314>",
                                                                                        UserProperties.VerifiedBot => "<:VerifiedBot:1301356829871444020>",
                                                                                        UserProperties.EarlyVerifiedBotDeveloper =>
                                                                                            "<:EarlyVerifiedBotDeveloper:1301356781754384504>",
                                                                                        UserProperties.DiscordCertifiedModerator => "<:CertifiedModerator:1301356995638722592>",
                                                                                        UserProperties.ActiveDeveloper => "<:ActiveDeveloper:1301357048205934623>",
                                                                                        _ => $"`{StringUtils.PascalCaseToSentenceCase(prop.ToString())}`"
                                                                                    }
                                                                                );

    [SlashCmd("Get information about a user in the server")]
    public async Task UserInfo(IGuildUser? user = null)
    {
        RestGuildUser? restUser = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, (user ?? (IGuildUser)Context.User).Id);

        EmbedFieldBuilder createdAtField = EmbedFieldBuilderFactory.Create("Created At", restUser.CreatedAt.GetRelativeTimestamp());
        var fields = new List<EmbedFieldBuilder>(7) { createdAtField };

        if (restUser.JoinedAt is { } joinedAt)
        {
            createdAtField.IsInline = true;
            fields.Add(EmbedFieldBuilderFactory.CreateInlined("Joined At", joinedAt.GetRelativeTimestamp()));
        }

        if (restUser.PremiumSince is { } premiumSince)
        {
            fields.Add(EmbedFieldBuilderFactory.Create("Premium Since", premiumSince.GetRelativeTimestamp()));
        }

        if (restUser.Nickname is { } nickname)
        {
            fields.Add(EmbedFieldBuilderFactory.Create("Nickname", nickname));
        }

        if (restUser.PublicFlags is { } publicFlags)
        {
            string[] badges = AllBadges.Where(pair => publicFlags.HasFlag(pair.Key)).Select(pair => pair.Value).ToArray();
            fields.Add(EmbedFieldBuilderFactory.Create($"Badges [{badges.Length}]", String.Join(' ', badges)));
        }

        const int literalLength = 4;
        const int snowflakeIdLength = 20;
        const int roleMentionLength = literalLength + snowflakeIdLength;
        const int separatorLength = 2;
        const int maxItems = (EmbedFieldBuilder.MaxFieldValueLength + separatorLength) / (roleMentionLength + separatorLength);
        if (restUser.RoleIds.Skip(1).Take(maxItems).Select(id => $"<@&{id}>").ToArray() is { Length: > 1 and var roleCount } roleMentions)
        {
            fields.Add(EmbedFieldBuilderFactory.Create($"Roles [{roleCount}]", String.Join(", ", roleMentions)));
        }

        GuildPermissions perms = restUser.GuildPermissions;
        if (DangerousGuildPermissionsArray.Where(tuple => perms.Has(tuple.permission)).ToArray() is { Length: > 0 and var dangerousPermissionCount } dangerousPermissions)
        {
            fields.Add(EmbedFieldBuilderFactory.Create($"Dangerous Permissions [{dangerousPermissionCount}]", String.Join(", ", dangerousPermissions)));
        }

        string displayAvatarUrl = restUser.GetDisplayAvatarUrl();

        await RespondAsync(
            embeds:
            [
                new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = restUser.Username,
                        IconUrl = displayAvatarUrl
                    },
                    ThumbnailUrl = displayAvatarUrl,
                    Color = restUser.AccentColor ?? Color.Purple,
                    Description = $"{restUser.Mention} ({restUser.DisplayName})",
                    Fields = fields,
                    Footer = new EmbedFooterBuilder { Text = $"ID: {restUser.Id}" },
                    Timestamp = DateTimeOffset.Now
                }.Build()
            ]
        );
    }
}
