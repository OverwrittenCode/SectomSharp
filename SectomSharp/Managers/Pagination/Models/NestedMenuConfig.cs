using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration options for nested menu creation.
/// </summary>
internal sealed class NestedMenuConfig
{
    /// <summary>
    ///     Gets or initialises the title of the embed for the menu.
    /// </summary>
    public required string EmbedTitle { get; init; }

    /// <summary>
    ///     Gets or initialises the colour of the embed for the menu.
    /// </summary>
    public required Color EmbedColour { get; init; }

    /// <summary>
    ///     Gets or initialises the description for category selection.
    /// </summary>
    public string CategoryDescription { get; init; } = "Select an option below to view details";

    /// <summary>
    ///     Gets or initialises the description for the home page.
    /// </summary>
    public string HomePageDescription { get; init; } =
        "Select a category from the menu below to view its contents";
}
