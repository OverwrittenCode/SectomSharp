using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    /// <summary>
    ///     Asynchronously waits for two given users to interact with a button or select menu.
    /// </summary>
    /// <param name="componentType">The type of component event to listen to.</param>
    /// <param name="message">The message of the interaction.</param>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="components">The components to use.</param>
    /// <param name="playerTwoId">The other player's id.</param>
    /// <param name="processPlayerChoice">Executed when either player makes their choice.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    /// <returns>The data for both players, or <c>null</c> if at least one player did not provide a choice in time.</returns>
    private async Task<MultiPlayerChoice<T>?> ObtainMultiPlayerChoiceAsync<T>(
        ComponentType componentType,
        IUserMessage message,
        EmbedBuilder embedBuilder,
        MessageComponent components,
        ulong playerTwoId,
        [RequireStaticDelegate] DualInputProcessPlayerChoice<T> processPlayerChoice
    )
        where T : struct
    {
        embedBuilder.WithDescription("Both players select your choice below.");
        await ModifyOriginalResponseAsync(properties =>
            {
                properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]);
                properties.Components = components;
            }
        );

        var tcs = new TaskCompletionSource<MultiPlayerChoice<T>>(TaskCreationOptions.RunContinuationsAsynchronously);
        T? playerOne = null;
        T? playerTwo = null;

        if (await TryWaitForComponentResponseAsync(componentType, tcs, OnMessageComponentExecuted))
        {
            (T userChoice, T opponentChoice) = tcs.Task.Result;
            return new MultiPlayerChoice<T>(userChoice, opponentChoice);
        }

        await ModifyOriginalResponseWithErrorEmbedAsync(embedBuilder, message, "Both players did not select their choices in time.");
        return null;

        async Task OnMessageComponentExecuted(SocketMessageComponent component)
        {
            if (component.Message.Id != message.Id || tcs.Task.IsCompleted)
            {
                return;
            }

            await component.DeferAsync(ephemeral: true);

            if (component.User.Id == Context.User.Id)
            {
                if (playerOne != null)
                {
                    await component.FollowupAsync(ChoiceAlreadyLockedMessage, ephemeral: true);
                    return;
                }

                playerOne = processPlayerChoice(component);
                await component.FollowupAsync(ChoiceLockedMessage, ephemeral: true);
            }
            else if (component.User.Id == playerTwoId)
            {
                if (playerTwo != null)
                {
                    await component.FollowupAsync(ChoiceAlreadyLockedMessage, ephemeral: true);
                    return;
                }

                playerTwo = processPlayerChoice(component);
                await component.FollowupAsync(ChoiceLockedMessage, ephemeral: true);
            }
            else
            {
                await component.FollowupAsync(ComponentNotForYou, ephemeral: true);
                return;
            }

            if (playerOne.HasValue && playerTwo.HasValue)
            {
                tcs.TrySetResult(new MultiPlayerChoice<T>(playerOne.Value, playerTwo.Value));
            }
        }
    }

    /// <summary>
    ///     Represents a structure for the choices chosen by two players for the current round.
    /// </summary>
    /// <param name="PlayerOne">The first player.</param>
    /// <param name="PlayerTwo">The second player.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    private record struct MultiPlayerChoice<T>(T PlayerOne, T PlayerTwo)
        where T : struct;
}
