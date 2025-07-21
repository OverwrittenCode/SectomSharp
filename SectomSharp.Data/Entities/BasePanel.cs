using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public abstract class BasePanel<TThis, TComponent> : BaseOneToManyGuildRelation
    where TThis : BasePanel<TThis, TComponent>
    where TComponent : BaseComponent<TComponent, TThis>
{
    public int Id { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] private set; }

    public ICollection<TComponent> Components { get; } = [];

    public required string Name { get; [UsedImplicitly] set; }
    public required string Description { get; [UsedImplicitly] set; }
    public Color Color { get; [UsedImplicitly] set; }
}

public static class BasePanel
{
    public const int MaxNameLength = EmbedBuilder.MaxTitleLength;
    public const int MaxDescriptionLength = EmbedBuilder.MaxDescriptionLength;
}

public abstract class BasePanelConfiguration<TPanel, TSubject> : BaseOneToManyGuildRelationConfiguration<TPanel>
    where TPanel : BasePanel<TPanel, TSubject>
    where TSubject : BaseComponent<TSubject, TPanel>
{
    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<TPanel> builder)
    {
        builder.Property(panel => panel.Id).ValueGeneratedOnAdd();
        builder.Property(panel => panel.Name).IsRequired().HasMaxLength(BasePanel.MaxNameLength);
        builder.Property(panel => panel.Description).IsRequired().HasMaxLength(BasePanel.MaxDescriptionLength);
        builder.Property(panel => panel.Color).HasConversion(color => (int)color.RawValue, rawValue => new Color((uint)rawValue));
        builder.HasIndex(panel => new { panel.GuildId, panel.Name }).IsUnique();
        base.Configure(builder);
    }
}
