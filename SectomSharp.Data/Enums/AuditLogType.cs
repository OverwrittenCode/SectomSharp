namespace SectomSharp.Data.Enums;

[Flags]
public enum AuditLogType
{
    Server = 1 << 0,
    Member = 1 << 1,
    Message = 1 << 2,
    Emoji = 1 << 3,
    Sticker = 1 << 4,
    Channel = 1 << 5,
    Thread = 1 << 6,
    Role = 1 << 7,
}
