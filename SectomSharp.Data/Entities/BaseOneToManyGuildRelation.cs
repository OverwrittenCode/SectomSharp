namespace SectomSharp.Data.Entities;

public abstract class BaseOneToManyGuildRelation : BaseEntity
{
    public required ulong GuildId { get; init; }
    public Guild Guild { get; private set; } = null!;
}
