using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    /// <summary>
    ///     Executed when either player makes their choice.
    /// </summary>
    /// <param name="components">The components.</param>
    /// <param name="playerOne">The data for player one.</param>
    /// <param name="playerTwo">The data for player two.</param>
    /// <param name="currentPlayer">The data for the current player.</param>
    /// <param name="moveCounter">The total number of moves including the current move.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    /// <returns>The outcome of the choice and the new components.</returns>
    private delegate TurnInputChoiceResult TurnInputProcessPlayerChoice<T>(
        SocketMessageComponent components,
        TurnInputPlayer<T> playerOne,
        TurnInputPlayer<T> playerTwo,
        TurnInputPlayer<T> currentPlayer,
        int moveCounter
    )
        where T : struct;

    /// <summary>
    ///     Executed when it is the computer's turn.
    /// </summary>
    /// <param name="components">The components.</param>
    /// <param name="computer">The data for the computer.</param>
    /// <param name="user">The data for the user.</param>
    /// <param name="moveCounter">The total number of moves including the current move.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    /// <returns>The outcome of the choice and the new components.</returns>
    private delegate TurnInputChoiceResult TurnInputProvideComputerChoice<T>(
        IReadOnlyCollection<IMessageComponent> components,
        TurnInputPlayer<T> computer,
        TurnInputPlayer<T> user,
        int moveCounter
    )
        where T : struct;

    /// <summary>
    ///     Represents the type of player in a turn based game.
    /// </summary>
    private enum PlayerType
    {
        One = 0,
        Two = 1
    }

    /// <summary>
    ///     Handles the logic for a turn input game session.
    /// </summary>
    /// <param name="componentType">The type of component event to listen to.</param>
    /// <param name="gameTitle">The game title used for the embed.</param>
    /// <param name="opponent">The opponent.</param>
    /// <param name="components">The components used for the game.</param>
    /// <param name="processPlayerChoice">Executed when either player makes their choice.</param>
    /// <param name="provideComputerChoice">Executed when it is the computer's turn.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    private async Task HandleTurnInputGameSession<T>(
        ComponentType componentType,
        string gameTitle,
        IGuildUser? opponent,
        MessageComponent components,
        [RequireStaticDelegate] TurnInputProcessPlayerChoice<T> processPlayerChoice,
        [RequireStaticDelegate] TurnInputProvideComputerChoice<T> provideComputerChoice
    )
        where T : struct
    {
        Color color;
        string summary;
        RoundOutcome roundOutcome;
        int moveCounter = 0;
        IUserMessage message;

        var embedBuilder = new EmbedBuilder();
        if (opponent?.IsBot == false)
        {
            if (await RequestOpponentToPlayAsync(embedBuilder, opponent, gameTitle) is not { } userMessage)
            {
                return;
            }

            message = userMessage;

            var playerOne = new TurnInputPlayer<T>(PlayerType.One, Context.User.Id);
            var playerTwo = new TurnInputPlayer<T>(PlayerType.Two, opponent.Id);

            TurnInputPlayer<T> currentPlayer;

            bool isPlayerOneTurn = true;

            while (true)
            {
                currentPlayer = isPlayerOneTurn ? playerOne : playerTwo;

                string currentPlayerMention = MentionUtils.MentionUser(currentPlayer.Id);
                embedBuilder.WithDescription($"Turn: {currentPlayerMention}");
                await ModifyOriginalResponseAsync(properties =>
                    {
                        properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]);
                        properties.Components = components;
                    }
                );

                var tcs = new TaskCompletionSource<TurnInputChoiceResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!await TryWaitForComponentResponseAsync(
                        componentType,
                        tcs,
                        async component =>
                        {
                            if (component.Message.Id != message.Id)
                            {
                                return;
                            }

                            await component.DeferAsync();

                            if (component.User.Id != currentPlayer.Id)
                            {
                                await component.FollowupAsync(ComponentNotForYou, ephemeral: true);
                                return;
                            }

                            tcs.TrySetResult(processPlayerChoice(component, playerOne, playerTwo, currentPlayer, ++moveCounter));
                        }
                    ))
                {
                    await ModifyOriginalResponseWithErrorEmbedAsync(embedBuilder, message, $"{currentPlayerMention} did not select a choice in time.");
                    return;
                }

                TurnInputChoiceResult choiceResult = tcs.Task.Result;
                components = choiceResult.NewComponents;
                message = await ModifyOriginalResponseAsync(properties => properties.Components = components);

                if (choiceResult.Outcome is { } result)
                {
                    roundOutcome = result;
                    break;
                }

                isPlayerOneTurn ^= true;
            }

            GetRoundOutcomeData(opponent, roundOutcome, out color, out summary);
        }
        else
        {
            const string yourTurnMessage = "It's your turn!";

            var user = new TurnInputPlayer<T>(PlayerType.One, Context.User.Id);
            var computer = new TurnInputPlayer<T>(PlayerType.Two, Context.Client.CurrentUser.Id);
            bool isUserTurn = true;

            embedBuilder.WithColor(Color.Purple).WithTitle(gameTitle).WithDescription(yourTurnMessage);

            await DeferAsync(true);
            message = await FollowupAsync(embeds: [embedBuilder.Build()], components: components);

            while (true)
            {
                if (isUserTurn)
                {
                    var tcs = new TaskCompletionSource<TurnInputChoiceResult>(TaskCreationOptions.RunContinuationsAsynchronously);
                    if (!await TryWaitForComponentResponseAsync(
                            componentType,
                            tcs,
                            async component =>
                            {
                                if (component.Message.Id == message.Id)
                                {
                                    await component.DeferAsync();
                                    tcs.TrySetResult(processPlayerChoice(component, user, computer, user, ++moveCounter));
                                }
                            }
                        ))
                    {
                        await ModifyOriginalResponseWithErrorEmbedAsync(embedBuilder, message, "You did not select a choice in time.");
                        return;
                    }

                    TurnInputChoiceResult choiceResult = tcs.Task.Result;
                    components = choiceResult.NewComponents;
                    message = await ModifyOriginalResponseAsync(properties => properties.Components = components);

                    if (choiceResult.Outcome is { } result)
                    {
                        roundOutcome = result;
                        break;
                    }

                    isUserTurn = false;
                }
                else
                {
                    const int delayMs = 1500;
                    await Task.Delay(delayMs);
                    embedBuilder.WithDescription("🤖 My turn. Thinking...");
                    message = await ModifyOriginalResponseAsync(properties => properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]));
                    await Task.Delay(delayMs);

                    TurnInputChoiceResult choiceResult = provideComputerChoice(message.Components, computer, user, ++moveCounter);
                    components = choiceResult.NewComponents;

                    if (choiceResult.Outcome is { } result)
                    {
                        await ModifyOriginalResponseAsync(properties => properties.Components = components);
                        roundOutcome = result;
                        break;
                    }

                    isUserTurn = true;
                    embedBuilder.WithDescription(yourTurnMessage);
                    message = await ModifyOriginalResponseAsync(properties =>
                        {
                            properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]);
                            properties.Components = components;
                        }
                    );
                }
            }

            GetRoundOutcomeData(roundOutcome, out color, out summary);
        }

        await ModifyOriginalResponseWithRoundOverAsync(embedBuilder, components, summary, color);
    }

    /// <summary>
    ///     Represents a structure for the result of a choice.
    /// </summary>
    /// <param name="Outcome">The outcome.</param>
    /// <param name="NewComponents">The new components.</param>
    private readonly record struct TurnInputChoiceResult(RoundOutcome? Outcome, MessageComponent NewComponents);

    /// <summary>
    ///     Represents a structure for a player.
    /// </summary>
    /// <typeparam name="T">The data for the choice.</typeparam>
    private sealed class TurnInputPlayer<T>
        where T : struct
    {
        /// <summary>
        ///     Gets the player's user id.
        /// </summary>
        public ulong Id { get; }

        /// <summary>
        ///     Gets the player's type.
        /// </summary>
        public PlayerType Type { get; }

        /// <summary>
        ///     Gets or sets the player's current data for the game.
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        ///     Creates a new player.
        /// </summary>
        /// <param name="type">The player's type.</param>
        /// <param name="id">The player's user id.</param>
        /// <param name="data">The player's current data for the game.</param>
        public TurnInputPlayer(PlayerType type, ulong id, T data = default)
        {
            Type = type;
            Id = id;
            Data = data;
        }
    }
}
