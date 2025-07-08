using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Managers;

/// <summary>
///     Provides base functionality for managing instances of a specific type with unique identifiers.
/// </summary>
/// <typeparam name="T">The type of instance being managed. Must inherit from <see cref="InstanceManager{T}" />.</typeparam>
[SuppressMessage("ReSharper", "StaticMemberInGenericType")]
internal abstract class InstanceManager<T>
    where T : InstanceManager<T>
{
    private const int Available = 0;
    private const int InSession = 1;
    private const int CleanupInProgress = 2;

    private const string SessionExpiredMessage = "This session has expired.";
    private const string SessionUnavailableMessage = "This session is currently unavailable. Please try again shortly.";
    private const string SessionNotForYouMessage = "This session is not for you.";

    private static readonly string Name = typeof(T).Name;

    private static readonly Func<ILogger, ulong, TimeSpan, IDisposable?> ScopeCallback = LoggerMessage.DefineScope<ulong, TimeSpan>("Instance Id={InstanceId}, Timeout={Timeout}");

    /// <summary>
    ///     A dictionary storing all active instances of <typeparamref name="T" /> keyed by <see cref="_interactionId" />.
    /// </summary>
    private static readonly ConcurrentDictionary<ulong, T> Instances = [];

    /// <summary>
    ///     Starts a task that executes <see cref="DisableMessageComponentsAsync" />.
    /// </summary>
    /// <param name="instance">The manager instance.</param>
    private static void RunOnTimeout(InstanceManager<T> instance)
        => _ = Task.Factory.StartNew(
                        static async state =>
                        {
                            var instance = (InstanceManager<T>)state!;
                            await instance.DisableMessageComponentsAsync();
                        },
                        instance,
                        CancellationToken.None,
                        TaskCreationOptions.DenyChildAttach,
                        TaskScheduler.Default
                    )
                   .Unwrap();

    /// <summary>
    ///     Attempts to get an instance by id, defer the provided interaction context, and
    ///     transition the instance state from <see cref="Available" /> to <see cref="InSession" />.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    /// <param name="id">The id.</param>
    /// <returns><c>true</c> if the instance was found and ownership was successfully acquired; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     Ownership is represented by a lock flag that prevents concurrent access.
    ///     The caller must invoke <see cref="TryComplete" /> or <see cref="TryReleaseSession" />
    ///     to avoid deadlocks or resource leaks.
    /// </remarks>
    [MustUseReturnValue("You must check the return value and return early if null")]
    protected static async Task<T?> TryAcquireSessionAndDeferAsync(SocketMessageComponent context, ulong id)
    {
        if (!Instances.TryGetValue(id, out T? instance))
        {
            await context.RespondAsync(SessionExpiredMessage, ephemeral: true);
            return null;
        }

        if (!instance.IsEphemeral && context.User.Id != instance._userId)
        {
            await context.RespondAsync(SessionNotForYouMessage, ephemeral: true);
            return null;
        }

        if (!instance.TryAcquireSession())
        {
            await context.FollowupAsync(SessionUnavailableMessage, ephemeral: true);
            return null;
        }

        await context.DeferAsync(instance.IsEphemeral);
        return instance;
    }

    private readonly ILogger<T> _logger;
    private readonly ulong _interactionId;
    private readonly ulong _userId;

    private int _state;
    private Timer? _inactivityTimer;
    private Timer? _hardTimer;
    private CancellationTokenSource? _inactivityCts;
    private CancellationTokenSource? _apiCts;
    private RestFollowupMessage _originalMessage = null!;

    /// <summary>
    ///     Gets the time interval to wait before the instance is automatically freed due to user inactivity.
    /// </summary>
    [PublicAPI]
    public TimeSpan InactivityTimeout { get; init; } = TimeSpan.FromMinutes(3);

    /// <summary>
    ///     Gets the time interval to wait before the instance is automatically freed, regardless of activity.
    /// </summary>
    [PublicAPI]
    public TimeSpan HardTimeout { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     Gets whether the interactions should be ephemeral.
    /// </summary>
    public bool IsEphemeral { get; init; }

    /// <summary>
    ///     Initialises a new instance of the InstanceManager class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="context">The context used for the <see cref="_interactionId" />.</param>
    protected InstanceManager(ILoggerFactory loggerFactory, SocketInteractionContext context)
    {
        _logger = loggerFactory.CreateLogger<T>();
        _interactionId = context.Interaction.Id;
        _userId = context.User.Id;
    }

    /// <summary>
    ///     Attempts to transition <see cref="_state" /> from <paramref name="currentState" /> to <paramref name="newState" />, atomically.
    /// </summary>
    /// <param name="currentState">The expected current state.</param>
    /// <param name="newState">The desired new state.</param>
    /// <returns><c>true</c> if the transition succeeded; otherwise, <c>false</c>.</returns>
    private bool TryTransitionState(int currentState, int newState) => Interlocked.CompareExchange(ref _state, newState, currentState) == currentState;

    /// <summary>
    ///     Sets <see cref="_state" /> to <paramref name="newState" />, unconditionally and atomically.
    /// </summary>
    /// <param name="newState">The new state to set.</param>
    /// <returns>The previous state before the transition.</returns>
    private int ForceTransitionState(int newState) => Interlocked.Exchange(ref _state, newState);

    /// <summary>
    ///     Checks whether <see cref="_state" /> is equal to <paramref name="state" />, atomically.
    /// </summary>
    /// <param name="state">The state to compare against.</param>
    /// <returns><c>true</c> if the current state matches; otherwise, <c>false</c>.</returns>
    private bool IsInState(int state) => Interlocked.CompareExchange(ref _state, state, state) == state;

    /// <summary>
    ///     Creates a scope that attaches metadata to any logs.
    /// </summary>
    /// <returns>The scope.</returns>
    [MustDisposeResource]
    private IDisposable? CreateScope() => ScopeCallback(_logger, _interactionId, InactivityTimeout);

    /// <summary>
    ///     Cleans up resources.
    /// </summary>
    private void Cleanup()
    {
        CancellationTokenSource? apiCts = Interlocked.Exchange(ref _apiCts, null);
        if (apiCts is not null)
        {
            apiCts.Cancel();
            apiCts.Dispose();
        }

        CancellationTokenSource? inactivityCts = Interlocked.Exchange(ref _inactivityCts, null);
        if (inactivityCts is not null)
        {
            inactivityCts.Cancel();
            inactivityCts.Dispose();
        }

        Timer? inactivityTimer = Interlocked.Exchange(ref _inactivityTimer, null);
        inactivityTimer?.Dispose();

        Timer? hardTimer = Interlocked.Exchange(ref _hardTimer, null);
        hardTimer?.Dispose();

        Instances.TryRemove(_interactionId, out _);
    }

    /// <summary>
    ///     Attempts to transition the state from <see cref="Available" /> to <see cref="InSession" />.
    /// </summary>
    /// <returns><c>true</c>if the transition was successful; otherwise, <c>false</c>.</returns>
    private bool TryAcquireSession() => TryTransitionState(Available, InSession);

    /// <summary>
    ///     Attempts to transition the state from <see cref="InSession" /> to <see cref="Available" />.
    /// </summary>
    /// <returns><c>true</c>if the transition was successful; otherwise, <c>false</c>.</returns>
    protected bool TryReleaseSession() => TryTransitionState(InSession, Available);

    /// <summary>
    ///     Attempts to transition the state from <see cref="InSession" /> to <see cref="CleanupInProgress" /> and applies a manual cleanup.
    /// </summary>
    /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
    [MustUseReturnValue("You must check the return value and return early if false")]
    protected bool TryComplete()
    {
        if (!TryTransitionState(InSession, CleanupInProgress))
        {
            return false;
        }

        Cleanup();
        using (CreateScope())
        {
            _logger.InstanceCompleted();
        }

        return true;
    }

    /// <summary>
    ///     Logs the specified exception, attempts to complete and clean up the instance,
    ///     invokes the timeout handler if applicable, and rethrows the exception while preserving the original stack trace.
    /// </summary>
    /// <param name="ex">The exception to handle and rethrow.</param>
    /// <remarks>
    ///     If the instance has not been completed, <see cref="TryComplete" /> and <see cref="DisableMessageComponentsAsync" /> are invoked.
    ///     The exception is rethrown using <see cref="ExceptionDispatchInfo.Throw()" /> to preserve the original call stack.
    /// </remarks>
    /// <exception cref="Exception">Always rethrows the original exception.</exception>
    [DoesNotReturn]
    protected async Task TryCompleteAndThrowAsync(Exception ex)
    {
        using (CreateScope())
        {
            _logger.InstanceUnhandledException(ex);
            if (TryComplete())
            {
                await DisableMessageComponentsAsync();
            }

            ExceptionDispatchInfo.Capture(ex).Throw();
        }
    }

    /// <summary>
    ///     Attempts to extend the inactivity timer.
    /// </summary>
    /// <returns><c>true</c> if successful; otherwise, <c>false</c>.</returns>
    [MustUseReturnValue("You must check the return value and return early if false")]
    protected bool TryExtend()
    {
        if (!IsInState(InSession))
        {
            return false;
        }

        try
        {
            Timer? inactivityTimer = _inactivityTimer;
            if (inactivityTimer is not null)
            {
                inactivityTimer.Change(InactivityTimeout, Timeout.InfiniteTimeSpan);
            }
            else
            {
                return false;
            }
        }
        catch (ObjectDisposedException)
        {
            return false;
        }

        using (CreateScope())
        {
            _logger.InstanceInactivityTimerExtended();
        }

        return true;
    }

    /// <summary>
    ///     Modifies the original message by disabling all the components.
    /// </summary>
    protected async Task DisableMessageComponentsAsync()
    {
        // Do not use cancellation token here as we want this API call to always succeed
        MessageComponent components = _originalMessage.Components.FromComponentsWithAllDisabled().Build();

        try
        {
            await _originalMessage.ModifyAsync(props => props.Components = components);
        }
        catch (InvalidOperationException)
        {
            // Token expired, message is no longer modifiable; also safe to ignore.
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
        {
            // Message deleted by user/bot; no action needed.
        }
        catch (Exception ex)
        {
            using (CreateScope())
            {
                _logger.InstanceUnhandledException(ex);
            }
        }
    }

    /// <summary>
    ///     Modifies the original interaction followup message.
    /// </summary>
    /// <param name="func">A delegate containing the properties to modify the message with.</param>
    protected async Task ModifyMessageAsync(Action<MessageProperties> func)
    {
        CancellationTokenSource? apiCts = _apiCts;
        if (apiCts is not { IsCancellationRequested: false })
        {
            return;
        }

        try
        {
            await _originalMessage.ModifyAsync(func, new RequestOptions { CancelToken = apiCts.Token });
        }
        catch (OperationCanceledException)
        {
            // API call was cancelled due to clean up, this is expected
        }
        catch (InvalidOperationException)
        {
            // Token expired, message is no longer modifiable; also safe to ignore.
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
        {
            // Message deleted by user/bot; no action needed.
        }
        catch (Exception ex)
        {
            using (CreateScope())
            {
                _logger.InstanceUnhandledException(ex);
            }
        }
    }

    /// <summary>
    ///     Follows up the deferred interaction with the initial response.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <returns>The message that was sent.</returns>
    protected abstract Task<RestFollowupMessage> FollowupWithInitialResponseAsync(SocketInteractionContext context);

    /// <summary>
    ///     Constructs a unique component id by combining the instance type <see cref="Name" />, the instance <see cref="_interactionId" />,
    ///     and a component-specific segment, using <see cref="Storage.ComponentWildcardSeparator" /> as the separator.
    /// </summary>
    /// <param name="str">The segment that identifies the specific component within this instance.</param>
    /// <returns>The full component id string.</returns>
    [Pure]
    protected string GenerateComponentId(string str)
    {
        // in 2090 this will be 20
        const int discordSnowflakeLength = 19;

        return String.Create(
            Name.Length + 1 + discordSnowflakeLength + 1 + str.Length,
            (Name, Id: _interactionId, str),
            static (span, state) =>
            {
                (string name, ulong id, string suffix) = state;

                name.AsSpan().CopyTo(span);
                int i = name.Length;
                span[i++] = Storage.ComponentWildcardSeparator;

                id.TryFormat(span[i..], out int idLen);
                i += idLen;

                span[i++] = Storage.ComponentWildcardSeparator;
                suffix.AsSpan().CopyTo(span[i..]);
            }
        );
    }

    /// <summary>
    ///     Initiates the instance Ensures the message is deferred, starts the timer, and sends the initial response.
    /// </summary>
    /// <param name="context">The context.</param>
    public async Task InitAsync(SocketInteractionContext context)
    {
        if (!context.Interaction.HasResponded)
        {
            await context.Interaction.DeferAsync(ephemeral: IsEphemeral);
        }

        _apiCts = new CancellationTokenSource();
        _inactivityCts = new CancellationTokenSource();
        CancellationToken inactivityCtsToken = _inactivityCts.Token;

        _inactivityTimer = new Timer(
            static void (state) =>
            {
                (InstanceManager<T> instance, CancellationToken token) = ((InstanceManager<T>, CancellationToken))state!;
                if (token.IsCancellationRequested || !instance.TryTransitionState(Available, CleanupInProgress))
                {
                    return;
                }

                instance.Cleanup();
                using (instance.CreateScope())
                {
                    instance._logger.InstanceInactivityTimeoutTriggered();
                }

                RunOnTimeout(instance);
            },
            (this, inactivityCtsToken),
            InactivityTimeout,
            Timeout.InfiniteTimeSpan
        );

        _hardTimer = new Timer(
            static void (state) =>
            {
                var instance = (InstanceManager<T>)state!;
                if (instance.ForceTransitionState(CleanupInProgress) == CleanupInProgress)
                {
                    return;
                }

                instance.Cleanup();
                using (instance.CreateScope())
                {
                    instance._logger.InstanceHardTimeoutTriggered();
                }

                RunOnTimeout(instance);
            },
            this,
            HardTimeout,
            Timeout.InfiniteTimeSpan
        );

        Instances[_interactionId] = (T)this;
        using (CreateScope())
        {
            _logger.InstanceTimerStarted();
        }

        _originalMessage = await FollowupWithInitialResponseAsync(context);
    }
}
