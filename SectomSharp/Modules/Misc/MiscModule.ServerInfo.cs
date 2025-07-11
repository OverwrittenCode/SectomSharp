using Discord;
using Discord.WebSocket;
using SectomSharp.Attributes;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Misc;

public sealed partial class MiscModule
{
    [SlashCmd("Get information about the server")]
    public async Task ServerInfo()
    {
        SocketGuild guild = Context.Guild;
        var fields = new List<EmbedFieldBuilder>(10)
        {
            EmbedFieldBuilderFactory.CreateInlined("Owner", guild.Owner.Mention),
            EmbedFieldBuilderFactory.CreateInlined("Roles", guild.Roles.Count),
            EmbedFieldBuilderFactory.CreateInlined("Members", guild.MemberCount)
        };

        AddInlineFieldEntryIfNotEmpty(guild.Emotes, "Emojis");
        AddInlineFieldEntryIfNotEmpty(guild.Stickers, "Stickers");
        AddInlineFieldEntryIfNotEmpty(guild.TextChannels, "Text Channels");
        AddInlineFieldEntryIfNotEmpty(guild.ThreadChannels, "Thread Channels");
        AddInlineFieldEntryIfNotEmpty(guild.ForumChannels, "Forum Channels");
        AddInlineFieldEntryIfNotEmpty(guild.MediaChannels, "Media Channels");
        AddInlineFieldEntryIfNotEmpty(guild.StageChannels, "Stage Channels");
        AddInlineFieldEntryIfNotEmpty(guild.CategoryChannels, "Category Channels");

        await RespondAsync(
            embeds:
            [
                new EmbedBuilder
                {
                    Author = new EmbedAuthorBuilder
                    {
                        Name = guild.Name,
                        IconUrl = guild.IconUrl
                    },
                    ThumbnailUrl = guild.IconUrl,
                    Color = Color.Purple,
                    Footer = new EmbedFooterBuilder { Text = $"ID: {guild.Id} | Created At" },
                    Timestamp = guild.CreatedAt,
                    Fields = fields
                }.Build()
            ]
        );
        return;

        void AddInlineFieldEntryIfNotEmpty<T>(IReadOnlyCollection<T> collection, string fieldName)
        {
            if (collection.Count is > 0 and var count)
            {
                fields.Add(EmbedFieldBuilderFactory.CreateInlined(fieldName, count));
            }
        }
    }
}
