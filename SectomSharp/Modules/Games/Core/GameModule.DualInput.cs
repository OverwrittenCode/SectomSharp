using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    /// <summary>
    ///     Executed when either player makes their choice.
    /// </summary>
    /// <param name="component">The component representing the user's choice.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    /// <returns>The user's processed choice.</returns>
    private delegate T DualInputProcessPlayerChoice<out T>(SocketMessageComponent component)
        where T : struct;

    /// <summary>
    ///     Executed when both players have made their choice.
    /// </summary>
    /// <param name="playerOne">The choice selected by player one.</param>
    /// <param name="playerTwo">The choice selected by player two.</param>
    /// <param name="outcome">The round outcome.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    /// <returns>The reason the round ended.</returns>
    private delegate string DualInputRoundResolver<in T>(T playerOne, T playerTwo, out RoundOutcome outcome)
        where T : struct;

    /// <summary>
    ///     Handles the logic for a dual input game session.
    /// </summary>
    /// <param name="componentType">The type of component event to listen to.</param>
    /// <param name="gameTitle">The game title used for the embed.</param>
    /// <param name="opponent">The opponent.</param>
    /// <param name="components">The components used for the game.</param>
    /// <param name="processPlayerChoice">Executed when either player makes their choice.</param>
    /// <param name="generateComputerMove">Executed when it is the computer's turn.</param>
    /// <param name="roundResolver">Executed when both players have made their choice.</param>
    /// <typeparam name="T">The data for the choice.</typeparam>
    private async Task HandleDualInputGameSession<T>(
        ComponentType componentType,
        string gameTitle,
        IGuildUser? opponent,
        MessageComponent components,
        [RequireStaticDelegate] DualInputProcessPlayerChoice<T> processPlayerChoice,
        [RequireStaticDelegate] Func<T> generateComputerMove,
        [RequireStaticDelegate] DualInputRoundResolver<T> roundResolver
    )
        where T : struct
    {
        string reason;
        string summary;
        Color color;
        EmbedBuilder embedBuilder;
        if (opponent?.IsBot == false)
        {
            embedBuilder = new EmbedBuilder();
            if (await RequestOpponentToPlayAsync(embedBuilder, opponent, gameTitle) is not { } message)
            {
                return;
            }

            if (await ObtainMultiPlayerChoiceAsync(componentType, message, embedBuilder, components, opponent.Id, processPlayerChoice) is not var (playerOne, playerTwo))
            {
                return;
            }

            reason = roundResolver(playerOne, playerTwo, out RoundOutcome result);
            GetRoundOutcomeData(opponent, result, out color, out summary);
        }
        else
        {
            if (await TryWaitForSinglePlayerChoiceAsync(componentType, components, processPlayerChoice) is not { } choice)
            {
                return;
            }

            T computerMove = generateComputerMove();
            reason = roundResolver(choice, computerMove, out RoundOutcome result);
            GetRoundOutcomeData(result, out color, out summary);
            embedBuilder = new EmbedBuilder();
        }

        await ModifyOriginalResponseWithRoundOverAsync(embedBuilder, components, $"{summary}\n{reason}", color);
    }
}
