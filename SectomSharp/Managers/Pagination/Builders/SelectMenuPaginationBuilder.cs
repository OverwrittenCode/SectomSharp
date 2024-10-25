using Discord;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Models;
using SectomSharp.Managers.Pagination.SelectMenu;
using SectomSharp.Utils;

namespace SectomSharp.Managers.Pagination.Builders;

/// <summary>
///     Builder for creating select menu-based pagination instances with support for nested menus.
/// </summary>
internal sealed class SelectMenuPaginationBuilder
{
    private readonly string _instanceId = StringUtils.GenerateUniqueId();

    /// <summary>
    ///     Gets or sets the builder for the select menu component.
    /// </summary>
    public SelectMenuBuilder SelectMenuBuilder { get; set; }

    /// <summary>
    ///     Gets or sets a list of key-value pairs representing options and corresponding pages.
    /// </summary>
    public List<KeyValuePair<string, SelectMenuPaginatorPage>> OptionKvp { get; set; } = [];

    /// <summary>
    ///     Gets or sets the list of select menu options.
    /// </summary>
    public List<SelectMenuPageOption> Options { get; set; } = [];

    /// <summary>
    ///     Gets or sets the response type for the select menu pagination.
    /// </summary>
    public SelectMenuResponse ResponseType { get; set; } = SelectMenuResponse.Update;

    /// <summary>
    ///     Gets or sets a value indicating whether the first row of the first option should be sticky across all pages.
    /// </summary>
    public bool IsStickyFirstRow { get; set; }

    /// <summary>
    ///     Gets or sets the timeout duration for the pagination in seconds.
    /// </summary>
    public int Timeout { get; set; } = 180;

    /// <summary>
    ///     Gets or sets a value indicating whether the pagination response should be ephemeral.
    /// </summary>
    public bool IsEphemeral { get; set; }

    /// <summary>
    ///     Sets the timeout duration for the pagination in seconds.
    /// </summary>
    /// <param name="timeout"></param>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder WithTimeout(int timeout)
    {
        Timeout = timeout;
        return this;
    }

    /// <summary>
    ///     Sets whether the pagination response should be ephemeral.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder WithEphemeral(bool isEphemeral = true)
    {
        IsEphemeral = isEphemeral;
        return this;
    }

    /// <summary>
    ///     Initialises a new instance of the <see cref="SelectMenuPaginationBuilder"/> class.
    /// </summary>
    /// <param name="placeholder">The placeholder text for the select menu.</param>
    public SelectMenuPaginationBuilder(string placeholder = "Select an item")
    {
        SelectMenuBuilder = new SelectMenuBuilder()
            .WithPlaceholder(placeholder)
            .WithMinValues(1)
            .WithMaxValues(1);
    }

    /// <summary>
    ///     Adds multiple options to the pagination.
    /// </summary>
    /// <param name="options">A collection of select menu options.</param>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder AddOptions(IEnumerable<SelectMenuPageOption> options)
    {
        foreach (var option in options)
        {
            AddOption(option);
        }

        return this;
    }

    /// <summary>
    ///     Adds a single option to the pagination.
    /// </summary>
    /// <param name="option">The select menu option to add.</param>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder AddOption(SelectMenuPageOption option)
    {
        Options.Add(option);
        SelectMenuBuilder.AddOption(option.Label, option.Value, option.Description, option.Emote);
        OptionKvp.Add(new(option.Value, new(option.Embeds, option.ActionRows)));
        return this;
    }

    /// <summary>
    ///     Creates a nested menu structure with categories and pages.
    /// </summary>
    /// <typeparam name="TCategory">The type representing categories.</typeparam>
    /// <typeparam name="TPage">The type representing items.</typeparam>
    /// <param name="groupedItems">The grouped items to categorise.</param>
    /// <param name="categoryConfig">Configuration for category display.</param>
    /// <param name="itemConfig">Configuration for item display.</param>
    /// <param name="menuConfig">Configuration for the overall menu structure.</param>
    /// <param name="prependIdWithInstanceId">
    ///     If <see cref="InstanceManager{T}.Id"/> should be added to the start of
    ///     the second menu's <see cref="SelectMenuBuilder.CustomId"/>
    /// </param>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder AddNestedMenu<TCategory, TPage>(
        IEnumerable<IGrouping<TCategory, TPage>> groupedItems,
        CategoryConfig<TCategory> categoryConfig,
        PageConfig<TPage> itemConfig,
        NestedMenuConfig menuConfig,
        bool prependIdWithInstanceId = true
    )
    {
        WithStickyFirstRow();

        AddOption(
            new SelectMenuPageOption
            {
                Label = "Home",
                Value = "home",
                Emote = new Emoji("🏠"),
                Description = "Return to main menu",
                Embeds =
                [
                    new EmbedBuilder()
                        .WithTitle(menuConfig.EmbedTitle)
                        .WithColor(menuConfig.EmbedColour)
                        .WithDescription(menuConfig.HomePageDescription)
                        .Build(),
                ],
            }
        );

        foreach (var group in groupedItems)
        {
            var category = group.Key;
            var categoryName = categoryConfig.GetName(category);

            var selectMenu = new SelectMenuBuilder().WithOptions(
                group
                    .Select(item => new SelectMenuOptionBuilder
                    {
                        Label = itemConfig.GetLabel(item),
                        Value = itemConfig.GetValue(item),
                        Description = itemConfig.GetDescription?.Invoke(item),
                        Emote = itemConfig.GetEmote?.Invoke(item),
                    })
                    .ToList()
            );

            if (prependIdWithInstanceId)
            {
                selectMenu.WithComponentId(
                    categoryConfig.CustomIdPrefix,
                    categoryConfig.GetCustomIdWildcards(category).Prepend(_instanceId).ToArray()
                );
            }
            else
            {
                selectMenu.WithComponentId(
                    categoryConfig.CustomIdPrefix,
                    categoryConfig.GetCustomIdWildcards(category)
                );
            }

            AddOption(
                new SelectMenuPageOption
                {
                    Label = categoryName,
                    Value = categoryName,
                    Emote = categoryConfig.GetEmote?.Invoke(category),
                    Description = categoryConfig.GetDescription?.Invoke(category),
                    Embeds =
                    [
                        new EmbedBuilder()
                            .WithTitle($"{menuConfig.EmbedTitle} | {categoryName}")
                            .WithColor(menuConfig.EmbedColour)
                            .WithDescription(menuConfig.CategoryDescription)
                            .Build(),
                    ],
                    ActionRows = [new ActionRowBuilder().AddComponent(selectMenu.Build())],
                }
            );
        }

        return this;
    }

    /// <summary>
    ///     Sets whether the select menu is disabled.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder WithDisabled(bool disabled = true)
    {
        SelectMenuBuilder.WithDisabled(disabled);
        return this;
    }

    /// <summary>
    ///     Sets the response type for the pagination.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder WithResponseType(SelectMenuResponse responseType)
    {
        ResponseType = responseType;
        return this;
    }

    /// <summary>
    ///     Sets whether the the first row of the first option
    ///     should be added to the start of all pages in the pagination.
    /// </summary>
    /// <returns>The current builder.</returns>
    public SelectMenuPaginationBuilder WithStickyFirstRow(bool isStickyFirstRow = true)
    {
        IsStickyFirstRow = isStickyFirstRow;
        return this;
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Empty list of options.</exception>
    public SelectMenuPaginationManager Build()
    {
        if (Timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Value must be greater than 0.");
        }

        if (Options.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one option must be added before building."
            );
        }

        return new(
            SelectMenuBuilder,
            OptionKvp,
            ResponseType,
            Timeout,
            IsEphemeral,
            IsStickyFirstRow,
            _instanceId
        );
    }
}
