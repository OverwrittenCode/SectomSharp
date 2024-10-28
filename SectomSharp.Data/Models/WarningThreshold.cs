using Discord;
using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class WarningThreshold
{
    public required BotLogType LogType { get; init; }
    public required int Value { get; init; }

    public TimeSpan? Span { get; init; }

    public string Display()
    {
        var ordinalSuffix = Value % 100 is >= 11 and <= 13
            ? "th"
            : (Value % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th"
            };

        var strikePosition = Value + ordinalSuffix;

        var durationText = Span is not { } timeSpan
            ? ""
            : timeSpan switch
            {
                { Days: var d and > 0 } => $"{d} day",
                { Hours: var h and > 0 } => $"{h} hour",
                { Minutes: var m and > 0 } => $"{m} minute",
                _ => $"{timeSpan.Seconds} second"
            };

        return $"- {strikePosition} Strike: {Format.Bold($"{durationText} {LogType}")}";
    }
}
