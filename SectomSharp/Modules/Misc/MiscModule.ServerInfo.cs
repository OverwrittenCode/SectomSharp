using Discord;
using Discord.WebSocket;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCmd("Get information about the server")]
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

        if (guild.Emotes.Count is > 0 and var emotesCount)
        {
            embedBuilder.AddField("Emojis", emotesCount, true);
        }

        if (guild.Stickers.Count is > 0 and var stickersCount)
        {
            embedBuilder.AddField("Stickers", stickersCount, true);
        }

        if (guild.TextChannels.Count is > 0 and var textChannelsCount)
        {
            embedBuilder.AddField("Text Channels", textChannelsCount, true);
        }

        if (guild.ThreadChannels.Count is > 0 and var threadChannelsCount)
        {
            embedBuilder.AddField("Thread Channels", threadChannelsCount, true);
        }

        if (guild.ForumChannels.Count is > 0 and var forumChannelsCount)
        {
            embedBuilder.AddField("Forum Channels", forumChannelsCount, true);
        }

        if (guild.MediaChannels.Count is > 0 and var mediaChannelsCount)
        {
            embedBuilder.AddField("Media Channels", mediaChannelsCount, true);
        }

        if (guild.StageChannels.Count is > 0 and var stageChannelsCount)
        {
            embedBuilder.AddField("Stage Channels", stageChannelsCount, true);
        }

        if (guild.CategoryChannels.Count is > 0 and var categoryChannelsCount)
        {
            embedBuilder.AddField("Category Channels", categoryChannelsCount, true);
        }

        await RespondOrFollowUpAsync(embeds: [embedBuilder.Build()]);
    }
}
