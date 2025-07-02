using Discord;
using Discord.Interactions;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Managers.Pagination.Builders;

/// <summary>
///     Builder for creating button-based pagination instances.
/// </summary>
internal sealed class ButtonPaginationBuilder
{
    private const int Timeout = 180;

    /// <summary>
    ///     Gets or sets the list of extra action rows to be added to the pagination.
    /// </summary>
    private List<ActionRowBuilder> ExtraActionRows { get; } = [];

    /// <summary>
    ///     Gets or sets the list of embeds used in the pagination.
    /// </summary>
    public List<Embed> Embeds { get; init; } = [];

    /// <summary>
    ///     Builds a new instance of <see cref="ButtonPaginationManager" />.
    /// </summary>
    /// <returns>The new instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><see cref="Timeout" /> is less than or equal to 0.</exception>
    /// <exception cref="InvalidOperationException">Empty list of <see cref="Embeds" />.</exception>
    public async Task BuildAndInit(SocketInteractionContext context)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(Timeout);
        ArgumentOutOfRangeException.ThrowIfZero(Embeds.Count);

        var manager = new ButtonPaginationManager([.. Embeds], [.. ExtraActionRows]);
        await manager.InitAsync(context);
    }
}
