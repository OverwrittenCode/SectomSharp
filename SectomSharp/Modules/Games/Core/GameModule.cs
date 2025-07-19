using System.Diagnostics;
using Discord;
using Discord.WebSocket;
using SectomSharp.Extensions;
using SectomSharp.Utils;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    private const int TimeoutSeconds = 15;
    private const string ComponentNotForYou = "This is not for you!";
    private const string ChoiceAlreadyLockedMessage = "Your choice is already locked.";
    private const string ChoiceLockedMessage = "Choice locked in. Waiting for opponent...";

    /// <summary>
    ///     Gets a random element of a given array.
    /// </summary>
    /// <param name="array">The array.</param>
    /// <typeparam name="T">The data type stored in the element of an array.</typeparam>
    /// <returns>The random element.</returns>
    private static T GetRandomElement<T>(T[] array) => array[Random.Shared.Next(array.Length)];

    private static T GetRandomElement<T>(Span<T> array) => array[Random.Shared.Next(array.Length)];

    /// <summary>
    ///     Sets the <paramref name="color" /> and <paramref name="summary" /> based on the <paramref name="result" />.
    /// </summary>
    /// <param name="result">The round outcome.</param>
    /// <param name="color">The color.</param>
    /// <param name="summary">The summary.</param>
    private static void GetRoundOutcomeData(RoundOutcome result, out Color color, out string summary)
    {
        switch (result)
        {
            case RoundOutcome.PlayerOneWins:
                summary = "🎉 You win!";
                color = Color.Green;
                break;
            case RoundOutcome.PlayerTwoWins:
                summary = "😔 Computer wins!";
                color = Color.Red;
                break;
            default:
                summary = "🤝 It's a tie!";
                color = Storage.LightGold;
                break;
        }
    }

    /// <summary>
    ///     Represents the state for the conclusion of a round.
    /// </summary>
    private enum RoundOutcome
    {
        /// <summary>
        ///     It was a tie.
        /// </summary>
        Tie = 0,

        /// <summary>
        ///     The first player won.
        /// </summary>
        PlayerOneWins = 1,

        /// <summary>
        ///     The second player won.
        /// </summary>
        PlayerTwoWins = 2
    }

    /// <summary>
    ///     Sets the <paramref name="color" /> and <paramref name="summary" /> based on the <paramref name="opponent" /> and <paramref name="result" />.
    /// </summary>
    /// <param name="opponent">The opponent.</param>
    /// <param name="result">The round outcome.</param>
    /// <param name="color">The color.</param>
    /// <param name="summary">The summary.</param>
    private void GetRoundOutcomeData(IGuildUser opponent, RoundOutcome result, out Color color, out string summary)
    {
        switch (result)
        {
            case RoundOutcome.PlayerOneWins:
                summary = $"🎉 {Context.User.Mention} wins!";
                color = Color.Green;
                break;
            case RoundOutcome.PlayerTwoWins:
                summary = $"🎉 {opponent.Mention} wins!";
                color = Color.Green;
                break;
            default:
                summary = "🤝 It's a tie!";
                color = Storage.LightGold;
                break;
        }
    }

    /// <summary>
    ///     Edits original response for this interaction with a timeout message and embed builder.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="message">The message for the interaction.</param>
    /// <param name="description">The description for the embed.</param>
    private async Task ModifyOriginalResponseWithErrorEmbedAsync(EmbedBuilder embedBuilder, IUserMessage message, string description)
    {
        embedBuilder.Color = Color.Red;
        embedBuilder.Description = description;
        await ModifyOriginalResponseAsync(properties =>
            {
                properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]);
                properties.Components = message.Components.FromComponentsWithAllDisabled().Build();
            }
        );
    }

    /// <summary>
    ///     Edits original response for this interaction with a round over message.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="components">The components for the interaction.</param>
    /// <param name="description">The description for the embed.</param>
    /// <param name="color">The color for the embed.</param>
    private async Task ModifyOriginalResponseWithRoundOverAsync(EmbedBuilder embedBuilder, MessageComponent components, string description, Color color)
    {
        embedBuilder.Color = color;
        embedBuilder.Description = description;
        await ModifyOriginalResponseAsync(properties =>
            {
                properties.Embeds = new Optional<Embed[]>([embedBuilder.Build()]);
                properties.Components = components.Components.FromComponentsWithAllDisabled().Build();
            }
        );
    }

    /// <summary>
    ///     Temporarily listens to an event with a provided handler.
    /// </summary>
    /// <param name="componentType">The type of component event to listen to.</param>
    /// <param name="tcs">The task completion source.</param>
    /// <param name="handler">Fired when the corresponding message component event interaction is received.</param>
    /// <typeparam name="TData">The data used for the <see cref="TaskCompletionSource{TResult}" />.</typeparam>
    /// <returns>A boolean representing whether the given <see cref="TaskCompletionSource{TResult}" /> completed first.</returns>
    private async Task<bool> TryWaitForComponentResponseAsync<TData>(ComponentType componentType, TaskCompletionSource<TData> tcs, Func<SocketMessageComponent, Task> handler)
    {
        Debug.Assert(componentType is ComponentType.Button or ComponentType.SelectMenu);

        try
        {
            if (componentType == ComponentType.Button)
            {
                Context.Client.ButtonExecuted += handler;
            }
            else
            {
                Context.Client.SelectMenuExecuted += handler;
            }

            Task completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(TimeoutSeconds)));
            return completedTask == tcs.Task;
        }
        finally
        {
            if (componentType == ComponentType.Button)
            {
                Context.Client.ButtonExecuted -= handler;
            }
            else
            {
                Context.Client.SelectMenuExecuted -= handler;
            }
        }
    }

    /// <summary>
    ///     Sends an embed requesting the opponent to play.
    /// </summary>
    /// <param name="embedBuilder">The embed builder to modify.</param>
    /// <param name="opponent">The opponent.</param>
    /// <param name="gameTitle">The title used for the embed.</param>
    /// <returns>A task with the editable message if the opponent accepted the challenge.</returns>
    private async Task<IUserMessage?> RequestOpponentToPlayAsync(EmbedBuilder embedBuilder, IGuildUser opponent, string gameTitle)
    {
        if (opponent.Id == Context.User.Id)
        {
            await RespondAsync("You cannot play against yourself.", ephemeral: true);
            return null;
        }

        embedBuilder.Color = Color.Purple;
        embedBuilder.Title = gameTitle;
        embedBuilder.Description = $"{Context.Interaction.User.Mention} has challenged {opponent.Mention}!";

        string customId = StringUtils.GenerateUniqueId();
        string acceptId = $"{customId}-accept";
        string declineId = $"{customId}-decline";
        var acceptButton = ButtonBuilder.CreateSuccessButton("Accept", acceptId);
        var declineButton = ButtonBuilder.CreateDangerButton("Decline", declineId);
        var actionRowBuilder = new ActionRowBuilder { Components = [acceptButton, declineButton] };
        MessageComponent components = new ComponentBuilder { ActionRows = [actionRowBuilder] }.Build();

        await DeferAsync();
        IUserMessage message = await FollowupAsync(embeds: [embedBuilder.Build()], components: components);

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (await TryWaitForComponentResponseAsync(ComponentType.Button, tcs, OnButtonExecuted) && tcs.Task.Result)
        {
            return message;
        }

        embedBuilder.Color = Color.Red;
        embedBuilder.Description = "Request declined.";
        Embed[] declinedEmbeds = [embedBuilder.Build()];
        await ModifyOriginalResponseAsync(properties =>
            {
                properties.Components = components.Components.FromComponentsWithAllDisabled().Build();
                properties.Embeds = declinedEmbeds;
            }
        );

        return null;

        async Task OnButtonExecuted(SocketMessageComponent button)
        {
            if (button.User.Id != opponent.Id)
            {
                await button.RespondAsync(ComponentNotForYou, ephemeral: true);
                return;
            }

            if (button.Data.CustomId == acceptId)
            {
                await button.DeferAsync();
                tcs.TrySetResult(true);
            }
            else if (button.Data.CustomId == declineId)
            {
                await button.DeferAsync();
                tcs.TrySetResult(false);
            }
        }
    }
}
