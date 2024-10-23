using Discord.Interactions;
using Discord.WebSocket;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;

    public DiscordEvent(
        IServiceProvider services,
        DiscordSocketClient client,
        InteractionService interactionService
    )
    {
        _services = services;
        _client = client;
        _interactionService = interactionService;
    }
}
