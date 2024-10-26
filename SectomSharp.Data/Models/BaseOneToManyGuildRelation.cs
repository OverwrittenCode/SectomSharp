namespace SectomSharp.Data.Models;

public abstract class BaseOneToManyGuildRelation : BaseEntity
{
    public Guild Guild { get; } = null!;
    public required ulong GuildId { get; init; }
}
