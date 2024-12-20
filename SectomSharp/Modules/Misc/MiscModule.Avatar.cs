using Discord;
using Discord.Interactions;
using Discord.Rest;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCommand("avatar", "Display the avatar of a user")]
    public async Task Avatar(IUser? user = null)
    {
        RestUser? restUser = await Context.Client.Rest.GetUserAsync((user ?? Context.User).Id);

        if (restUser is null)
        {
            await RespondOrFollowUpAsync("Unknown user", ephemeral: true);
            return;
        }

        var avatarUrl = restUser.GetDisplayAvatarUrl(size: 2048)!;

        EmbedBuilder embed = new EmbedBuilder().WithColor(restUser.AccentColor ?? Color.Purple).WithAuthor(restUser.GlobalName).WithImageUrl(avatarUrl);

        await RespondOrFollowUpAsync(embeds: [embed.Build()]);
    }
}
