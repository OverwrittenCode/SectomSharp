using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Attributes;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Data.Enums;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Builders;
using SectomSharp.Managers.Pagination.Button;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Moderation;

public sealed partial class ModerationModule
{
    [Group("case", "Container of all cases in the server")]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public sealed class CaseModule : BaseModule<CaseModule>
    {
        /// <inheritdoc />
        public CaseModule(ILogger<CaseModule> logger) : base(logger) { }

        [SlashCmd("View a specific case on the server")]
        public async Task View([MinLength(CaseConfiguration.IdLength)] [MaxLength(CaseConfiguration.IdLength)] string id)
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

            await RespondOrFollowUpAsync(embeds: [@case.CommandInputEmbedBuilder.Build()], components: CaseUtils.GenerateLogMessageButton(@case));
        }

        [SlashCmd("List and filter all cases on the server")]
        public async Task List(IUser? target = null, IUser? perpetrator = null, IChannel? channel = null, BotLogType? logType = null, OperationType? operationType = null)
        {
            await DeferAsync();

            await using var dbContext = new ApplicationDbContext();
            IQueryable<Case> query = dbContext.Cases.Where(@case => @case.GuildId == Context.Guild.Id).AsNoTracking();

            if (target?.Id is { } targetId)
            {
                query = query.Where(@case => @case.TargetId == targetId);
            }

            if (perpetrator?.Id is { } perpetratorId)
            {
                query = query.Where(@case => @case.PerpetratorId == perpetratorId);
            }

            if (channel?.Id is { } channelId)
            {
                query = query.Where(@case => @case.ChannelId == channelId);
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

            var cases = await query.Select(@case => new
                                        {
                                            @case.Id,
                                            @case.LogType,
                                            @case.OperationType,
                                            @case.CreatedAt
                                        }
                                    )
                                   .ToListAsync();

            if (cases.Count == 0)
            {
                await RespondOrFollowUpAsync("No cases found.");
                return;
            }

            List<string> embedDescriptions = cases.Select(@case
                                                       => $"{Format.Code(@case.Id)} {Format.Bold($"[{@case.LogType}{@case.OperationType}]")} {@case.CreatedAt.GetRelativeTimestamp()}"
                                                   )
                                                  .ToList();

            Embed[] embeds = ButtonPaginationManager.GetEmbeds(embedDescriptions, $"{Context.Guild.Name} Cases ({cases.Count})");

            var pagination = new ButtonPaginationBuilder
            {
                Embeds = [.. embeds]
            };

            await pagination.Build().Init(Context);
        }
    }
}
