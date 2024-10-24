using Discord;
using Discord.Interactions;

namespace SectomSharp.Attributes;

internal sealed record class RateLimitLog
{
    public DateTime LastExecutedAt { get; set; }
    public ICommandInfo CommandInfo { get; init; }

    public RateLimitLog(ICommandInfo commandInfo)
    {
        CommandInfo = commandInfo;
        LastExecutedAt = DateTime.UtcNow;
    }
}

/// <summary>
///     Enforce a rate limit on a module or command by <see cref="InteractionType"/>.
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true
)]
internal sealed class RateLimitAttribute : PreconditionAttribute
{
    private const int DefaultSeconds = 3;

    /// <summary>
    ///     A dictionary storing user ids with their corresponding command rate limits.
    /// </summary>
    /// <remarks>
    ///     Each user ID maps to another dictionary that maps <see cref="ICommandInfo.Name"/>
    ///     to <see cref="RateLimitLog"/>.
    /// </remarks>
    private readonly Dictionary<ulong, Dictionary<string, RateLimitLog>> _rateLimits = [];
    private readonly InteractionType _interactionType;
    private readonly TimeSpan _rateLimit;

    /// <summary>
    ///     Extend a module or command by enforcing a rate limit on executing commands.
    /// </summary>
    /// <param name="interactionType">The type of interaction this rate limit applies to</param>
    /// <param name="seconds">The duration in seconds to wait between command execution.</param>
    public RateLimitAttribute(
        InteractionType interactionType = InteractionType.ApplicationCommand,
        int seconds = DefaultSeconds
    )
    {
        _interactionType = interactionType;
        _rateLimit = TimeSpan.FromSeconds(seconds);
    }

    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services
    )
    {
        if (context.Interaction.Type != _interactionType)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        var userId = context.User.Id;
        var commandName = commandInfo.Name;

        if (!_rateLimits.TryGetValue(userId, out var userRateLimits))
        {
            _rateLimits[userId] = new Dictionary<string, RateLimitLog>
            {
                { commandName, new RateLimitLog(commandInfo) },
            };

            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        if (userRateLimits.TryGetValue(commandName, out var rateLimitLog))
        {
            var timeDifference = DateTime.UtcNow - rateLimitLog.LastExecutedAt;

            if (timeDifference < _rateLimit)
            {
                var remainingSeconds = (_rateLimit - timeDifference).Seconds;

                var message =
                    remainingSeconds == 0
                        ? "You are sending requests to fast!"
                        : $"Cooldown: {remainingSeconds} {(remainingSeconds == 1 ? "second" : "seconds")} remaining";

                return Task.FromResult(PreconditionResult.FromError(message));
            }

            rateLimitLog.LastExecutedAt = DateTime.UtcNow;
        }
        else
        {
            userRateLimits[commandName] = new RateLimitLog(commandInfo);
        }

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
