using Discord.Interactions;
using Discord.WebSocket;

namespace SectomSharp.Events;

public sealed partial class DiscordEvent
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public DiscordEvent(IServiceProvider services, DiscordSocketClient client, InteractionService interactionService)
    {
        _services = services;
        _client = client;
        _interactionService = interactionService;
    }
}
