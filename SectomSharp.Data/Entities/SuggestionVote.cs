using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SectomSharp.Data.Enums;
using SectomSharp.Data.Extensions;

namespace SectomSharp.Data.Entities;

public sealed class SuggestionVote : BaseOneToManyGuildRelation
{
    public required ulong UserId { get; init; }
    public User User { get; init; } = null!;

    public required int SuggestionId { get; init; }
    public SuggestionPost Suggestion { get; init; } = null!;

    public required VoteType Type { get; init; }
}

public sealed class SuggestionVoteConfiguration : BaseOneToManyGuildRelationConfiguration<SuggestionVote>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<SuggestionVote> builder)
    {
        builder.HasOne(vote => vote.Guild).WithMany().HasForeignKey(vote => vote.GuildId).IsRequired();

        builder.Property(vote => vote.UserId).IsRequiredSnowflakeId();
        builder.HasOne(vote => vote.User).WithMany().HasForeignKey(vote => new { vote.GuildId, vote.UserId });

        builder.HasOne(vote => vote.Suggestion).WithMany(suggestion => suggestion.Votes).HasForeignKey(vote => new { vote.GuildId, vote.SuggestionId });

        builder.Property(vote => vote.Type).IsRequired();

        builder.HasIndex(vote => new { vote.GuildId, vote.UserId, vote.SuggestionId }).IsUnique();
        builder.HasIndex(vote => new { vote.GuildId, vote.SuggestionId, vote.Type });

        builder.HasKey(vote => new { vote.GuildId, vote.UserId, vote.SuggestionId });

        base.Configure(builder);
    }
}
