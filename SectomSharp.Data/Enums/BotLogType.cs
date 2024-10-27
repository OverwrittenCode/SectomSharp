namespace SectomSharp.Data.Enums;

[Flags]
public enum BotLogType
{
    Warn = 1 << 0,
    Ban = 1 << 1,
    Softban = 1 << 2,
    Timeout = 1 << 3,
    Configuration = 1 << 4,
    Kick = 1 << 5,
    Deafen = 1 << 6,
    Mute = 1 << 7,
    Nick = 1 << 8,
    Purge = 1 << 9,
    ModNote = 1 << 10,
    All =
        Warn
        | Ban
        | Softban
        | Timeout
        | Configuration
        | Kick
        | Deafen
        | Mute
        | Nick
        | Purge
        | ModNote
}
