using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class WarningThreshold
{
    public required BotLogType LogType { get; init; }
    public required int Value { get; init; }

    public TimeSpan? Span { get; init; }
}
