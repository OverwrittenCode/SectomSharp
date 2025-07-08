using System.Diagnostics;
using Discord;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    [SlashCmd("ttt", "Play tic-tac-toe")]
    public async Task TicTacToe(IGuildUser? opponent = null)
    {
        await HandleTurnInputGameSession<int>(
            ComponentType.Button,
            "Tic-Tac-Toe",
            opponent,
            TicTacToeStorage.Components,
            static (component, playerOne, playerTwo, currentPlayer, moveCounter) =>
            {
                int moveIndex = Int32.Parse(component.Data.CustomId);
                currentPlayer.Data |= 1 << moveIndex;

                MessageComponent newComponents = GetNewComponents(component.Message.Components, currentPlayer.Type, moveIndex);
                return GenerateResult(playerOne.Data, playerTwo.Data, moveCounter, newComponents);
            },
            static (components, computer, user, moveCounter) =>
            {
                int takenMoves = computer.Data | user.Data;
                Span<int> indexes = stackalloc int[TicTacToeStorage.MaxMoves];
                int count = 0;
                for (int i = 0; i < TicTacToeStorage.MaxMoves; i++)
                {
                    int bitmask = 1 << i;
                    if ((takenMoves & bitmask) == 0)
                    {
                        indexes[count++] = i;
                    }
                }

                int moveIndex = GetRandomElement(indexes[..count]);
                computer.Data |= 1 << moveIndex;

                MessageComponent newComponents = GetNewComponents(components, computer.Type, moveIndex);
                return GenerateResult(user.Data, computer.Data, moveCounter, newComponents);
            }
        );

        return;

        static MessageComponent GetNewComponents(IReadOnlyCollection<IMessageComponent> components, PlayerType playerType, int moveIndex)
        {
            int row = moveIndex / TicTacToeStorage.GridSize;
            int col = moveIndex % TicTacToeStorage.GridSize;

            ComponentBuilder componentBuilder = ComponentBuilder.FromComponents(components);
            var buttonComponent = (ButtonComponent)componentBuilder.ActionRows[row].Components[col];

            Debug.Assert(!buttonComponent.IsDisabled);

            ButtonBuilder buttonBuilder = buttonComponent.ToBuilder().WithDisabled(true);
            if (playerType == PlayerType.One)
            {
                buttonBuilder.WithLabel("X").WithStyle(ButtonStyle.Danger);
            }
            else
            {
                buttonBuilder.WithLabel("O").WithStyle(ButtonStyle.Primary);
            }

            componentBuilder.ActionRows[row].Components[col] = buttonBuilder.Build();
            return componentBuilder.Build();
        }

        static TurnInputChoiceResult GenerateResult(int playerOne, int playerTwo, int moveCounter, MessageComponent newComponents)
        {
            switch (moveCounter)
            {
                case TicTacToeStorage.MaxMoves:
                    return new TurnInputChoiceResult(RoundOutcome.Tie, newComponents);

                case >= 5:
                    {
                        foreach (int combination in TicTacToeStorage.WinningCombinations)
                        {
                            if ((playerOne & combination) == combination)
                            {
                                return new TurnInputChoiceResult(RoundOutcome.PlayerOneWins, newComponents);
                            }

                            if ((playerTwo & combination) == combination)
                            {
                                return new TurnInputChoiceResult(RoundOutcome.PlayerTwoWins, newComponents);
                            }
                        }

                        break;
                    }
            }

            return new TurnInputChoiceResult(null, newComponents);
        }
    }

    private static class TicTacToeStorage
    {
        private const string InvisibleCharacter = "\u2800";
        public const int GridSize = 3;
        public const int MaxMoves = GridSize * GridSize;

        public static readonly HashSet<int> WinningCombinations;
        public static readonly MessageComponent Components;

        static TicTacToeStorage()
        {
            WinningCombinations =
            [
                0x7,
                0x38,
                0x1C0,
                0x49,
                0x92,
                0x124,
                0x111,
                0x54
            ];

            var componentBuilder = new ComponentBuilder();
            for (int row = 0; row < GridSize; row++)
            {
                var actionRowBuilder = new ActionRowBuilder();
                for (int col = 0; col < GridSize; col++)
                {
                    actionRowBuilder.WithButton(InvisibleCharacter, (row * GridSize + col).ToString(), ButtonStyle.Secondary);
                }

                componentBuilder.AddRow(actionRowBuilder);
            }

            Components = componentBuilder.Build();
        }
    }
}
