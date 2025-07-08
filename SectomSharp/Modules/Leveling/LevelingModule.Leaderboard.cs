using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Extensions;
using SectomSharp.Graphics;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Leveling;

public sealed partial class LevelingModule
{
    [SlashCmd("Displays the level xp leaderboard")]
    public async Task Leaderboard()
    {
        await DeferAsync();
        Task<LeaderboardPlayer>[] tasks = [];
        int i = 0;
        bool hasRows;
        Stopwatch stopwatch;
        await using (ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync())
        {
            await db.Database.OpenConnectionAsync();
            await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

            cmd.CommandText = """
                              SELECT
                                  u."Id" AS "UserId",
                                  get_level(u."Level_CurrentXp") AS "CurrentLevel",
                                  u."Level_CurrentXp" AS "CurrentXp"
                              FROM "Users" u
                              WHERE u."GuildId" = @guildId
                                AND u."Level_CurrentXp" > 0
                              ORDER BY u."Level_CurrentXp" DESC
                              LIMIT 5;
                              """;

            cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));

            stopwatch = Stopwatch.StartNew();
            await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.CloseConnection))
            {
                hasRows = await reader.ReadAsync();
                if (hasRows)
                {
                    tasks = new Task<LeaderboardPlayer>[LeaderboardPlayers.Length];
                    do
                    {
                        ulong userId = (ulong)reader.GetInt64(0);
                        uint level = (uint)reader.GetInt32(1);
                        uint xp = (uint)reader.GetInt32(2);

                        tasks[i++] = GetLeaderboardPlayerAsync(Context, userId, level, xp);
                    } while (await reader.ReadAsync());
                }

                stopwatch.Stop();
            }
        }

        Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
        if (!hasRows)
        {
            await FollowupAsync(NothingToView, ephemeral: true);
            return;
        }

        for (; i < tasks.Length; i++)
        {
            tasks[i] = Task.FromResult(LeaderboardPlayer.Unknown);
        }

        LeaderboardPlayer[] players = await Task.WhenAll(tasks);
        var leaderboardPlayers = new LeaderboardPlayers();

        for (i = 0; i < players.Length; i++)
        {
            leaderboardPlayers[i] = players[i];
        }

        var leaderboard = new LeaderboardBuilder
        {
            Guild = Context.Guild,
            Players = leaderboardPlayers
        };

        byte[] imageBytes = await leaderboard.BuildAsync();
        using var stream = new MemoryStream(imageBytes);
        await FollowupWithFileAsync(stream, "LeaderboardCard.png");
        return;

        static async Task<LeaderboardPlayer> GetLeaderboardPlayerAsync(SocketInteractionContext context, ulong userId, uint level, uint xp)
            => (context.Guild.GetUser(userId) ?? (IGuildUser?)await context.Client.Rest.GetGuildUserAsync(context.Guild.Id, userId)) is { } user
                ? new LeaderboardPlayer
                {
                    DisplayName = user.DisplayName,
                    Username = user.Username,
                    Level = level,
                    Xp = xp,
                    AvatarUrl = user.GetDisplayAvatarUrl(ImageFormat.Png, 4096)
                }
                : LeaderboardPlayer.Unknown;
    }
}
