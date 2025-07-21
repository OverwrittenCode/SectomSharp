using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SectomSharp.Data.Entities;

public abstract class BaseComponent<TThis, TPanel> : BaseOneToManyGuildRelation
    where TThis : BaseComponent<TThis, TPanel>
    where TPanel : BasePanel<TPanel, TThis>
{
    public int Id { get; [UsedImplicitly(Reason = Constants.ValueGeneratedOnAdd)] private set; }

    public required int PanelId { get; [UsedImplicitly] init; }
    public TPanel Panel { get; [UsedImplicitly] init; } = null!;

    public required string Name { get; [UsedImplicitly] set; }
    public required string Description { get; [UsedImplicitly] set; }
    public IEmote? Emote { get; [UsedImplicitly] set; }
}

public static class BaseComponent
{
    public const int MaxNameLength = SelectMenuOptionBuilder.MaxSelectLabelLength;
    public const int MaxDescriptionLength = SelectMenuOptionBuilder.MaxDescriptionLength;

    /// <remarks>
    ///     The maximum length, in characters, of a custom Discord emoji tag (<c>&lt;a:name:id&gt;</c>).
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Animated prefix (<c>&lt;a:</c>) = 3 characters</description>
    ///         </item>
    ///         <item>
    ///             <description>Maximum emoji name = 32 characters</description>
    ///         </item>
    ///         <item>
    ///             <description>Separator (<c>:</c>) = 1 character</description>
    ///         </item>
    ///         <item>
    ///             <description>Maximum emoji snowflake id = 20 digits</description>
    ///         </item>
    ///         <item>
    ///             <description>Closing bracket (<c>&gt;</c>) = 1 character</description>
    ///         </item>
    ///     </list>
    ///     <para>Total: 3 + 32 + 1 + 20 + 1 = 57</para>
    /// </remarks>
    public const int MaxIEmoteLength = 57;
}

public abstract class BaseComponentConfiguration<TComponent, TPanel> : BaseOneToManyGuildRelationConfiguration<TComponent>
    where TComponent : BaseComponent<TComponent, TPanel>
    where TPanel : BasePanel<TPanel, TComponent>
{
    private static IEmote ParseIEmote(string text) => Emoji.TryParse(text, out Emoji? emoji) ? emoji : Emote.Parse(text);

    /// <inheritdoc />
    public override void Configure(EntityTypeBuilder<TComponent> builder)
    {
        builder.HasOne(component => component.Panel).WithMany(panel => panel.Components).HasForeignKey(component => component.PanelId).IsRequired();

        builder.Property(component => component.Id).ValueGeneratedOnAdd();

        builder.Property(component => component.Name).IsRequired().HasMaxLength(BaseComponent.MaxNameLength);
        builder.Property(component => component.Description).IsRequired().HasMaxLength(BaseComponent.MaxDescriptionLength);
        builder.Property(component => component.Emote)
               .IsUnicode()
               .HasMaxLength(BaseComponent.MaxIEmoteLength)
               .HasConversion(emoji => emoji == null ? null : emoji.ToString(), text => text == null ? null : ParseIEmote(text));

        builder.HasIndex(component => new { component.GuildId, component.PanelId, component.Name }).IsUnique();

        base.Configure(builder);
    }
}
