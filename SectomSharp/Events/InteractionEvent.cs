using Discord.Interactions;
using Discord.WebSocket;

namespace SectomSharp.Events;

public sealed class InteractionEvent
{
    private readonly IServiceProvider _services;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;

    public InteractionEvent(
        IServiceProvider services,
        DiscordSocketClient client,
        InteractionService interactionService
    )
    {
        _services = services;
        _client = client;
        _interactionService = interactionService;
    }

    public async Task OnInteractionCreated(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(_client, interaction);
        var result = await _interactionService.ExecuteCommandAsync(ctx, _services);

        if (!result.IsSuccess)
        {
            var message =
                result.Error == InteractionCommandError.UnmetPrecondition
                    ? result.ErrorReason
                    : $"{result.Error}: {result.ErrorReason}";

            if (interaction.HasResponded)
            {
                await interaction.FollowupAsync(message, ephemeral: true);
            }
            else
            {
                await interaction.RespondAsync(message, ephemeral: true);
            }
        }
    }
}
