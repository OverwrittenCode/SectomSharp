using Discord;
using Discord.Rest;
using SectomSharp.Attributes;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    private const GuildPermission DangerousGuildPermissions = GuildPermission.Administrator
                                                            | GuildPermission.ManageGuild
                                                            | GuildPermission.ManageWebhooks
                                                            | GuildPermission.ManageRoles
                                                            | GuildPermission.ManageChannels
                                                            | GuildPermission.ManageEvents
                                                            | GuildPermission.ManageEmojisAndStickers
                                                            | GuildPermission.BanMembers
                                                            | GuildPermission.KickMembers
                                                            | GuildPermission.ModerateMembers
                                                            | GuildPermission.ManageMessages
                                                            | GuildPermission.MentionEveryone;

    private static readonly UserProperties[] UserPropertiesArray = Enum.GetValues<UserProperties>().Skip(1).ToArray();

    private static readonly Dictionary<UserProperties, string> Badges = new()
    {
        { UserProperties.Staff, "<:Staff:1301356970850258985>" },
        { UserProperties.Partner, "<:Partner:1301356982758015056>" },
        { UserProperties.HypeSquadEvents, "<:HypeSquadEvents:1301356855683059804>" },
        { UserProperties.BugHunterLevel1, "<:BugHunterLevel1:1301357035601789040>" },
        { UserProperties.HypeSquadBravery, "<:HypeSquadBravery:1301356944606494772>" },
        { UserProperties.HypeSquadBrilliance, "<:HypeSquadBrilliance:1301356870526832760>" },
        { UserProperties.HypeSquadBalance, "<:HypeSquadBalance:1301356957168570448>" },
        { UserProperties.EarlySupporter, "<:EarlySupporter:1301357010553671680>" },
        { UserProperties.BugHunterLevel2, "<:BugHunterLevel2:1301357023476322314>" },
        { UserProperties.VerifiedBot, "<:VerifiedBot:1301356829871444020>" },
        { UserProperties.EarlyVerifiedBotDeveloper, "<:EarlyVerifiedBotDeveloper:1301356781754384504>" },
        { UserProperties.DiscordCertifiedModerator, "<:CertifiedModerator:1301356995638722592>" },
        { UserProperties.ActiveDeveloper, "<:ActiveDeveloper:1301357048205934623>" }
    };

    [SlashCmd("Get information about a user in the server")]
    public async Task UserInfo(IGuildUser? user = null)
    {
        RestGuildUser? restUser = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, (user ?? (IGuildUser)Context.User).Id);
        EmbedBuilder embedBuilder = new EmbedBuilder().WithAuthor(restUser.Username, restUser.GetDisplayAvatarUrl())
                                                      .WithThumbnailUrl(restUser.GetDisplayAvatarUrl())
                                                      .WithColor(restUser.AccentColor ?? Color.Purple)
                                                      .WithDescription($"{restUser.Mention} ({restUser.DisplayName})")
                                                      .WithFooter($"ID: {restUser.Id}")
                                                      .WithCurrentTimestamp();

        EmbedFieldBuilder createdAtField = new EmbedFieldBuilder().WithName("Created At").WithValue(restUser.CreatedAt.DateTime.GetRelativeTimestamp());

        if (restUser.JoinedAt is { DateTime: var joinedAt })
        {
            embedBuilder.AddField(createdAtField.WithIsInline(true)).AddField("Joined At", joinedAt.GetRelativeTimestamp(), true);
        }
        else
        {
            embedBuilder.AddField(createdAtField);
        }

        if (restUser.PremiumSince is { DateTime: var premiumSince })
        {
            embedBuilder.AddField("Premium Since", premiumSince.GetRelativeTimestamp());
        }

        if (restUser.Nickname is { } nickname)
        {
            embedBuilder.AddField("Nickname", nickname);
        }

        if (restUser.PublicFlags is { } publicFlags)
        {
            List<string> badges = UserPropertiesArray.Where(property => publicFlags.HasFlag(property))
                                                     .Select(property => Badges.TryGetValue(property, out string? emoji)
                                                          ? emoji
                                                          : $"{Format.Code(StringUtils.PascalCaseToSentenceCase(property.ToString()))}"
                                                      )
                                                     .ToList();

            embedBuilder.AddField($"Badges [{badges.Count}]", String.Join(' ', badges));
        }

        if (restUser.RoleIds.Skip(1).Select(MentionUtils.MentionRole).ToList() is { Count: > 1 and var roleCount } roleMentions)
        {
            embedBuilder.AddField($"Roles [{roleCount}]", String.Join(", ", roleMentions).Truncate(EmbedFieldBuilder.MaxFieldValueLength));
        }

        if (restUser.GuildPermissions.ToList()
                    .Where(guildPermission => DangerousGuildPermissions.HasFlag(guildPermission))
                    .Select(permission => StringUtils.PascalCaseToSentenceCase(permission.ToString()))
                    .ToList() is { Count: > 0 and var dangerousPermissionCount } dangerousPermissions)
        {
            embedBuilder.AddField($"Dangerous Permissions [{dangerousPermissionCount}]", String.Join(", ", dangerousPermissions).Truncate(EmbedFieldBuilder.MaxFieldValueLength));
        }

        await RespondOrFollowupAsync(embeds: [embedBuilder.Build()]);
    }
}
