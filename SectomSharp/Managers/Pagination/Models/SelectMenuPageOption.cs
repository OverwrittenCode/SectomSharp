using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Represents a select menu option with its associated page content.
/// </summary>
internal sealed class SelectMenuPageOption
{
    /// <summary>
    ///     Gets or initialises the label of the select menu option.
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    ///     Gets or initialises the value of the select menu option.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    ///     Gets or initialises the embeds associated with the select menu option.
    /// </summary>
    public required Embed[] Embeds { get; init; }

    /// <summary>
    ///     Gets or initialises the description of the select menu option.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    ///     Gets or initialises the emote associated with the select menu option.
    /// </summary>
    public IEmote? Emote { get; init; }

    /// <summary>
    ///     Gets or initialises the action rows associated with the select menu option.
    /// </summary>
    public List<ActionRowBuilder> ActionRows { get; init; } = [];
}
