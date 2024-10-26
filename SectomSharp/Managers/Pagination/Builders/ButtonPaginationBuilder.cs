using Discord;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Managers.Pagination.Builders;

/// <summary>
///     Builder for creating button-based pagination instances.
/// </summary>
internal sealed class ButtonPaginationBuilder
{
    /// <summary>
    ///     Gets or sets the list of extra action rows to be added to the pagination.
    /// </summary>
    private List<ActionRowBuilder> ExtraActionRows { get; } = [];

    /// <summary>
    ///     Gets or sets a value indicating whether the pagination response should be ephemeral.
    /// </summary>
    private bool IsEphemeral { get; set; }

    /// <summary>
    ///     Gets or sets the list of embeds used in the pagination.
    /// </summary>
    public List<Embed> Embeds { get; init; } = [];

    /// <summary>
    ///     Gets or sets the timeout duration for the pagination in seconds.
    /// </summary>
    public int Timeout { get; set; } = 180;

    /// <summary>
    ///     Sets the timeout duration for the pagination in seconds.
    /// </summary>
    /// <param name="timeout">The duration in seconds.</param>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder WithTimeout(int timeout)
    {
        Timeout = timeout;
        return this;
    }

    /// <summary>
    ///     Sets whether the pagination response should be ephemeral.
    /// </summary>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder WithEphemeral(bool isEphemeral = true)
    {
        IsEphemeral = isEphemeral;
        return this;
    }

    /// <summary>
    ///     Adds a single embed to the pagination.
    /// </summary>
    /// <param name="embed">The embed to add.</param>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder AddEmbed(Embed embed)
    {
        Embeds.Add(embed);
        return this;
    }

    /// <summary>
    ///     Adds multiple embeds to the pagination.
    /// </summary>
    /// <param name="embeds">A collection of embeds to add.</param>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder AddEmbeds(IEnumerable<Embed> embeds)
    {
        Embeds.AddRange(embeds);
        return this;
    }

    /// <summary>
    ///     Adds content that will be automatically split into embeds.
    /// </summary>
    /// <param name="content">The content to add.</param>
    /// <param name="title">The title of the embeds.</param>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder AddContent(string content, string title)
    {
        Embeds.AddRange(BasePagination<ButtonPaginationManager>.GetEmbeds(content, title));
        return this;
    }

    /// <summary>
    ///     Adds an extra action row to appear below the navigation buttons.
    /// </summary>
    /// <param name="actionRow">The action row to add.</param>
    /// <returns>The current builder.</returns>
    public ButtonPaginationBuilder AddExtraActionRow(ActionRowBuilder actionRow)
    {
        ExtraActionRows.Add(actionRow);
        return this;
    }

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
            throw new InvalidOperationException(
                "At least one embed must be added before building."
            );
        }

        return new([.. Embeds], [.. ExtraActionRows], Timeout, IsEphemeral);
    }
}
