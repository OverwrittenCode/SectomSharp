using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using StrongInteractions.Generated;

namespace SectomSharp.Managers.Pagination.Button;

/// <summary>
///     Manages button-based pagination for Discord embeds, allowing users to navigate through multiple pages of content.
/// </summary>
internal sealed class ButtonPaginationManager : InstanceManager<ButtonPaginationManager>
{
    public const int ChunkSize = 10;

    private static readonly PageNavigationButton[] PageNavigationButtons = Enum.GetValues<PageNavigationButton>();

    /// <summary>
    ///     Handles pagination for the button components.
    /// </summary>
    /// <param name="context">The message component context.</param>
    /// <param name="id">The <see cref="InstanceManager{T}.InteractionId" />.</param>
    /// <param name="position">The <see cref="PageNavigationButton" />.</param>
    /// <returns>A task representing the asynchronous operation of updating a message.</returns>
    public static async Task OnHit(SocketMessageComponent context, ulong id, PageNavigationButton position)
    {
        if (await TryAcquireSessionAndDeferAsync(context, id) is not { } instance)
        {
            return;
        }

        try
        {
            switch (position)
            {
                case PageNavigationButton.Start:
                    instance._currentPageIndex = 0;
                    break;
                case PageNavigationButton.End:
                    instance._currentPageIndex = instance.Embeds.Length - 1;
                    break;
                case PageNavigationButton.Next:
                    instance._currentPageIndex++;
                    break;
                case PageNavigationButton.Previous:
                    instance._currentPageIndex--;
                    break;

                case PageNavigationButton.Exit:
                    if (!instance.TryComplete())
                    {
                        return;
                    }

                    await instance.DisableMessageComponentsAsync();
                    return;
            }

            if (!instance.TryExtend())
            {
                return;
            }

            await instance.ModifyMessageAsync(message =>
                {
                    message.Components = instance.MessageComponent;
                    message.Embeds = instance.CurrentEmbeds;
                }
            );

            instance.TryReleaseSession();
        }
        catch (Exception ex)
        {
            await instance.TryCompleteAndThrowAsync(ex);
        }
    }

    private int _currentPageIndex;

    /// <summary>
    ///     Gets an array containing only the current page's embed.
    /// </summary>
    private Embed[] CurrentEmbeds => [Embeds[_currentPageIndex]];

    /// <summary>
    ///     Gets a message component list containing of the button builders used to navigate pages.
    /// </summary>
    private List<IMessageComponent> ButtonComponents
        => PageNavigationButtons.Select(IMessageComponent (pageNavigatorButton) =>
                                     {
                                         string label = pageNavigatorButton.ToString();
                                         return new ButtonBuilder(
                                             label,
                                             StrongInteractionIds.Button(InteractionId, pageNavigatorButton),
                                             pageNavigatorButton == PageNavigationButton.Exit ? ButtonStyle.Danger : ButtonStyle.Primary,
                                             isDisabled: pageNavigatorButton switch
                                             {
                                                 PageNavigationButton.Start or PageNavigationButton.Previous => _currentPageIndex == 0,
                                                 PageNavigationButton.End or PageNavigationButton.Next => _currentPageIndex == Embeds.Length - 1,
                                                 _ => false
                                             }
                                         ).Build();
                                     }
                                 )
                                .ToList();

    /// <summary>
    ///     Gets the message component containing navigation buttons and extra action rows.
    /// </summary>
    private MessageComponent MessageComponent
        => new ComponentBuilder
        {
            ActionRows =
            [
                new ActionRowBuilder { Components = ButtonComponents }
            ]
        }.Build();

    /// <summary>
    ///     Gets the array of embeds to paginate through.
    /// </summary>
    public required Embed[] Embeds { get; init; }

    /// <inheritdoc />
    public ButtonPaginationManager(ILoggerFactory loggerFactory, SocketInteractionContext context) : base(loggerFactory, context) { }

    /// <inheritdoc />
    protected override Task<RestFollowupMessage> FollowupWithInitialResponseAsync(SocketInteractionContext context)
        => context.Interaction.FollowupAsync(embeds: CurrentEmbeds, components: Embeds.Length == 1 ? null : MessageComponent, ephemeral: IsEphemeral);
}
