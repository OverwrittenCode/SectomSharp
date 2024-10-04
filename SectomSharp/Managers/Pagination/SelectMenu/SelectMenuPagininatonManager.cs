using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Managers.Pagination.SelectMenu;

/// <summary>
///     Manages select menu-based pagination for Discord embeds, allowing users to navigate
///     through multiple pages of content.
/// </summary>
internal sealed class SelectMenuPaginationManager : BasePagination<SelectMenuPaginationManager>
{
    /// <summary>
    ///     Handles pagination for the select menu components.
    /// </summary>
    /// <inheritdoc cref="ButtonPaginationManager.OnHit(SocketMessageComponent, String, PageNavigationButton)"/>
    /// <param name="values">The selected values from the component.</param>
    public static async Task OnHit(SocketMessageComponent context, string id, string[] values)
    {
        try
        {
            var instance = AllInstances[id];
            SelectMenuPaginatorPage page = instance
                ._optionKvp.First(pair => pair.Key == values[0])
                .Value;

            var components = new ComponentBuilder() { ActionRows = [.. page.ActionRows] }.Build();

            if (instance._responseType == SelectMenuResponse.Reply)
            {
                await context.RespondAsync(
                    components: components,
                    embeds: page.Embeds,
                    ephemeral: instance.IsEphemeral
                );
            }
            else
            {
                await context.UpdateAsync(message =>
                {
                    message.Components = components;
                    message.Embeds = page.Embeds;
                });
            }

            await instance.StartExpirationTimer(instance.Timeout);
        }
        catch (KeyNotFoundException)
        {
            await context.RespondAsync(PaginationExpiredMessage, ephemeral: true);
        }
    }

    private readonly SelectMenuBuilder _selectMenuBuilder;
    private readonly List<KeyValuePair<string, SelectMenuPaginatorPage>> _optionKvp;
    private readonly SelectMenuResponse _responseType;
    private readonly SelectMenuPaginatorPage _firstPage;
    private readonly bool _isStickySelectMenu;

    /// <summary>
    ///     Initialises a new instance of the <see cref="SelectMenuPaginationManager"/> class.
    /// </summary>
    /// <param name="selectMenuBuilder">The select menu builder.</param>
    /// <param name="optionKvp">The list of key value pair paginator pages.</param>
    /// <param name="replyType">The response type for the paginator.</param>
    /// <param name="isStickySelectMenu">
    ///     The action row of <paramref name="selectMenuBuilder"/> should be added to
    ///     the start of
    ///     <see langword="true"/>; all pages
    ///     <see langword="false"/>; the first page
    /// </param>
    /// <inheritdoc cref="BasePagination{T}.BasePagination(Int32, Boolean, global::System.String?)"/>
    public SelectMenuPaginationManager(
        SelectMenuBuilder selectMenuBuilder,
        List<KeyValuePair<string, SelectMenuPaginatorPage>> optionKvp,
        SelectMenuResponse replyType,
        int timeout = 180,
        bool isEphemeral = false,
        bool isStickySelectMenu = false,
        string? id = null
    )
        : base(timeout, isEphemeral, id)
    {
        _selectMenuBuilder = selectMenuBuilder.WithComponentId<SelectMenuPaginationManager>(Id);
        _optionKvp = optionKvp;
        _responseType = replyType;
        _firstPage = _optionKvp.First().Value;
        _isStickySelectMenu = isStickySelectMenu;

        var actionRow = new ActionRowBuilder() { Components = [_selectMenuBuilder.Build()] };

        if (_isStickySelectMenu)
        {
            for (var i = 0; i < _optionKvp.Count; i++)
            {
                _optionKvp[i].Value.ActionRows.Insert(0, actionRow);
            }
        }
        else
        {
            _optionKvp[0].Value.ActionRows.Insert(0, actionRow);
        }
    }

    protected override async Task RespondAsync(SocketInteractionContext context) =>
        await context.Interaction.RespondAsync(
            embeds: _firstPage.Embeds,
            components: new ComponentBuilder()
            {
                ActionRows = _firstPage.ActionRows,
                //_isStickySelectMenu // Component custom id cannot be duplicated
                //    ? _firstPage.ActionRows
                //    :
                //    [
                //        new ActionRowBuilder() { Components = [SelectMenuBuilder.Build()] },
                //        .. _firstPage.ActionRows,
                //    ],
            }.Build(),
            ephemeral: IsEphemeral
        );
}
