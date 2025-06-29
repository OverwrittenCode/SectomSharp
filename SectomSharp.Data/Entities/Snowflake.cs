using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public interface ISnowflakeId
{
    ulong Id { get; }
}

public abstract class Snowflake : BaseOneToManyGuildRelation, ISnowflakeId
{
    public required ulong Id { get; [UsedImplicitly] init; }
}

public abstract class SnowflakeIdConfiguration<T> : BaseEntityConfiguration<T>
    where T : BaseEntity, ISnowflakeId
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(entity => entity.Id).IsRequiredSnowflakeId();
        base.Configure(builder);
    }
}

public abstract class SnowflakeConfiguration<T> : BaseOneToManyGuildRelationConfiguration<T>
    where T : Snowflake
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(entity => entity.Id).IsRequiredSnowflakeId();
        base.Configure(builder);
    }
}
