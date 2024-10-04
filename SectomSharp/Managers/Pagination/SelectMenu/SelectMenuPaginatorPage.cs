using Discord;

namespace SectomSharp.Managers.Pagination.SelectMenu;

/// <summary>
///     Represents a single page in a select menu-based pagination system,
///     containing embeds and interactive components.
/// </summary>
internal readonly record struct SelectMenuPaginatorPage
{
    /// <summary>
    ///     Gets the embeds for the paginator page.
    /// </summary>
    public Embed[] Embeds { get; }

    /// <summary>
    ///     Gets the action rows for the paginator page.
    /// </summary>
    public List<ActionRowBuilder> ActionRows { get; }

    /// <param name="embeds">The embeds to display.</param>
    /// <param name="actionRows">The action rows to display.</param>
    public SelectMenuPaginatorPage(Embed[] embeds, List<ActionRowBuilder> actionRows)
    {
        Embeds = embeds;
        ActionRows = actionRows;
    }

    ///<inheritdoc cref="SelectMenuPaginatorPage(Embed[], List{ActionRowBuilder})"/>
    public SelectMenuPaginatorPage(Embed[] embeds)
        : this(embeds, []) { }
}
