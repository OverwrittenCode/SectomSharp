using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class SuggestionPost : BaseOneToManyGuildRelation
{
    public int Id { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] private set; }

    public ICollection<SuggestionVote> Votes { get; } = [];

    public required ulong AuthorId { get; init; }
    public User Author { get; init; } = null!;

    public required SuggestionStatus Status { get; init; }

    public int UpvoteCount { get; init; }
    public int DownvoteCount { get; init; }
}

public sealed class SuggestionPostConfiguration : BaseOneToManyGuildRelationConfiguration<SuggestionPost>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<SuggestionPost> builder)
    {
        builder.Property(post => post.Id).ValueGeneratedOnAdd();
        builder.Property(post => post.AuthorId).IsRequiredSnowflakeId();
        builder.HasOne(post => post.Author).WithMany().HasForeignKey(post => new { post.GuildId, post.AuthorId });

        builder.Property(post => post.Status).IsRequired().HasDefaultValue(SuggestionStatus.Pending);
        builder.Property(post => post.UpvoteCount).IsRequired().HasDefaultValue(0);
        builder.Property(post => post.DownvoteCount).IsRequired().HasDefaultValue(0);

        builder.HasKey(post => new { post.GuildId, post.Id });

        base.Configure(builder);
    }
}
