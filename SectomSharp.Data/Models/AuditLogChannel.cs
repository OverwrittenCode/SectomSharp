using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class AuditLogChannel : Snowflake
{
    public required string WebhookUrl { get; set; }

    public AuditLogType AuditLogType { get; set; }
    public OperationType OperationType { get; set; }
}
