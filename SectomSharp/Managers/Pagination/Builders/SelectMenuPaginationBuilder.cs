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
    private const int Timeout = 180;
    
    private readonly string _instanceId = StringUtils.GenerateUniqueId();

    /// <summary>
    ///     Gets the builder for the select menu component.
    /// </summary>
    private SelectMenuBuilder SelectMenuBuilder { get; }

    /// <summary>
    ///     Gets a list of key-value pairs representing options and corresponding pages.
    /// </summary>
    private List<KeyValuePair<string, SelectMenuPaginatorPage>> OptionKvp { get; } = [];

    /// <summary>
    ///     Gets the list of select menu options.
    /// </summary>
    private List<SelectMenuPageOption> Options { get; } = [];

    /// <summary>
    ///     Gets or sets the response type for the select menu pagination.
    /// </summary>
    private SelectMenuResponse ResponseType { get; set; } = SelectMenuResponse.Update;

    /// <summary>
    ///     Gets or sets a value indicating whether the first row of the first option should be sticky across all pages.
    /// </summary>
    private bool IsStickyFirstRow { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the pagination response should be ephemeral.
    /// </summary>
    private bool IsEphemeral { get; set; }

    /// <summary>
    ///     Initialises a new instance of the <see cref="SelectMenuPaginationBuilder" /> class.
    /// </summary>
    /// <param name="placeholder">The placeholder text for the select menu.</param>
    public SelectMenuPaginationBuilder(string placeholder = "Select an item")
        => SelectMenuBuilder = new SelectMenuBuilder().WithPlaceholder(placeholder).WithMinValues(1).WithMaxValues(1);

    /// <summary>
    ///     Adds a single option to the pagination.
    /// </summary>
    /// <param name="option">The select menu option to add.</param>
    private void AddOption(SelectMenuPageOption option)
    {
        Options.Add(option);
        SelectMenuBuilder.AddOption(option.Label, option.Value, option.Description, option.Emote);
        OptionKvp.Add(new KeyValuePair<string, SelectMenuPaginatorPage>(option.Value, new SelectMenuPaginatorPage(option.Embeds, option.ActionRows)));
    }

    /// <summary>
    ///     Sets whether the first row of the first option
    ///     should be added to the start of all pages in the pagination.
    /// </summary>
    /// <returns>The current builder.</returns>
    private void WithStickyFirstRow(bool isStickyFirstRow = true) => IsStickyFirstRow = isStickyFirstRow;

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
    ///     Creates a nested menu structure with categories and pages.
    /// </summary>
    /// <typeparam name="TCategory">The type representing categories.</typeparam>
    /// <typeparam name="TPage">The type representing items.</typeparam>
    /// <param name="groupedItems">The grouped items to categorise.</param>
    /// <param name="categoryConfig">Configuration for category display.</param>
    /// <param name="itemConfig">Configuration for item display.</param>
    /// <param name="menuConfig">Configuration for the overall menu structure.</param>
    /// <param name="prependIdWithInstanceId">
    ///     If <see cref="InstanceManager{T}.Id" /> should be added to the start of
    ///     the second menu's <see cref="SelectMenuBuilder.CustomId" />
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
                    new EmbedBuilder
                    {
                        Title = menuConfig.EmbedTitle,
                        Color = menuConfig.EmbedColour,
                        Description = "Select a category from the menu below to view its contents"
                    }.Build()
                ]
            }
        );

        foreach (IGrouping<TCategory, TPage> group in groupedItems)
        {
            TCategory category = group.Key;
            string categoryName = categoryConfig.GetName(category);
            string categoryValue = categoryConfig.GetValue(category);

            SelectMenuBuilder selectMenu = new SelectMenuBuilder().WithOptions(
                group.Select(
                          item => new SelectMenuOptionBuilder
                          {
                              Label = itemConfig.GetLabel(item),
                              Value = itemConfig.GetValue(item),
                              Description = itemConfig.GetDescription?.Invoke(item)
                          }
                      )
                     .ToList()
            );

            selectMenu.WithComponentId(
                categoryConfig.CustomIdPrefix,
                (prependIdWithInstanceId ? categoryConfig.GetCustomIdWildcards(category).Prepend(_instanceId) : categoryConfig.GetCustomIdWildcards(category)).ToArray<object>()
            );

            AddOption(
                new SelectMenuPageOption
                {
                    Label = categoryName,
                    Value = categoryValue,
                    Emote = categoryConfig.GetEmote?.Invoke(category),
                    Description = categoryConfig.GetDescription?.Invoke(category),
                    Embeds =
                    [
                        new EmbedBuilder().WithTitle($"{menuConfig.EmbedTitle} | {categoryName}")
                                          .WithColor(menuConfig.EmbedColour)
                                          .WithDescription("Select an option below to view details")
                                          .Build()
                    ],
                    ActionRows = [new ActionRowBuilder().AddComponent(selectMenu.Build())]
                }
            );
        }

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

    /// <exception cref="InvalidOperationException">Empty list of options.</exception>
    public SelectMenuPaginationManager Build()
    {
        if (Timeout <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(Timeout), "Value must be greater than 0.");
        }

        if (Options.Count == 0)
        {
            throw new InvalidOperationException("At least one option must be added before building.");
        }

        return new SelectMenuPaginationManager(SelectMenuBuilder, OptionKvp, ResponseType, Timeout, IsEphemeral, IsStickyFirstRow, _instanceId);
    }
}
