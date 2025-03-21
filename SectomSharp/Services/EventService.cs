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

    public EventService(DiscordSocketClient client, DiscordEvent discord, InteractionService interactionService)
    {
        _client = client;
        _discord = discord;
        _interactionService = interactionService;
    }

    public void RegisterEvents()
    {
        _client.Ready += _discord.HandleClientReady;

        _client.InteractionCreated += _discord.HandleInteractionCreated;

        _client.GuildUpdated += DiscordEvent.HandleGuildUpdatedAsync;

        _client.GuildMemberUpdated += DiscordEvent.HandleGuildMemberUpdatedAsync;

        _client.MessageDeleted += DiscordEvent.HandleMessageDeletedAsync;
        _client.MessageUpdated += DiscordEvent.HandleMessageUpdatedAsync;

        _client.GuildStickerCreated += DiscordEvent.HandleGuildStickerCreatedAsync;
        _client.GuildStickerDeleted += DiscordEvent.HandleGuildStickerDeletedAsync;
        _client.GuildStickerUpdated += DiscordEvent.HandleGuildStickerUpdatedAsync;

        _client.ChannelCreated += DiscordEvent.HandleChannelCreatedAsync;
        _client.ChannelDestroyed += DiscordEvent.HandleChannelDestroyedAsync;
        _client.ChannelUpdated += DiscordEvent.HandleChannelUpdatedAsync;

        _client.ThreadCreated += DiscordEvent.HandleThreadCreatedAsync;
        _client.ThreadDeleted += DiscordEvent.HandleThreadDeleteAsync;
        _client.ThreadUpdated += DiscordEvent.HandleThreadUpdatedAsync;

        _client.RoleCreated += DiscordEvent.HandleRoleCreatedAsync;
        _client.RoleDeleted += DiscordEvent.HandleRoleDeletedAsync;
        _client.RoleUpdated += DiscordEvent.HandleRoleUpdateAsync;

        _interactionService.SlashCommandExecuted += async (_, context, result) =>
        {
            if (!result.IsSuccess)
            {
                string message = result.Error == InteractionCommandError.UnmetPrecondition ? result.ErrorReason : $"{result.Error}: {result.ErrorReason}";

                await context.Interaction.RespondOrFollowupAsync(message, ephemeral: true);
            }
        };
    }
}
