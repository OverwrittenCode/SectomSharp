using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Events;
using SectomSharp.Extensions;

namespace SectomSharp.Services;

internal sealed class EventService
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordEvent _discord;
    private readonly InteractionService _interactionService;

    public EventService(
        DiscordSocketClient client,
        DiscordEvent discord,
        InteractionService interactionService
    )
    {
        _client = client;
        _discord = discord;
        _interactionService = interactionService;
    }

    public void RegisterEvents()
    {
        _client.Ready += _discord.OnReady;

        _client.InteractionCreated += _discord.OnInteractionCreated;

        _interactionService.SlashCommandExecuted += async (arg1, context, result) =>
        {
            if (!result.IsSuccess)
            {
                var message =
                    result.Error == InteractionCommandError.UnmetPrecondition
                        ? result.ErrorReason
                        : $"{result.Error}: {result.ErrorReason}";

                await context.Interaction.RespondOrFollowupAsync(message, ephemeral: true);
            }
        };
    }
}
