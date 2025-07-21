using Discord;
using SectomSharp.Attributes;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    [SlashCmd("Play rock-paper-scissors-lizard-spock")]
    public Task Rps(IGuildUser? opponent = null)
        => HandleDualInputGameSession(
            ComponentType.Button,
            "Rock-Paper-Scissors-Lizard-Spock",
            opponent,
            RpsStorage.Components,
            static component => Enum.Parse<RpsStorage.Move>(component.Data.CustomId),
            static () => GetRandomElement(RpsStorage.AllMoves),
            static (playerOne, playerTwo, out outcome) =>
            {
                (outcome, string action) = RpsStorage.OutcomeMap[(playerOne, playerTwo)];
                return $"{playerOne} {action} {playerTwo}";
            }
        );

    private static class RpsStorage
    {
        public static readonly Dictionary<(Move playerOne, Move playerTwo), (RoundOutcome outcome, string action)> OutcomeMap = new()
        {
            [(Move.Rock, Move.Rock)] = (RoundOutcome.Tie, "ties with"),
            [(Move.Rock, Move.Scissors)] = (RoundOutcome.PlayerOneWins, "crushes"),
            [(Move.Rock, Move.Lizard)] = (RoundOutcome.PlayerOneWins, "crushes"),
            [(Move.Rock, Move.Paper)] = (RoundOutcome.PlayerTwoWins, "is covered by"),
            [(Move.Rock, Move.Spock)] = (RoundOutcome.PlayerTwoWins, "is vaporized by"),
            [(Move.Paper, Move.Paper)] = (RoundOutcome.Tie, "ties with"),
            [(Move.Paper, Move.Rock)] = (RoundOutcome.PlayerOneWins, "covers"),
            [(Move.Paper, Move.Spock)] = (RoundOutcome.PlayerOneWins, "disproves"),
            [(Move.Paper, Move.Scissors)] = (RoundOutcome.PlayerTwoWins, "is cut by"),
            [(Move.Paper, Move.Lizard)] = (RoundOutcome.PlayerTwoWins, "is eaten by"),
            [(Move.Scissors, Move.Scissors)] = (RoundOutcome.Tie, "ties with"),
            [(Move.Scissors, Move.Paper)] = (RoundOutcome.PlayerOneWins, "cuts"),
            [(Move.Scissors, Move.Lizard)] = (RoundOutcome.PlayerOneWins, "decapitates"),
            [(Move.Scissors, Move.Rock)] = (RoundOutcome.PlayerTwoWins, "is crushed by"),
            [(Move.Scissors, Move.Spock)] = (RoundOutcome.PlayerTwoWins, "is smashed by"),
            [(Move.Lizard, Move.Lizard)] = (RoundOutcome.Tie, "ties with"),
            [(Move.Lizard, Move.Spock)] = (RoundOutcome.PlayerOneWins, "poisons"),
            [(Move.Lizard, Move.Paper)] = (RoundOutcome.PlayerOneWins, "eats"),
            [(Move.Lizard, Move.Scissors)] = (RoundOutcome.PlayerTwoWins, "is decapitated by"),
            [(Move.Lizard, Move.Rock)] = (RoundOutcome.PlayerTwoWins, "is crushed by"),
            [(Move.Spock, Move.Spock)] = (RoundOutcome.Tie, "ties with"),
            [(Move.Spock, Move.Scissors)] = (RoundOutcome.PlayerOneWins, "smashes"),
            [(Move.Spock, Move.Rock)] = (RoundOutcome.PlayerOneWins, "vaporizes"),
            [(Move.Spock, Move.Paper)] = (RoundOutcome.PlayerTwoWins, "is disproven by"),
            [(Move.Spock, Move.Lizard)] = (RoundOutcome.PlayerTwoWins, "is poisoned by")
        };

        public static readonly Move[] AllMoves;
        public static readonly MessageComponent Components;

        static RpsStorage()
        {
            AllMoves = Enum.GetValues<Move>();

            List<IMessageComponentBuilder> buttons = AllMoves.Select(IMessageComponentBuilder (move) =>
                                                                  {
                                                                      string s = move.ToString();
                                                                      var emote = new Emoji(
                                                                          move switch
                                                                          {
                                                                              Move.Rock => "🪨",
                                                                              Move.Paper => "📄",
                                                                              Move.Scissors => "✂️",
                                                                              Move.Lizard => "🦎",
                                                                              Move.Spock => "🖖",
                                                                              _ => ""
                                                                          }
                                                                      );

                                                                      return ButtonBuilder.CreateSecondaryButton(s, s, emote);
                                                                  }
                                                              )
                                                             .ToList();

            Components = new ComponentBuilder { ActionRows = [new ActionRowBuilder { Components = buttons }] }.Build();
        }

        public enum Move
        {
            Rock = 0,
            Paper = 1,
            Scissors = 2,
            Lizard = 3,
            Spock = 4
        }
    }
}
