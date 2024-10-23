using SectomSharp.Data.Enums;

namespace SectomSharp.Data.Models;

public sealed class Case : BaseOneToManyGuildRelation
{
    public required string Id { get; init; }

    public ulong? PerpetratorId { get; init; }
    public User? Perpetrator { get; init; }

    public ulong? TargetId { get; init; }
    public User? Target { get; init; }

    public ulong? ChannelId { get; init; }
    public Channel? Channel { get; init; }

    public required BotLogType LogType { get; init; }
    public required OperationType OperationType { get; init; }

    public DateTime? ExpiresAt { get; init; }
    public string? Reason { get; set; }
    public string? LogMessageURL { get; set; }
}
