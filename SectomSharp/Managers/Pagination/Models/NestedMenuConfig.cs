using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration options for nested menu creation.
/// </summary>
internal readonly record struct NestedMenuConfig(string EmbedTitle, Color EmbedColour);
