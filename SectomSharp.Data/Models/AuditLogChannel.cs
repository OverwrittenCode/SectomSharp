using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class AuditLogChannel : Snowflake
{
    public required string WebhookUrl { get; init; }

    public required AuditLogType Type { get; set; }
}
