using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Extensions;

namespace SectomSharp.Modules;

/// <inheritdoc />
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithInheritors)]
public abstract class BaseModule<TThis> : InteractionModuleBase<SocketInteractionContext>
    where TThis : BaseModule<TThis>
{
    protected const string TimespanDescription = "Allowed formats: 4d3h2m1s, 4d3h, 3h2m1s, 3h1s, 2m, 20s (d=days, h=hours, m=minutes, s=seconds)";
    protected const string NothingToView = "Nothing to view yet.";

    private readonly ILogger<BaseModule<TThis>> _logger;
    private string _source = null!;

    /// <summary>
    ///     Gets the db factory.
    /// </summary>
    protected IDbContextFactory<ApplicationDbContext> DbContextFactory { get; }

    /// <summary>
    ///     Creates a new instance of the class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="dbContextFactory">The factory for creating <see cref="ApplicationDbContext" /> instances.</param>
    protected BaseModule(ILogger<BaseModule<TThis>> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory)
    {
        _logger = logger;
        DbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="DiscordExtensions.RespondOrFollowupAsync" />
    protected async Task RespondOrFollowupAsync(
        string? text = null,
        Embed[]? embeds = null,
        bool ephemeral = false,
        AllowedMentions? allowedMentions = null,
        MessageComponent? components = null,
        RequestOptions? options = null,
        PollProperties? poll = null
    )
        => await Context.Interaction.RespondOrFollowupAsync(text, embeds, ephemeral, allowedMentions, components, options, poll);

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
