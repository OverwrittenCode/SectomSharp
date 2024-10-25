using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class BotLogChannel : Snowflake
{
    public required BotLogType BotLogType { get; set; }
}
