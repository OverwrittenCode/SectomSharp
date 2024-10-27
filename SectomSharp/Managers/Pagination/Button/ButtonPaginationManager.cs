using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Extensions;

namespace SectomSharp.Managers.Pagination.Button;

/// <summary>
///     Manages button-based pagination for Discord embeds, allowing users to navigate
///     through multiple pages of content.
/// </summary>
internal sealed class ButtonPaginationManager : BasePagination<ButtonPaginationManager>
{
    private static readonly PageNavigationButton[] PageNavigationButtons =
        Enum.GetValues<PageNavigationButton>();

    /// <summary>
    ///     Handles pagination for the button components.
    /// </summary>
    /// <param name="context">The message component context.</param>
    /// <param name="id">The <see cref="InstanceManager{T}.Id" />.</param>
    /// <param name="position">The <see cref="PageNavigationButton" />.</param>
    /// <inheritdoc cref="SocketMessageComponent.UpdateAsync(Action{MessageProperties}, RequestOptions)" path="/returns" />
    public static async Task OnHit(
        SocketMessageComponent context,
        string id,
        PageNavigationButton position
    )
    {
        try
        {
            ButtonPaginationManager instance = AllInstances[id];

            switch (position)
            {
                case PageNavigationButton.Start:
                    instance._currentPageIndex = 0;
                    break;
                case PageNavigationButton.End:
                    instance._currentPageIndex = instance._embeds.Length - 1;
                    break;
                case PageNavigationButton.Next:
                    instance._currentPageIndex++;
                    break;
                case PageNavigationButton.Previous:
                    instance._currentPageIndex--;
                    break;
                case PageNavigationButton.Exit:
                    await context.UpdateAsync(message => message.Components = null);
                    return;
            }

            await context.UpdateAsync(message =>
            {
                message.Components = instance.MessageComponent;
                message.Embeds = instance.CurrentEmbeds;
            });

            await instance.StartExpirationTimer(instance.Timeout);
        }
        catch (KeyNotFoundException)
        {
            await SendExpiredMessageAsync(context);
        }
    }

    private readonly Embed[] _embeds;

    private readonly ActionRowBuilder[] _extraActionRows;

    private int _currentPageIndex;

    /// <summary>
    ///     Gets an array containing only the current page's embed.
    /// </summary>
    private Embed[] CurrentEmbeds => [_embeds[_currentPageIndex]];

    /// <summary>
    ///     Gets a message component list containing of the button builders used to navigate pages.
    /// </summary>
    private List<IMessageComponent> ButtonComponents =>
        PageNavigationButtons
            .Select(
                IMessageComponent (pageNavigatorButton) =>
                    new ButtonBuilder()
                        .WithLabel(pageNavigatorButton.ToString())
                        .WithComponentId<ButtonPaginationManager>(Id, pageNavigatorButton)
                        .WithStyle(
                            pageNavigatorButton == PageNavigationButton.Exit
                                ? ButtonStyle.Danger
                                : ButtonStyle.Primary
                        )
                        .WithDisabled(
                            pageNavigatorButton switch
                            {
                                PageNavigationButton.Start or PageNavigationButton.Previous =>
                                    _currentPageIndex == 0,
                                PageNavigationButton.End or PageNavigationButton.Next =>
                                    _currentPageIndex == _embeds.Length - 1,
                                _ => false
                            }
                        )
                        .Build()
            )
            .ToList();

    /// <summary>
    ///     Gets the message component containing navigation buttons and extra action rows.
    /// </summary>
    private MessageComponent MessageComponent =>
        new ComponentBuilder
        {
            ActionRows =
            [
                new ActionRowBuilder().WithComponents(ButtonComponents),
                .. _extraActionRows
            ]
        }.Build();

    /// <param name="embeds">Array of embeds to paginate through.</param>
    /// <param name="extraActionRows">Optional additional action rows to include in the message.</param>
    /// <param name="timeout">The duration in seconds.</param>
    /// <param name="isEphemeral">If the pagination is ephemeral.</param>
    public ButtonPaginationManager(
        Embed[] embeds,
        ActionRowBuilder[]? extraActionRows = null,
        int timeout = 180,
        bool isEphemeral = false
    )
        : base(timeout, isEphemeral)
    {
        _embeds = embeds;
        _extraActionRows = extraActionRows ?? [];
    }

    /// <summary>
    ///     Initialises a new instance of the ButtonPaginationManager class using content that will be split into embeds.
    /// </summary>
    /// <param name="embedTitle">The title for all generated embeds.</param>
    /// <param name="content">The content to split into multiple embeds.</param>
    /// <param name="extraActionRows">Optional additional action rows to include in the message.</param>
    /// <param name="timeout">The duration in seconds.</param>
    /// <param name="isEphemeral">If the pagination should be ephemeral.</param>
    public ButtonPaginationManager(
        string embedTitle,
        string content,
        ActionRowBuilder[]? extraActionRows = null,
        int timeout = 180,
        bool isEphemeral = false
    )
        : this(GetEmbeds(content, embedTitle), extraActionRows, timeout, isEphemeral) { }

    protected override async Task RespondOrFollowupAsync(SocketInteractionContext context) =>
        await context.Interaction.RespondOrFollowupAsync(
            embeds: CurrentEmbeds,
            components: _embeds.Length == 1 ? null : MessageComponent,
            ephemeral: IsEphemeral
        );
}
