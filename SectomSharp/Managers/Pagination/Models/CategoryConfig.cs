using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration for how to handle categories in a nested menu.
/// </summary>
/// <typeparam name="T">The type of the category.</typeparam>
internal readonly record struct CategoryConfig<T>(
    string CustomIdPrefix,
    Func<T, string> GetName,
    Func<T, string> GetValue,
    Func<T, string[]> GetCustomIdWildcards,
    Func<T, string>? GetDescription = null,
    Func<T, IEmote?>? GetEmote = null
);
