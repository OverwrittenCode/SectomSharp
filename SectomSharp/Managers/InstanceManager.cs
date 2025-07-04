using SectomSharp.Utils;

namespace SectomSharp.Managers;

/// <summary>
///     Provides base functionality for managing instances of a specific type with unique identifiers.
/// </summary>
/// <typeparam name="T">The type of instance being managed. Must inherit from <see cref="InstanceManager{T}" />.</typeparam>
internal abstract class InstanceManager<T> : IDisposable, IAsyncDisposable
    where T : InstanceManager<T>
{
    /// <summary>
    ///     A dictionary storing all active instances of <typeparamref name="T" /> keyed by <see cref="Id" />.
    /// </summary>
    private static readonly Dictionary<string, T> Instances = [];

    /// <summary>
    ///     Gets a read-only collection of all active instances of <typeparamref name="T" /> keyed by <see cref="Id" />.
    /// </summary>
    public static IReadOnlyDictionary<string, T> AllInstances => Instances;

    private readonly Lock _timerLock = new();
    private bool _disposedValue;
    private Timer? _expirationTimer;

    private CancellationTokenSource? _timeoutCts;

    /// <summary>
    ///     Gets the unique identifier for <typeparamref name="T" />.
    /// </summary>
    public string Id { get; }

    /// <summary>
    ///     Initialises a new instance of the InstanceManager class with an optional pre-generated ID.
    /// </summary>
    /// <param name="id">An optional pre-generated ID. If <c>null</c>, a new ID will be generated.</param>
    protected InstanceManager(string? id = null)
    {
        Id = id ?? StringUtils.GenerateUniqueId();
        var instance = (T)this;
        Instances[instance.Id] = instance;
    }

    /// <summary>
    ///     Asynchronously performs cleanup operations and disposes of resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (!_disposedValue)
        {
            try
            {
                await CleanupAsync();
            }
            finally
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }
    }

    /// <summary>
    ///     Performs synchronous cleanup of resources and removes this instance from the instance collection.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Throws an <see cref="ObjectDisposedException" /> if this <typeparamref name="T" /> instance has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the instance has been disposed.</exception>
    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposedValue, this);

    /// <summary>
    ///     Removes this instance from the static instance collection.
    /// </summary>
    /// <inheritdoc cref="ThrowIfDisposed" path="/exception" />
    private void RemoveInstance()
    {
        ThrowIfDisposed();
        Instances.Remove(Id);
    }

    /// <summary>
    ///     Cancels any pending cleanup operation and disposes associated resources.
    /// </summary>
    /// <inheritdoc cref="ThrowIfDisposed" path="/exception" />
    private void CancelCleanup()
    {
        ThrowIfDisposed();

        lock (_timerLock)
        {
            try
            {
                _timeoutCts?.Cancel();
                _timeoutCts?.Dispose();
                _timeoutCts = null;

                _expirationTimer?.Dispose();
                _expirationTimer = null;
            }
            catch
            {
                // Swallow any exceptions during cancellation
            }
        }
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the object and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources;
    ///     <c>false</c> to release only unmanaged resources.
    /// </param>
    private void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            CancelCleanup();
            RemoveInstance();
        }

        _disposedValue = true;
    }

    /// <summary>
    ///     Performs asynchronous cleanup operations specific to the implementing class.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup operation.</returns>
    protected abstract Task CleanupAsync();

    /// <summary>
    ///     Starts a timer that disposes this instance unless cancelled.
    /// </summary>
    /// <param name="seconds">The number of seconds to wait before disposal.</param>
    /// <returns>A task representing the timer initialization.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="seconds" /> is less than 0.</exception>
    /// <inheritdoc cref="ThrowIfDisposed" path="/exception" />
    protected Task StartExpirationTimer(int seconds)
    {
        ThrowIfDisposed();

        ArgumentOutOfRangeException.ThrowIfNegative(seconds);

        lock (_timerLock)
        {
            CancelCleanup();

            _timeoutCts = new CancellationTokenSource();
            CancellationToken token = _timeoutCts.Token;

            _expirationTimer?.Dispose();
            _expirationTimer = new Timer(
                _ =>
                {
                    Task.Run(
                        async () =>
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }

                            try
                            {
                                await CleanupAsync();
                            }
                            finally
                            {
                                RemoveInstance();
                            }
                        },
                        token
                    );
                },
                null,
                TimeSpan.FromSeconds(seconds),
                Timeout.InfiniteTimeSpan
            );
        }

        return Task.CompletedTask;
    }
}
