using System.Data;
using System.Data.Common;
using System.Diagnostics;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Extensions;
using SectomSharp.Graphics;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Leveling;

public sealed partial class LevelingModule
{
    [SlashCmd("Display the rank of a user")]
    public async Task Rank(SocketGuildUser? user = null)
    {
        if (user is null)
        {
            user = (SocketGuildUser)Context.User;
        }
        else if (user.IsBot)
        {
            await RespondAsync(NothingToView, ephemeral: true);
            return;
        }

        await DeferAsync();
        await using ApplicationDbContext db = await DbContextFactory.CreateDbContextAsync();
        await db.Database.OpenConnectionAsync();
        await using DbCommand cmd = db.Database.GetDbConnection().CreateCommand();

        cmd.CommandText = """
                          SELECT
                              get_level(u."Level_CurrentXp") AS "CurrentLevel",
                              u."Level_CurrentXp" AS "CurrentXp",
                              get_required_xp(get_level(u."Level_CurrentXp")) AS "RequiredXp",
                              (
                                  SELECT COUNT(*) + 1
                                  FROM "Users" u2
                                  WHERE u2."GuildId" = @guildId
                                    AND u2."Level_CurrentXp" > u."Level_CurrentXp"
                              ) AS "Rank"
                          FROM "Users" u
                          WHERE u."GuildId" = @guildId AND u."Id" = @userId;
                          """;

        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("guildId", Context.Guild.Id));
        cmd.Parameters.Add(NpgsqlParameterFactory.FromSnowflakeId("userId", user.Id));

        uint level;
        uint currentXp;
        uint requiredXp;
        uint rank;
        var stopwatch = Stopwatch.StartNew();
        await using (DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess | CommandBehavior.SingleRow))
        {
            if (!await reader.ReadAsync())
            {
                stopwatch.Stop();
                Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
                await RespondOrFollowupAsync(NothingToView, ephemeral: true);
                return;
            }

            level = (uint)reader.GetInt32(0);
            currentXp = (uint)reader.GetInt32(1);
            requiredXp = (uint)reader.GetInt32(2);
            rank = (uint)reader.GetInt32(3);

            stopwatch.Stop();
            Logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
        }

        var rankCardBuilder = new RankCardBuilder
        {
            User = user,
            Level = level,
            Rank = rank,
            CurrentXp = currentXp,
            RequiredXp = requiredXp
        };

        byte[] imageBytes = await rankCardBuilder.BuildAsync();
        using var stream = new MemoryStream(imageBytes);

        await FollowupWithFileAsync(stream, "RankCard.png");
    }
}
