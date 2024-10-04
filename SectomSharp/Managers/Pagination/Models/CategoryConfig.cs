using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration for how to handle categories in a nested menu.
/// </summary>
/// <typeparam name="T">The type of the category.</typeparam>
internal sealed class CategoryConfig<T>
{
    /// <summary>
    ///     Gets or initialises the function to retrieve the name of the category.
    /// </summary>
    public required Func<T, string> GetName { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the value of the category.
    /// </summary>
    public required Func<T, string> GetValue { get; init; }

    /// <summary>
    ///     Gets or initialises the prefix for the custom id.
    /// </summary>
    public required string CustomIdPrefix { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the
    ///     wildcards for the custom id of the category.
    /// </summary>
    public required Func<T, string[]> GetCustomIdWildcards { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the description of the category.
    /// </summary>
    public Func<T, string?>? GetDescription { get; init; }

    /// <summary>
    ///     Gets or initialises the function to retrieve the emote associated with the category.
    /// </summary>
    public Func<T, IEmote?>? GetEmote { get; init; }
}
