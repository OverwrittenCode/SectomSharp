using System.Runtime.InteropServices;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using SectomSharp.Utils;

namespace SectomSharp.Managers.Pagination;

/// <summary>
///     Represents a base class for pagination functionality with shared utility methods.
///     Provides common functionality for handling paginated content in Discord interactions.
/// </summary>
/// <typeparam name="T">The type of the implementing pagination manager. Must inherit from <see cref="InstanceManager{T}" />.</typeparam>
internal abstract class BasePagination<T> : InstanceManager<T>
    where T : BasePagination<T>
{
    /// <summary>
    ///     The maximum number of items to include in each chunk when splitting content.
    ///     Used when content exceeds Discord's maximum embed description length.
    /// </summary>
    private const int ChunkSize = 10;

    /// <summary>
    ///     Creates an embed builder with standard formatting.
    /// </summary>
    /// <param name="description">The content to display in the embed.</param>
    /// <param name="title">The title of the embed.</param>
    /// <returns>A configured EmbedBuilder instance.</returns>
    private static EmbedBuilder GetEmbedBuilder(string description, string title)
        => new()
        {
            Description = description,
            Title = title,
            Color = Storage.LightGold
        };

    /// <summary>
    ///     Creates an array of embeds by splitting <paramref name="strings" /> into chunks of <see cref="ChunkSize" />.
    /// </summary>
    /// <param name="strings">The strings to split into chunks.</param>
    /// <param name="title">The title of each embed.</param>
    /// <returns>An array of embed objects.</returns>
    /// <exception cref="InvalidOperationException">A chunk of <paramref name="strings" /> exceeds <see cref="EmbedBuilder.MaxDescriptionLength" />.</exception>
    public static Embed[] GetEmbeds(List<string> strings, string title)
    {
        Span<string> span = CollectionsMarshal.AsSpan(strings);
        var chunks = new List<string>();

        for (int i = 0; i < span.Length; i += ChunkSize)
        {
            string chunk = String.Join('\n', span.Slice(i, Math.Min(ChunkSize, span.Length - i)));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(chunk.Length, EmbedBuilder.MaxDescriptionLength);
            chunks.Add(chunk);
        }

        if (chunks.Count == 1)
        {
            return [GetEmbedBuilder(chunks[0], title).Build()];
        }

        IEnumerable<Embed> embeds = chunks.Select((description, i)
            => GetEmbedBuilder(description, title).WithFooter(builder => builder.WithText($"Page {i + 1} / {chunks.Count}")).Build()
        );

        return [.. embeds];
    }

    /// <inheritdoc />
    protected BasePagination(ILoggerFactory loggerFactory, SocketInteractionContext context) : base(loggerFactory, context) { }
}
