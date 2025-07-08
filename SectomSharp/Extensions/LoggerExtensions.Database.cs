using Microsoft.Extensions.Logging;

namespace SectomSharp.Extensions;

internal static partial class LoggerExtensions
{
    [LoggerMessage(50, LogLevel.Information, "Executed DbCommand ({ElapsedMilliseconds} ms)")]
    public static partial void SqlQueryExecuted(this ILogger logger, long elapsedMilliseconds);
}
