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

        AddInlineFieldEntryIfNotEmpty(guild.Emotes, "Emojis");
        AddInlineFieldEntryIfNotEmpty(guild.Stickers, "Stickers");
        AddInlineFieldEntryIfNotEmpty(guild.TextChannels, "Text Channels");
        AddInlineFieldEntryIfNotEmpty(guild.ThreadChannels, "Thread Channels");
        AddInlineFieldEntryIfNotEmpty(guild.ForumChannels, "Forum Channels");
        AddInlineFieldEntryIfNotEmpty(guild.MediaChannels, "Media Channels");
        AddInlineFieldEntryIfNotEmpty(guild.StageChannels, "Stage Channels");
        AddInlineFieldEntryIfNotEmpty(guild.CategoryChannels, "Category Channels");

        await RespondAsync(embeds: [embedBuilder.Build()]);
        return;

        void AddInlineFieldEntryIfNotEmpty<T>(IReadOnlyCollection<T> collection, string fieldName)
        {
            if (collection.Count is > 0 and var count)
            {
                embedBuilder.AddField(fieldName, count, true);
            }
        }
    }
}
