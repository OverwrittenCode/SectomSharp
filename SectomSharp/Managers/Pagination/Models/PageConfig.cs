using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration for how to handle items in a nested menu.
/// </summary>
/// <typeparam name="T">The type of the page.</typeparam>
internal sealed class PageConfig<T>
{
    /// <summary>
    ///     Gets or initialises the function to retrieve the label of the page.
    /// </summary>
    public required Func<T, string> GetLabel { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the value of the page.
    /// </summary>
    public required Func<T, string> GetValue { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the description of the page.
    /// </summary>
    public Func<T, string?>? GetDescription { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the emote associated with the page.
    /// </summary>
    public Func<T, IEmote?>? GetEmote { get; init; }
}
