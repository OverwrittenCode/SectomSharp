namespace SectomSharp.Data.Models;

public abstract class BaseGuildRelation : BaseEntity
{
    public required ulong GuildId { get; init; }

    public Guild Guild { get; init; } = null!;
}
