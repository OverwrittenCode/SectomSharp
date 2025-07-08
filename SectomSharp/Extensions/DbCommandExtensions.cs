using System.Data.Common;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace SectomSharp.Extensions;

internal static class DbCommandExtensions
{
    /// <summary>
    ///     Executes a scalar command asynchronously while timing and logging the query duration,
    ///     and attempts to cast the result to the specified type <typeparamref name="T" />.
    /// </summary>
    /// <typeparam name="T">The expected type of the scalar result.</typeparam>
    /// <param name="cmd">The database command to execute.</param>
    /// <param name="logger">The logger used to log the elapsed time.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The scalar result cast to <typeparamref name="T" />.</returns>
    /// <exception cref="InvalidCastException">Thrown if the result cannot be cast to <typeparamref name="T" />.</exception>
    public static async Task<T> ExecuteScalarTimedAsync<T>(this DbCommand cmd, ILogger logger, CancellationToken cancellationToken = default)
        => await cmd.ExecuteScalarTimedAsync(logger, cancellationToken) is T result ? result : throw new InvalidCastException($"Returned value is not of type {typeof(T)}");

    /// <summary>
    ///     Executes a scalar command asynchronously while timing and logging the query duration.
    /// </summary>
    /// <param name="cmd">The database command to execute.</param>
    /// <param name="logger">The logger used to log the elapsed time.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>The scalar result from the command execution.</returns>
    public static async Task<object?> ExecuteScalarTimedAsync(this DbCommand cmd, ILogger logger, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        object? scalarResult = await cmd.ExecuteScalarAsync(cancellationToken);
        stopwatch.Stop();
        logger.SqlQueryExecuted(stopwatch.ElapsedMilliseconds);
        return scalarResult;
    }
}
