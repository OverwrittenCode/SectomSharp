using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public abstract class BaseOneToManyGuildRelation : BaseEntity
{
    public required ulong GuildId { get; init; }
    public Guild Guild { get; private set; } = null!;
}

public abstract class BaseOneToManyGuildRelationConfiguration<TThis> : BaseEntityConfiguration<TThis>
    where TThis : BaseOneToManyGuildRelation
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<TThis> builder)
    {
        builder.Property(relation => relation.GuildId).IsRequiredSnowflakeId();
        base.Configure(builder);
    }
}
