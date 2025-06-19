using Discord;

namespace SectomSharp.Managers.Pagination.Models;

/// <summary>
///     Configuration options for nested menu creation.
/// </summary>
/// <param name="EmbedTitle">The embed title.</param>
/// <param name="EmbedColour">The embed colour.</param>
internal readonly record struct NestedMenuConfig(string EmbedTitle, Color EmbedColour);
