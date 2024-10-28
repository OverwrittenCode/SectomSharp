using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Models;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Services;

namespace SectomSharp.Modules.Moderation;

public partial class ModerationModule
{
    [Group("case", "Container of all cases in the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public sealed class CaseModule : BaseModule
    {
        [SlashCommand("view", "View a specific case on the server")]
        public async Task View(
            [MinLength(CaseService.IdLength)] [MaxLength(CaseService.IdLength)] string id
        )
        {
            await DeferAsync();

            await using var dbContext = new ApplicationDbContext();

            Case? @case = await dbContext.Cases.FindAsync(id, Context.Guild.Id);

            if (@case is null)
            {
                await RespondOrFollowUpAsync("Invalid case id provided.");
                return;
            }

            dbContext.Entry(@case).State = EntityState.Detached;

            (_, EmbedBuilder serverLog, EmbedBuilder commandLog) = CaseService.GenerateLogEmbeds(@case);

            await RespondOrFollowUpAsync(
                embeds: [serverLog.Build(), commandLog.Build()],
                components: CaseService.GenerateLogMessageButton(@case)
            );
        }

        [SlashCommand("list", "List and filter all cases on the server")]
        public async Task List(
            IUser? target = null,
            IUser? perpetrator = null,
            IChannel? channel = null,
            BotLogType? logType = null,
            OperationType? operationType = null
        )
        {
            await DeferAsync();

            await using var dbContext = new ApplicationDbContext();

            IQueryable<Case> query = dbContext
                .Cases.Where(@case => @case.GuildId == Context.Guild.Id)
                .AsNoTracking();

            if (target is not null)
            {
                query = query.Where(@case => @case.TargetId == target.Id);
            }

            if (perpetrator is not null)
            {
                query = query.Where(@case => @case.PerpetratorId == perpetrator.Id);
            }

            if (channel is not null)
            {
                query = query.Where(@case => @case.ChannelId == channel.Id);
            }

            if (logType is not null)
            {
                query = query.Where(@case => @case.LogType == logType);
            }

            if (operationType is not null)
            {
                query = query.Where(@case => @case.OperationType == operationType);
            }

            query = query.OrderByDescending(@case => @case.CreatedAt);

            var cases = await query
                .Select(@case => new
                {
                    @case.Id, @case.LogType, @case.OperationType, @case.CreatedAt
                })
                .ToListAsync();

            if (cases.Count == 0)
            {
                await RespondOrFollowUpAsync("No cases found.");
                return;
            }

            List<string> embedDescriptions = cases
                .Select(@case =>
                    $"{Format.Code(@case.Id)} {Format.Bold($"[{@case.LogType}{@case.OperationType}]")} {TimestampTag.FormatFromDateTime(@case.CreatedAt, TimestampTagStyles.Relative)}"
                )
                .ToList();

            Embed[] embeds = ButtonPaginationManager.GetEmbeds(
                embedDescriptions,
                $"{Context.Guild.Name} Cases ({cases.Count})"
            );

            ButtonPaginationBuilder pagination = new()
            {
                Embeds = [.. embeds]
            };

            await pagination.Build().Init(Context);
        }
    }
}
