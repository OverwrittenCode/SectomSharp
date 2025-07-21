using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Utils;
using StrongInteractions.Attributes;
using StrongInteractions.Generated;

namespace SectomSharp.Modules.Admin;

public sealed partial class AdminModule
{
    public sealed partial class ConfigModule
    {
        public sealed partial class SuggestionModule
        {
            [StrongModalInteraction]
            private async Task SuggestionPostModalResponse(ulong postChannelId, SuggestionPostModal modal)
            {
                if (Context.Guild.GetTextChannel(postChannelId) is not { } postChannel)
                {
                    await DisablePanel(Context.Interaction, "The panel has been disabled as this is no longer a valid text channel.");
                    return;
                }

                if (!Context.Guild.CurrentUser.GetPermissions(postChannel).SendMessages)
                {
                    await DisablePanelFromInsufficientPermission(Context.Interaction, postChannelId);
                    return;
                }

                await DeferAsync();

                int suggestionId;
                Stopwatch stopwatch;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    await db.Database.OpenConnectionAsync();
                    await using (DbCommand cmd = db.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = """
                                          INSERT INTO "SuggestionPosts" ("GuildId", "AuthorId")
                                          VALUES (@guildId, @authorId)
                                          RETURNING "Id"
                                          """;

                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("authorId", Context.User.Id));

                        stopwatch = Stopwatch.StartNew();
                        object? scalarResult = await cmd.ExecuteScalarAsync();
                        stopwatch.Stop();

                        suggestionId = (int)scalarResult!;
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                var author = (SocketGuildUser)Context.User;
                string url;
                try
                {
                    RestUserMessage message = await postChannel.SendMessageAsync(
                        embeds:
                        [
                            new EmbedBuilder
                            {
                                Title = modal.Title,
                                Fields =
                                [
                                    new EmbedFieldBuilder { Name = "Content", Value = modal.Description },
                                    new EmbedFieldBuilder
                                    {
                                        Name = "Status",
                                        Value = "⏳ Pending"
                                    },
                                    new EmbedFieldBuilder
                                    {
                                        Name = "Votes",
                                        Value = VoteFormatter.Empty
                                    }
                                ],
                                Color = Storage.LightGold,
                                Author = new EmbedAuthorBuilder { Name = author.DisplayName, IconUrl = author.GetDisplayAvatarUrl() },
                                Footer = new EmbedFooterBuilder { Text = $"Suggestion ID: {suggestionId} | User ID: {author.Id}" },
                                Timestamp = DateTimeOffset.Now
                            }.Build()
                        ],
                        components: new ComponentBuilder
                        {
                            ActionRows =
                            [
                                new ActionRowBuilder
                                {
                                    Components =
                                    [
                                        ButtonBuilder.CreatePrimaryButton(
                                            nameof(VoteType.Upvote),
                                            StrongInteractionIds.SuggestionPostVoteButton(suggestionId, VoteType.Upvote),
                                            ControlEmojis.Upvote
                                        ),
                                        ButtonBuilder.CreatePrimaryButton(
                                            nameof(VoteType.Downvote),
                                            StrongInteractionIds.SuggestionPostVoteButton(suggestionId, VoteType.Downvote),
                                            ControlEmojis.Downvote
                                        )
                                    ]
                                },
                                new ActionRowBuilder
                                {
                                    Components =
                                    [
                                        ButtonBuilder.CreateSuccessButton(
                                            "Approve",
                                            StrongInteractionIds.SuggestionPostVerdictButton(suggestionId, SuggestionStatus.Approved),
                                            ControlEmojis.Approve
                                        ),
                                        ButtonBuilder.CreateDangerButton(
                                            "Reject",
                                            StrongInteractionIds.SuggestionPostVerdictButton(suggestionId, SuggestionStatus.Rejected),
                                            ControlEmojis.Reject
                                        )
                                    ]
                                }
                            ]
                        }.Build()
                    );

                    url = message.GetJumpUrl();
                }
                catch (HttpException ex) when (ex.DiscordCode is DiscordErrorCode.MissingPermissions)
                {
                    await DisablePanelFromInsufficientPermission(Context.Interaction, postChannelId);
                    return;
                }

                var button = ButtonBuilder.CreateLinkButton("View your suggestion", url);
                await FollowupAsync(
                    "Suggestion sent.",
                    components: new ComponentBuilder { ActionRows = [new ActionRowBuilder { Components = [button] }] }.Build(),
                    ephemeral: true
                );
                return;

                static async Task DisablePanel(SocketInteraction interaction, string message)
                {
                    MessageComponent disabledComponents = (await interaction.GetOriginalResponseAsync()).Components.FromComponentsWithAllDisabled().Build();
                    await interaction.ModifyOriginalResponseAsync(properties =>
                        {
                            properties.Content = message;
                            properties.Components = disabledComponents;
                        }
                    );
                }

                static Task DisablePanelFromInsufficientPermission(SocketInteraction interaction, ulong postChannelId)
                    => DisablePanel(interaction, $"The panel has been disabled as I no longer have permission to send messages to <#{postChannelId}>.");
            }

            [StrongButtonInteraction]
            private async Task SuggestionPostVoteButton(int id, VoteType type)
            {
                SocketUserMessage message = ((SocketMessageComponent)Context.Interaction).Message;

                Embed embed = message.Embeds.First();
                ReadOnlySpan<char> footer = embed.Footer!.Value.Text.AsSpan();
                if (UInt64.Parse(footer[(footer.LastIndexOf(' ') + 1)..]) == Context.User.Id)
                {
                    await RespondAsync("You cannot vote yourself.", ephemeral: true);
                    return;
                }

                await DeferAsync();
                Stopwatch stopwatch;
                int upvotes = -1;
                int downvotes = -1;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    const string upvoteString = "1";
                    const string downvoteString = "-1";

                    await db.Database.OpenConnectionAsync();
                    await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();
                    cmd.CommandText = $"""
                                       WITH
                                           previous_vote AS (
                                               SELECT "Type"
                                               FROM "SuggestionVotes"
                                               WHERE
                                                   "GuildId" = @guildId
                                                   AND "UserId" = @userId
                                                   AND "SuggestionId" = @suggestionId
                                           ),
                                           upsert AS (
                                               INSERT INTO "SuggestionVotes" ("GuildId", "UserId", "SuggestionId", "Type")
                                               SELECT @guildId, @userId, @suggestionId, @newType
                                               ON CONFLICT ("GuildId", "UserId", "SuggestionId") DO UPDATE SET "Type" = EXCLUDED."Type"
                                               WHERE "SuggestionVotes"."Type" IS DISTINCT FROM EXCLUDED."Type"
                                               RETURNING "Type"
                                           ),
                                           update_counts AS (
                                               UPDATE "SuggestionPosts" p
                                               SET
                                                   "UpvoteCount" = "UpvoteCount" +
                                                       CASE
                                                           WHEN @newType = {upvoteString} AND prev."Type" IS DISTINCT FROM {upvoteString} THEN 1
                                                           WHEN @newType = {downvoteString} AND prev."Type" = {upvoteString} THEN -1
                                                           ELSE 0
                                                       END,
                                                   "DownvoteCount" = "DownvoteCount" +
                                                       CASE
                                                           WHEN @newType = {downvoteString} AND prev."Type" IS DISTINCT FROM {downvoteString} THEN 1
                                                           WHEN @newType = {upvoteString} AND prev."Type" = {downvoteString} THEN -1
                                                           ELSE 0
                                                       END
                                               FROM upsert u
                                               LEFT JOIN previous_vote prev ON TRUE
                                               WHERE
                                                   p."GuildId" = @guildId
                                                   AND p."Id" = @suggestionId
                                               RETURNING
                                                   p."UpvoteCount",
                                                   p."DownvoteCount"
                                           )
                                       SELECT * FROM update_counts
                                       """;

                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("userId", Context.User.Id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromInt32("suggestionId", id));
                    cmd.Parameters.Add(NpgsqlParameterFactory.FromEnum32("newType", type));

                    stopwatch = Stopwatch.StartNew();
                    await using (DbDataReader reader =
                                 await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleResult | CommandBehavior.CloseConnection))
                    {
                        if (await reader.ReadAsync())
                        {
                            upvotes = reader.GetInt32(0);
                            downvotes = reader.GetInt32(1);
                        }

                        stopwatch.Stop();
                    }
                }

                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);

                if (upvotes < 0)
                {
                    string text = type is VoteType.Upvote
                        ? "You have already upvoted this suggestion. You may only change your vote."
                        : "You have already downvoted this suggestion. You may only change your vote.";

                    await FollowupAsync(text, ephemeral: true);
                    return;
                }

                var embedBuilder = embed.ToEmbedBuilder();
                string voteFieldValue = VoteFormatter.FormatResults((uint)upvotes, (uint)downvotes);
                EmbedFieldBuilder voteField = embedBuilder.Fields[2];
                voteField.Value = voteFieldValue;

                Embed[] embeds = [embedBuilder.Build()];
                await message.ModifyAsync(properties => properties.Embeds = embeds);
            }

            [StrongButtonInteraction]
            private async Task SuggestionPostVerdictButton(int id, SuggestionStatus status)
            {
                if (!((SocketGuildUser)Context.User).GuildPermissions.Administrator)
                {
                    await RespondAsync("You do not have the permission to do that.", ephemeral: true);
                    return;
                }

                await DeferAsync();
                int rowsAffected;
                await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
                {
                    rowsAffected = await db.SuggestionPosts.Where(post => post.GuildId == Context.Guild.Id && post.Id == id)
                                           .ExecuteUpdateAsync(x => x.SetProperty(post => post.Status, status));
                }

                if (rowsAffected == 0)
                {
                    await FollowupAsync("Suggestion not found or already processed.", ephemeral: true);
                    return;
                }

                SocketUserMessage message = ((SocketMessageComponent)Context.Interaction).Message;
                var embedBuilder = message.Embeds.First().ToEmbedBuilder();
                EmbedFieldBuilder statusField = embedBuilder.Fields[1];
                if (status == SuggestionStatus.Approved)
                {
                    statusField.Value = $"{ControlEmojis.ApproveUnicode} Approved";
                    embedBuilder.Color = Color.Green;
                }
                else
                {
                    statusField.Value = $"{ControlEmojis.RejectUnicode} Rejected";
                    embedBuilder.Color = Color.Red;
                }

                MessageComponent disabledComponents = message.Components.FromComponentsWithAllDisabled().Build();
                Embed[] embeds = [embedBuilder.Build()];
                await message.ModifyAsync(properties =>
                    {
                        properties.Components = disabledComponents;
                        properties.Embeds = embeds;
                    }
                );
            }

            [StrongSelectMenuInteraction]
            private async Task SuggestionPanelSelectMenu(ulong postChannelId, string[] _)
            {
                await RespondWithModalAsync<SuggestionPostModal>(StrongInteractionIds.SuggestionPostModalResponse(postChannelId));
                SocketUserMessage message = ((SocketMessageComponent)Context.Interaction).Message;
                Embed[] embeds = message.Embeds.ToArray();
                await message.ModifyAsync(properties => properties.Embeds = embeds);
            }

            private static class ControlEmojis
            {
                public const string UpvoteUnicode = "👍";
                public const string DownvoteUnicode = "👎";
                public const string ApproveUnicode = "✅";
                public const string RejectUnicode = "❌";

                public static readonly Emoji Upvote = new(UpvoteUnicode);
                public static readonly Emoji Downvote = new(DownvoteUnicode);
                public static readonly Emoji Approve = new(ApproveUnicode);
                public static readonly Emoji Reject = new(RejectUnicode);
            }

            private static class VoteFormatter
            {
                private const int EmojiLength = 24;
                private const int ProgressBarLength = 14;
                private const int ProgressBarFullLength = (ProgressBarLength + 2) * EmojiLength;

                private const string LeftEmpty = "<:_:1393919948904071260>";
                private const string MiddleEmpty = "<:_:1393919993628065996>";
                private const string RightEmpty = "<:_:1393920023122415616>";

                private const string LeftFull = "<:_:1393920051593089085>";
                private const string MiddleFull = "<:_:1393920076151001108>";
                private const string RightFull = "<:_:1393920098875605032>";

                private const string AllProgressBars = Bar0 + Bar1 + Bar2 + Bar3 + Bar4 + Bar5 + Bar6 + Bar7 + Bar8 + Bar9 + Bar10 + Bar11 + Bar12 + Bar13 + Bar14;

                private const string Bar0 = LeftEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar1 = LeftFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar2 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar3 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar4 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar5 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar6 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar7 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar8 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar9 = LeftFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleFull
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + MiddleEmpty
                                          + RightEmpty;

                private const string Bar10 = LeftFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + RightEmpty;

                private const string Bar11 = LeftFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + RightEmpty;

                private const string Bar12 = LeftFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleEmpty
                                           + MiddleEmpty
                                           + RightEmpty;

                private const string Bar13 = LeftFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleEmpty
                                           + RightEmpty;

                private const string Bar14 = LeftFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + MiddleFull
                                           + RightFull;

                public const string Empty = $"""
                                             {ControlEmojis.UpvoteUnicode} 0 upvotes (0.0%) • {ControlEmojis.DownvoteUnicode} 0 downvotes (0.0%)
                                             {Bar0}
                                             """;

                public static unsafe string FormatResults(uint upvotes, uint downvotes)
                {
                    double totalVotes = upvotes + downvotes;
                    if (totalVotes == 0)
                    {
                        return Empty;
                    }

                    const int upvoteEmojiLength = 2;    // ControlEmojis.UpvoteUnicode.Length;
                    const int spaceLength = 1;          // " ".Length
                    const int maxVoteDigits = 7;        //"2500000".Length;
                    const int upvoteLabelLength = 10;   // " upvotes (".Length; 
                    const int maxPercentLength = 5;     // "100.0".Length;
                    const int percentSuffixLength = 2;  // "%)".Length;       
                    const int separatorLength = 3;      // " • ".Length;          
                    const int downvoteEmojiLength = 2;  // ControlEmojis.DownvoteUnicode.Length;
                    const int downvoteLabelLength = 12; // " downvotes (".Length;

                    const int firstLineMaxLength = upvoteEmojiLength
                                                 + spaceLength
                                                 + maxVoteDigits
                                                 + upvoteLabelLength
                                                 + maxPercentLength
                                                 + percentSuffixLength
                                                 + separatorLength
                                                 + downvoteEmojiLength
                                                 + spaceLength
                                                 + maxVoteDigits
                                                 + downvoteLabelLength
                                                 + maxPercentLength
                                                 + percentSuffixLength;

                    const int lineBreakLength = 1; // "\n".Length
                    const int totalMaxLength = firstLineMaxLength + lineBreakLength + ProgressBarFullLength;

                    double upPercent = upvotes / totalVotes * 100;
                    double downPercent = downvotes / totalVotes * 100;
                    int filled = Math.Clamp((int)Math.Round(upvotes / totalVotes * ProgressBarLength), 0, ProgressBarLength);

                    string formatResults = String.Create(totalMaxLength, (upvotes, downvotes, upPercent, downPercent, filled), Action);
                    return formatResults;

                    static void Action(Span<char> span, (uint upvotes, uint downvotes, double upPercent, double downPercent, int filled) state)
                    {
                        (uint upvotes, uint downvotes, double upPercent, double downPercent, int filled) = state;
                        int pos = 0;

                        $"{ControlEmojis.UpvoteUnicode} ".AsSpan().CopyTo(span);
                        pos += upvoteEmojiLength + spaceLength;

                        upvotes.TryFormat(span[pos..], out int upvoteLen);
                        pos += upvoteLen;

                        " upvotes (".CopyTo(span[pos..]);
                        pos += upvoteLabelLength;

                        upPercent.TryFormat(span[pos..], out int upPercentLen, "F1");
                        pos += upPercentLen;

                        $"%) • {ControlEmojis.DownvoteUnicode} ".CopyTo(span[pos..]);
                        pos += percentSuffixLength + separatorLength + downvoteEmojiLength + spaceLength;

                        downvotes.TryFormat(span[pos..], out int downvoteLen);
                        pos += downvoteLen;

                        " downvotes (".CopyTo(span[pos..]);
                        pos += downvoteLabelLength;

                        downPercent.TryFormat(span[pos..], out int downPercentLen, "F1");
                        pos += downPercentLen;

                        "%)\n".CopyTo(span[pos..]);
                        pos += percentSuffixLength + lineBreakLength;

                        fixed (char* srcPtr = AllProgressBars)
                        fixed (char* dstPtr = &MemoryMarshal.GetReference(span))
                        {
                            char* srcOffset = srcPtr + filled * ProgressBarFullLength;
                            char* dstOffset = dstPtr + pos;
                            Buffer.MemoryCopy(srcOffset, dstOffset, (span.Length - pos) * sizeof(char), ProgressBarFullLength * sizeof(char));
                        }
                    }
                }
            }

            public class SuggestionPostModal : IModal
            {
                /// <inheritdoc />
                public string Title => "Suggestion Post";

                [InputLabel("What is your suggestion??")]
                [ModalTextInput(nameof(Description), TextInputStyle.Paragraph, "Enter some text here", 32, 512)]
                public required string Description { get; set; }
            }
        }
    }
}
