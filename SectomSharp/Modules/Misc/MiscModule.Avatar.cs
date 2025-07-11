using Discord;
using Discord.Rest;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCmd("Display the avatar of a user")]
    public async Task Avatar(IUser? user = null)
    {
        RestUser? restUser = await Context.Client.Rest.GetUserAsync((user ?? Context.User).Id);

        if (restUser is null)
        {
            await RespondAsync("Unknown user", ephemeral: true);
            return;
        }

        await RespondAsync(
            embeds:
            [
                new EmbedBuilder
                {
                    Color = restUser.AccentColor ?? Color.Purple,
                    Author = new EmbedAuthorBuilder { Name = restUser.GlobalName },
                    ImageUrl = restUser.GetDisplayAvatarUrl(size: 2048)
                }.Build()
            ]
        );
    }
}
