namespace SectomSharp.Data.Models;

public abstract class BaseOneToManyGuildRelation : BaseEntity
{
    public Guild Guild { get; private set; } = null!;
    public required ulong GuildId { get; init; }
}
