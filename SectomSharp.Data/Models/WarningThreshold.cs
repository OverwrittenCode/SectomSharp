using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class WarningThreshold
{
    public required BotLogType LogType { get; set; }
    public required int Value { get; set; }

    public TimeSpan? Span { get; set; }
}
