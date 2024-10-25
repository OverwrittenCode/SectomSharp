using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class AuditLogChannel : Snowflake
{
    public required string WebhookUrl { get; set; }

    public required AuditLogType AuditLogType { get; set; }
}
