using Discord;
using Discord.Interactions;

namespace SectomSharp.Attributes;

/// <summary>
///     Enforce a rate limit on a module or command by <see cref="InteractionType" />.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
internal sealed class RateLimitAttribute : PreconditionAttribute
{
    private const int DefaultSeconds = 3;

    private readonly InteractionType _interactionType;
    private readonly TimeSpan _rateLimit;

    /// <summary>
    ///     A dictionary storing user ids with their corresponding command rate limits.
    /// </summary>
    /// <remarks>
    ///     Each user ID maps to another dictionary that maps <see cref="ICommandInfo.Name" /> to a <see cref="DateTime" />.
    /// </remarks>
    private readonly Dictionary<ulong, Dictionary<string, DateTime>> _rateLimits = [];

    /// <summary>
    ///     Extend a module or command by enforcing a rate limit on executing commands.
    /// </summary>
    /// <param name="interactionType">The type of interaction this rate limit applies to.</param>
    /// <param name="seconds">The duration in seconds to wait between command execution.</param>
    public RateLimitAttribute(InteractionType interactionType = InteractionType.ApplicationCommand, int seconds = DefaultSeconds)
    {
        _interactionType = interactionType;
        _rateLimit = TimeSpan.FromSeconds(seconds);
    }

    /// <inheritdoc />
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        if (context.Interaction.Type != _interactionType)
        {
            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        ulong userId = context.User.Id;
        string commandName = commandInfo.Name;

        if (!_rateLimits.TryGetValue(userId, out Dictionary<string, DateTime>? userRateLimits))
        {
            _rateLimits[userId] = new Dictionary<string, DateTime>
            {
                [commandName] = new()
            };

            return Task.FromResult(PreconditionResult.FromSuccess());
        }

        if (userRateLimits.TryGetValue(commandName, out DateTime lastExecutedAt))
        {
            TimeSpan timeDifference = DateTime.UtcNow - lastExecutedAt;

            if (timeDifference < _rateLimit)
            {
                int remainingSeconds = (_rateLimit - timeDifference).Seconds;

                string message = remainingSeconds == 0
                    ? "You are sending requests too fast!"
                    : $"Cooldown: {remainingSeconds} {(remainingSeconds == 1 ? "second" : "seconds")} remaining";

                return Task.FromResult(PreconditionResult.FromError(message));
            }
        }

        userRateLimits[commandName] = DateTime.UtcNow;

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
