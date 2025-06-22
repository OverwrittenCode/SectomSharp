using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Data.Entities;
using SectomSharp.Extensions;

namespace SectomSharp.Modules;

/// <inheritdoc />
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
public abstract class BaseModule<TThis> : InteractionModuleBase<SocketInteractionContext>
    where TThis : BaseModule<TThis>
{
    private protected const string TimespanDescription = "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)";

    private readonly ILogger<BaseModule<TThis>> _logger;
    private string _source = null!;

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    protected BaseModule(ILogger<BaseModule<TThis>> logger) => _logger = logger;

    /// <inheritdoc cref="DiscordExtensions.RespondOrFollowupAsync" />
    protected async Task RespondOrFollowUpAsync(
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    )
        => await Context.Interaction.RespondOrFollowupAsync(text, embeds, ephemeral, allowedMentions, components, options, poll);

    protected async Task<Guild> EnsureGuildAsync(ApplicationDbContext db)
    {
        Guild? guild = await db.Guilds.FindAsync(Context.Guild.Id);
        if (guild != null)
        {
            return guild;
        }

        guild = new Guild
        {
            Id = Context.Guild.Id
        };

        db.Guilds.Add(guild);
        return guild;
    }

    public override void BeforeExecute(ICommandInfo command)
    {
        _source = command.MethodName;
        base.BeforeExecute(command);
    }

    public void LogError(string message, Exception? ex = null) => _logger.LogError(ex, "[{Source}] {Message}", _source, message);

    public void LogWarning(string message, Exception? ex = null) => _logger.LogWarning(ex, "[{Source}] {Message}", _source, message);

    public void LogInfo(string message, Exception? ex = null) => _logger.LogInformation(ex, "[{Source}] {Message}", _source, message);

    public void LogVerbose(string message, Exception? ex = null) => _logger.LogTrace(ex, "[{Source}] {Message}", _source, message);

    public void LogDebug(string message, Exception? ex = null) => _logger.LogDebug(ex, "[{Source}] {Message}", _source, message);
}
