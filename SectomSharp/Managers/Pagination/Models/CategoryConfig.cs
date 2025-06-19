using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration for how to handle categories in a nested menu.
/// </summary>
/// <param name="CustomIdPrefix">The prefix for the custom id.</param>
/// <param name="GetName">A callback for generating the name.</param>
/// <param name="GetValue">A callback for generating the value.</param>
/// <param name="GetCustomIdWildcards">A callback for getting the custom id wildcards.</param>
/// <param name="GetDescription">A callback for generating the description.</param>
/// <param name="GetEmote">A callback for generating the emote.</param>
/// <typeparam name="T">The data for the category.</typeparam>
internal readonly record struct CategoryConfig<T>
(
    string CustomIdPrefix,
    Func<T, string> GetName,
    Func<T, string> GetValue,
    Func<T, string[]> GetCustomIdWildcards,
    Func<T, string>? GetDescription = null,
    Func<T, IEmote?>? GetEmote = null
);
