using Discord;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Managers.Pagination.Builders;

/// <summary>
///     Builder for creating button-based pagination instances.
/// </summary>
internal sealed class ButtonPaginationBuilder
{
    private const int Timeout  = 180;

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
    public ButtonPaginationManager Build()
    {
        if (Timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Value must be greater than 0.");
        }

        if (Embeds.Count == 0)
        {
            throw new InvalidOperationException("At least one embed must be added before building.");
        }

        return new ButtonPaginationManager([.. Embeds], [.. ExtraActionRows], Timeout, IsEphemeral);
    }
}
