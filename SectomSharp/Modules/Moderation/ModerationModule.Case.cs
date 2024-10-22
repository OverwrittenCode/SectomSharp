using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using SectomSharp.Data;
using SectomSharp.Data.Enums;
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

            using var dbContext = new ApplicationDbContext();

            var @case = await dbContext.Cases.FindAsync(id, Context.Guild.Id);

            if (@case is null)
            {
                await RespondOrFollowUpAsync("Invalid case id provided.");
                return;
            }

            dbContext.Entry(@case).State = EntityState.Detached;

            CaseService.GenerateLogEmbeds(@case, out var serverLogEmbed, out var _);

            await RespondOrFollowUpAsync(
                embeds: [serverLogEmbed],
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

            using var dbContext = new ApplicationDbContext();

            var query = dbContext
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
                    @case.Id,
                    @case.LogType,
                    @case.OperationType,
                    @case.CreatedAt,
                })
                .ToListAsync();

            var embedDescriptions = cases
                .Select(@case =>
                    $"{Format.Code(@case.Id)} {Format.Bold($"[{@case.LogType}{@case.OperationType}]")} {TimestampTag.FormatFromDateTime(@case.CreatedAt, TimestampTagStyles.Relative)}"
                )
                .ToList();

            var embeds = ButtonPaginationManager.GetEmbeds(
                embedDescriptions,
                $"{Context.Guild.Name} Cases ({cases.Count})"
            );

            var pagination = new ButtonPaginationBuilder() { Embeds = [.. embeds] };

            await pagination.Build().Init(Context);
        }

        [SlashCommand("amend-reason", "Amend the reason of a specific case on the server")]
        public async Task AmendReason(
            [MinLength(CaseService.IdLength)] [MaxLength(CaseService.IdLength)] string id,
            [MaxLength(CaseService.MaxReasonLength)] string newReason
        )
        {
            await DeferAsync();

            using var dbContext = new ApplicationDbContext();

            var @case = await dbContext
                .Cases.Where(@case => @case.Id == id && @case.GuildId == Context.Guild.Id)
                .FirstOrDefaultAsync();

            if (@case is null)
            {
                await RespondOrFollowUpAsync("Invalid case id provided.");
                return;
            }

            if (@case.PerpetratorId != Context.User.Id)
            {
                await RespondOrFollowUpAsync("You may only amend cases actioned by yourself.");
                return;
            }

            @case.Reason = newReason;

            if (@case.LogMessageId is null)
            {
                // TODOs:
                // - setup logging system
                // - check if guild has a log channel for the action and send then send embed in the channel
            }

            await RespondOrFollowUpAsync(
                "Case reason has been amended.",
                components: CaseService.GenerateLogMessageButton(@case)
            );

            await dbContext.SaveChangesAsync();
        }
    }
}
