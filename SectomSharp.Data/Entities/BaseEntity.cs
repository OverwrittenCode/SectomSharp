using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public abstract class BaseEntity
{
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; internal set; }
}

public abstract class BaseEntityConfiguration<T> : IEntityTypeConfiguration<T>
    where T : BaseEntity
{
    /// <inheritdoc />
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(entity => entity.CreatedAt).HasColumnType(Constants.PostgreSql.Timestamptz).IsRequired();
        builder.Property(entity => entity.UpdatedAt).HasColumnType(Constants.PostgreSql.Timestamptz);
    }
}
