namespace SectomSharp.Data.Enums;

[Flags]
public enum BotLogType
{
    Warn = 1 << 0,
    Ban = 1 << 1,
    Softban = 1 << 2,
    Timeout = 1 << 3,
    Configuration = 1 << 4,
}
