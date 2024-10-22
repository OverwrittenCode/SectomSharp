using Discord;
using Discord.Interactions;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCommand("avatar", "Display the avatar of a user")]
    public async Task Avatar(IUser? user = null)
    {
        const int Size = 2048;

        var restUser = await Context.Client.Rest.GetUserAsync((user ?? Context.User).Id);

        string avatarUrl = restUser.GetDisplayAvatarUrl(size: Size);

        var embed = new EmbedBuilder()
            .WithColor(restUser.AccentColor ?? Color.Purple)
            .WithAuthor(restUser.GlobalName)
            .WithImageUrl(avatarUrl);

        await RespondOrFollowUpAsync(embeds: [embed.Build()]);
    }
}
