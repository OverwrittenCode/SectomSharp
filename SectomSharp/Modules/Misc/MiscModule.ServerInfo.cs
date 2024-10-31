using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCommand("serverinfo", "Get information about the server")]
    public async Task ServerInfo()
    {
        SocketGuild guild = Context.Guild;
        EmbedBuilder embedBuilder = new EmbedBuilder().WithAuthor(guild.Name, guild.IconUrl)
                                                      .WithThumbnailUrl(guild.IconUrl)
                                                      .WithColor(Color.Purple)
                                                      .WithFooter($"ID: {guild.Id} | Created At")
                                                      .WithTimestamp(guild.CreatedAt)
                                                      .AddField("Owner", guild.Owner.Mention, true)
                                                      .AddField("Roles", guild.Roles.Count, true)
                                                      .AddField("Members", guild.MemberCount, true);
        if (guild.Emotes.Count > 0)
        {
            embedBuilder.AddField("Emojis", guild.Emotes.Count, true);
        }

        if (guild.Stickers.Count > 0)
        {
            embedBuilder.AddField("Stickers", guild.Stickers.Count, true);
        }

        if (guild.TextChannels.Count > 0)
        {
            embedBuilder.AddField("Text Channels", guild.TextChannels.Count, true);
        }

        if (guild.ThreadChannels.Count > 0)
        {
            embedBuilder.AddField("Thread Channels", guild.ThreadChannels.Count, true);
        }

        if (guild.ForumChannels.Count > 0)
        {
            embedBuilder.AddField("Forum Channels", guild.ForumChannels.Count, true);
        }

        if (guild.MediaChannels.Count > 0)
        {
            embedBuilder.AddField("Media Channels", guild.MediaChannels.Count, true);
        }

        if (guild.StageChannels.Count > 0)
        {
            embedBuilder.AddField("Stage Channels", guild.StageChannels.Count, true);
        }

        if (guild.CategoryChannels.Count > 0)
        {
            embedBuilder.AddField("Category Channels", guild.CategoryChannels.Count, true);
        }

        await RespondOrFollowUpAsync(embeds: [embedBuilder.Build()]);
    }
}
