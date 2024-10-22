using Discord.Interactions;
using Discord.WebSocket;
using SectomSharp.Events;
using SectomSharp.Extensions;

namespace SectomSharp.Services;

internal sealed class EventService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionEvent _interactionEvent;
    private readonly ReadyEvent _readyEvent;
    private readonly InteractionService _interactionService;

    public EventService(
        DiscordSocketClient client,
        InteractionEvent interactionEvent,
        ReadyEvent readyEvent,
        InteractionService interactionService
    )
    {
        _client = client;
        _interactionEvent = interactionEvent;
        _readyEvent = readyEvent;
        _interactionService = interactionService;
    }

    public void RegisterEvents()
    {
        _client.Ready += _readyEvent.OnReady;

        _client.InteractionCreated += _interactionEvent.OnInteractionCreated;

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
