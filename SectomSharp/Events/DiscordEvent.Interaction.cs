using Discord.Interactions;
using Discord.WebSocket;

namespace SectomSharp.Events;

public partial class DiscordEvent
{
    public async Task OnInteractionCreated(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        await _interactionService.ExecuteCommandAsync(ctx, _services);
    }
}
