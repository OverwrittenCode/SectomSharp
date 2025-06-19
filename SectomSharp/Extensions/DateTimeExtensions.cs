using Discord;

namespace SectomSharp.Extensions;

internal static class DateTimeExtensions
{
    /// <summary>
    ///     Immediately formats the provided time and style into a relative timestamp string.
    /// </summary>
    /// <param name="time">The time of this timestamp tag.</param>
    /// <returns>The newly create timestamp string.</returns>
    public static string GetRelativeTimestamp(this DateTime time) => TimestampTag.FormatFromDateTime(time, TimestampTagStyles.Relative);
}
