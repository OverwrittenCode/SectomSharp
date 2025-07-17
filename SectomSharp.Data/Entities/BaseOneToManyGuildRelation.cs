using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public abstract class BaseOneToManyGuildRelation : BaseEntity
{
    public required ulong GuildId { get; [UsedImplicitly] init; }
    public Guild Guild { get; [UsedImplicitly] init; } = null!;
}

public abstract class BaseOneToManyGuildRelationConfiguration<TEntity> : BaseEntityConfiguration<TEntity>
    where TEntity : BaseOneToManyGuildRelation
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(relation => relation.GuildId).IsRequiredSnowflakeId();
        builder.HasOne(relation => relation.Guild).WithMany().HasForeignKey(relation => relation.GuildId);
        base.Configure(builder);
    }
}
