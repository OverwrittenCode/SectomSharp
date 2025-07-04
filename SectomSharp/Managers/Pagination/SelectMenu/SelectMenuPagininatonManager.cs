using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.Button;

namespace SectomSharp.Managers.Pagination.SelectMenu;

/// <summary>
///     Manages select menu-based pagination for Discord embeds, allowing users to navigate through multiple pages of content.
/// </summary>
internal sealed class SelectMenuPaginationManager : BasePagination<SelectMenuPaginationManager>
{
    /// <summary>
    ///     Handles pagination for the select menu components.
    /// </summary>
    /// <inheritdoc cref="ButtonPaginationManager.OnHit(SocketMessageComponent, String, PageNavigationButton)" />
    /// <param name="values">The selected values from the component.</param>
    public static async Task OnHit(SocketMessageComponent context, string id, string[] values)
    {
        try
        {
            SelectMenuPaginationManager instance = AllInstances[id];
            SelectMenuPaginatorPage page = instance._optionKvp.First(pair => pair.Key == values[0]).Value;

            MessageComponent? components = new ComponentBuilder { ActionRows = [.. page.ActionRows] }.Build();

            if (instance._responseType == SelectMenuResponse.Reply)
            {
                await context.RespondOrFollowupAsync(components: components, embeds: page.Embeds, ephemeral: instance.IsEphemeral);
            }
            else
            {
                await context.UpdateAsync(message =>
                    {
                        message.Components = components;
                        message.Embeds = page.Embeds;
                    }
                );
            }

            await instance.StartExpirationTimer(instance.Timeout);
        }
        catch (KeyNotFoundException)
        {
            await SendExpiredMessageAsync(context);
        }
    }

    private readonly SelectMenuPaginatorPage _firstPage;

    private readonly List<KeyValuePair<string, SelectMenuPaginatorPage>> _optionKvp;
    private readonly SelectMenuResponse _responseType;

    /// <summary>
    ///     Initialises a new instance of the <see cref="SelectMenuPaginationManager" /> class.
    /// </summary>
    /// <param name="selectMenuBuilder">The select menu builder.</param>
    /// <param name="optionKvp">The list of key value pair paginator pages.</param>
    /// <param name="replyType">The response type for the paginator.</param>
    /// <param name="isStickySelectMenu">
    ///     <c>true</c>; the action row is added to all pages.
    ///     <c>false</c>; the action row is added only to the first page.
    /// </param>
    /// <inheritdoc cref="BasePagination{T}(Int32, Boolean, global::System.String?)" />
    public SelectMenuPaginationManager(
        SelectMenuBuilder selectMenuBuilder,
        List<KeyValuePair<string, SelectMenuPaginatorPage>> optionKvp,
        SelectMenuResponse replyType,
        int timeout = 180,
        bool isEphemeral = false,
        bool isStickySelectMenu = false,
        string? id = null
    ) : base(timeout, isEphemeral, id)
    {
        SelectMenuBuilder selectMenuBuilder1 = selectMenuBuilder.WithComponentId<SelectMenuPaginationManager>(Id);
        _optionKvp = optionKvp;
        _responseType = replyType;
        _firstPage = _optionKvp.First().Value;

        var actionRow = new ActionRowBuilder { Components = [selectMenuBuilder1.Build()] };

        if (isStickySelectMenu)
        {
            foreach (KeyValuePair<string, SelectMenuPaginatorPage> pair in _optionKvp)
            {
                pair.Value.ActionRows.Insert(0, actionRow);
            }
        }
        else
        {
            _optionKvp[0].Value.ActionRows.Insert(0, actionRow);
        }
    }

    /// <inheritdoc />
    protected override async Task RespondOrFollowupAsync(SocketInteractionContext context)
        => await context.Interaction.RespondOrFollowupAsync(
            embeds: _firstPage.Embeds,
            components: new ComponentBuilder { ActionRows = _firstPage.ActionRows }.Build(),
            ephemeral: IsEphemeral
        );
}
