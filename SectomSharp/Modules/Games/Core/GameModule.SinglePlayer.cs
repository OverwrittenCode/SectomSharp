using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace SectomSharp.Modules.Games;

public sealed partial class GameModule
{
    /// <summary>
    ///     Asynchronously waits for the current user to interact with a button or select menu.
    /// </summary>
    /// <param name="componentType"> The type of component event to listen to.</param>
    /// <param name="components">The components to use.</param>
    /// <param name="processPlayerChoice">Executed when either player makes their choice.</param>
    /// <typeparam name="T">The choice data.</typeparam>
    /// <returns>null if the user took too long, otherwise the processed choice.</returns>
    private async Task<T?> TryWaitForSinglePlayerChoiceAsync<T>(
        ComponentType componentType,
        MessageComponent components,
        [RequireStaticDelegate] DualInputProcessPlayerChoice<T> processPlayerChoice
    )
        where T : struct
    {
        await DeferAsync(true);
        EmbedBuilder embedBuilder = new EmbedBuilder().WithColor(Color.Purple).WithDescription("Select your choice below to play against the computer.");
        IUserMessage message = await FollowupAsync(embeds: [embedBuilder.Build()], components: components);

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (await TryWaitForComponentResponseAsync(componentType, tcs, OnMessageComponentExecuted))
        {
            return tcs.Task.Result;
        }

        await ModifyOriginalResponseWithErrorEmbedAsync(embedBuilder, message, "You did not select a choice in time.");
        return null;

        async Task OnMessageComponentExecuted(SocketMessageComponent component)
        {
            if (component.Message.Id == message.Id)
            {
                await component.DeferAsync(true);
                tcs.TrySetResult(processPlayerChoice(component));
            }
        }
    }
}
