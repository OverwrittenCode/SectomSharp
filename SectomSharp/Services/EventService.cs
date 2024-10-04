using Discord.WebSocket;
using SectomSharp.Events;

namespace SectomSharp.Services;

internal sealed class EventService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionEvent _interactionEvent;
    private readonly ReadyEvent _readyEvent;

    public EventService(
        DiscordSocketClient client,
        InteractionEvent interactionEvent,
        ReadyEvent readyEvent
    )
    {
        _client = client;
        _interactionEvent = interactionEvent;
        _readyEvent = readyEvent;
    }

    public void RegisterEvents()
    {
        _client.Ready += _readyEvent.OnReady;

        _client.InteractionCreated += _interactionEvent.OnInteractionCreated;
    }
}
