using System.ComponentModel;
using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.Rest;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Managers.Pagination;

/// <summary>
///     Represents a base class for pagination functionality with shared utility methods.
///     Provides common functionality for handling paginated content in Discord interactions.
/// </summary>
/// <typeparam name="T">
///     The type of the implementing pagination manager.
///     Must inherit from <see cref="InstanceManager{T}" />.
/// </typeparam>
internal abstract class BasePagination<T> : InstanceManager<T>
    where T : InstanceManager<T>
{
    /// <summary>
    ///     The maximum number of items to include in each chunk when splitting content.
    ///     Used when content exceeds Discord's maximum embed description length.
    /// </summary>
    private const int ChunkSize = 10;

    public const string PaginationExpiredMessage = "The pagination for this message has expired.";

    private static Embed[] ToSanitisedEmbeds(string title, List<string> chunks)
    {
        if (chunks.Count == 1)
        {
            return [GetEmbedBuilder(chunks[0], title).Build()];
        }

        List<Embed> embeds = chunks.Select((t, i) => GetEmbedBuilder(t, title).WithFooter(builder => builder.WithText($"Page {i + 1} / {chunks.Count}")).Build()).ToList();

        return [.. embeds];
    }

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
            Color = Constants.LightGold
        };

    protected static async Task SendExpiredMessageAsync(IDiscordInteraction interaction) => await interaction.RespondOrFollowupAsync(PaginationExpiredMessage, ephemeral: true);

    /// <summary>
    ///     Creates an array of embeds by splitting <paramref name="strings" /> into chunks of <see cref="ChunkSize" />.
    /// </summary>
    /// <param name="strings">The strings to split into chunks.</param>
    /// <param name="title">The title of each embed.</param>
    /// <returns>An array of embed objects.</returns>
    /// <exception cref="InvalidOperationException">
    ///     A chunk of <paramref name="strings" /> exceeds <see cref="EmbedBuilder.MaxDescriptionLength" />
    /// </exception>
    public static Embed[] GetEmbeds(List<string> strings, string title)
    {
        var chunks = new List<string>();

        for (int i = 0; i < strings.Count; i += ChunkSize)
        {
            string chunk = String.Join("\n", strings.GetRange(i, Math.Min(ChunkSize, strings.Count - i)));

            ArgumentOutOfRangeException.ThrowIfGreaterThan(chunk.Length, EmbedBuilder.MaxDescriptionLength);

            chunks.Add(chunk);
        }

        return ToSanitisedEmbeds(title, chunks);
    }

    /// <summary>
    ///     Gets the time in seconds to wait before invoking <see cref="CleanupAsync" />.
    /// </summary>
    protected int Timeout { get; }

    /// <summary>
    ///     Gets whether the pagination response should be ephemeral.
    /// </summary>
    protected bool IsEphemeral { get; }

    /// <summary>
    ///     Gets the original message structure from
    ///     <see cref="Discord.WebSocket.SocketInteraction.GetOriginalResponseAsync(RequestOptions)" />.
    /// </summary>
    /// <value>
    ///     <see langword="null" /> if <see cref="Init(SocketInteractionContext)" />
    ///     has not been called yet.
    /// </value>
    public RestInteractionMessage? Message { get; protected set; }

    /// <summary>
    ///     Initialises a new instance of the <typeparamref name="T" /> class with specified visibility settings.
    /// </summary>
    /// <param name="isEphemeral">
    ///     <see langword="true" />;
    ///     the pagination response will only be visible to the user
    ///     who triggered the interaction.<br />
    ///     <see langword="false" />;
    ///     the response will be visible to
    ///     all users in the channel.
    /// </param>
    /// <param name="timeout">
    ///     The time in seconds to wait before invoking <see cref="CleanupAsync" />.
    /// </param>
    /// <inheritdoc cref="InstanceManager{T}(global::System.String?)" path="/param" />
    /// <exception cref="ArgumentOutOfRangeException">Timeout is less than 0.</exception>
    protected BasePagination(int timeout, bool isEphemeral = false, string? id = null) : base(id)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeout);

        Timeout = timeout;
        IsEphemeral = isEphemeral;
    }

    /// <summary>
    ///     Responds or follows up a Discord interaction with the initial pagination state.
    /// </summary>
    /// <param name="context">
    ///     The interaction context containing information about the Discord interaction
    ///     that triggered the pagination.
    /// </param>
    /// <returns>
    ///     A task representing the asynchronous operation of
    ///     responding or following up the interaction.
    /// </returns>
    protected abstract Task RespondOrFollowupAsync(SocketInteractionContext context);

    /// <summary>
    ///     Removes all the components in <see cref="Message" />.
    /// </summary>
    /// <inheritdoc />
    /// <inheritdoc cref="RestInteractionMessage.ModifyAsync(Action{MessageProperties}, RequestOptions)" path="/exception" />
    /// <exception cref="InvalidOperationException"><see cref="Message" /> is <see langword="null" /></exception>
    protected sealed override async Task CleanupAsync()
    {
        if (Message is null)
        {
            throw new InvalidOperationException($"{nameof(Message)} is null as it has not been called yet with {nameof(RespondOrFollowupAsync)}");
        }

        try
        {
            ComponentBuilder componentBuilder = ComponentBuilder.FromMessage(Message);

            foreach (ActionRowBuilder actionRow in componentBuilder.ActionRows)
            {
                for (int i = 0; i < actionRow.Components.Count; i++)
                {
                    IMessageComponent component = actionRow.Components[i];

                    switch (component.Type)
                    {
                        case ComponentType.Button:
                            {
                                ButtonBuilder builder = ((ButtonComponent)component).ToBuilder();
                                builder.IsDisabled = true;
                                actionRow.Components[i] = builder.Build();
                            }

                            break;

                        case ComponentType.SelectMenu:
                            {
                                SelectMenuBuilder builder = ((SelectMenuComponent)component).ToBuilder();
                                builder.IsDisabled = true;
                                actionRow.Components[i] = builder.Build();
                            }

                            break;

                        default:
                            throw new InvalidEnumArgumentException(nameof(component.Type), (int)component.Type, typeof(ComponentType));
                    }
                }
            }

            MessageComponent components = componentBuilder.Build();

            await Message.ModifyAsync(
                props =>
                {
                    props.Components = components;
                }
            );
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage) { }
    }

    /// <summary>
    ///     Starts the pagination.
    /// </summary>
    /// <inheritdoc cref="RespondOrFollowupAsync(SocketInteractionContext)" />
    public async Task Init(SocketInteractionContext context)
    {
        await RespondOrFollowupAsync(context);

        Message = await context.Interaction.GetOriginalResponseAsync();

        await RestartTimer();
    }

    /// <summary>
    ///     Restarts the expiration timer for this instance.
    /// </summary>
    /// <inheritdoc cref="InstanceManager{T}.ThrowIfDisposed" path="/exception" />
    public async Task RestartTimer() => await StartExpirationTimer(Timeout);
}
