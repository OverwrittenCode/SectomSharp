using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public sealed class SuggestionPanel : BaseOneToManyGuildRelation
{
    public int Id { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] private set; }

    public ICollection<SuggestionComponent> Components { get; } = [];

    public required string Name { get; [UsedImplicitly] set; }
    public required string Description { get; [UsedImplicitly] set; }
    public Color Color { get; [UsedImplicitly] set; }
}

public sealed class SuggestionPanelConfiguration : BaseOneToManyGuildRelationConfiguration<SuggestionPanel>
{
    public const int MaxNameLength = EmbedBuilder.MaxTitleLength;
    public const int MaxDescriptionLength = EmbedBuilder.MaxDescriptionLength;

    public override void Configure(EntityTypeBuilder<SuggestionPanel> builder)
    {
        builder.Property(panel => panel.Id).ValueGeneratedOnAdd();
        builder.Property(panel => panel.Name).IsRequired().HasMaxLength(MaxNameLength);
        builder.Property(panel => panel.Description).IsRequired().HasMaxLength(MaxDescriptionLength);
        builder.Property(panel => panel.Color).HasConversion(color => (int)color.RawValue, rawValue => new Color((uint)rawValue));
        builder.HasIndex(panel => new { panel.GuildId, panel.Name }).IsUnique();
        base.Configure(builder);
    }
}
