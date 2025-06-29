using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAt
    {
        get;
        [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] private set;
    }
}

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<T> builder)
        => builder.Property(e => e.CreatedAt).HasColumnType(Constants.PostgreSql.Timestamptz).HasDefaultValueSql(Constants.PostgreSql.Now).ValueGeneratedOnAdd().IsRequired();
}
