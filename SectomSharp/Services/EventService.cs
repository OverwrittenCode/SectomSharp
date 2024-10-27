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
        _client.Ready += _discord.HandleClientReady;

        _client.InteractionCreated += _discord.HandleInteractionCreated;

        _client.GuildUpdated += _discord.HandleGuildUpdatedAsync;

        _client.GuildMemberUpdated += _discord.HandleGuildMemberUpdatedAsync;

        _client.MessageDeleted += _discord.HandleMessageDeletedAsync;
        _client.MessageUpdated += _discord.HandleMessageUpdatedAsync;

        _client.GuildStickerCreated += _discord.HandleGuildStickerCreatedAsync;
        _client.GuildStickerDeleted += _discord.HandleGuildStickerDeletedAsync;
        _client.GuildStickerUpdated += _discord.HandleGuildStickerUpdatedAsync;

        _client.ChannelCreated += _discord.HandleChannelCreatedAsync;
        _client.ChannelDestroyed += _discord.HandleChannelDestroyedAsync;
        _client.ChannelUpdated += _discord.HandleChannelUpdatedAsync;

        _client.ThreadCreated += _discord.HandleThreadCreatedAsync;
        _client.ThreadDeleted += _discord.HandleThreadDeleteAsync;
        _client.ThreadUpdated += _discord.HandleThreadUpdatedAsync;

        _client.RoleCreated += _discord.HandleRoleCreatedAsync;
        _client.RoleDeleted += _discord.HandleRoleDeletedAsync;
        _client.RoleUpdated += _discord.HandleRoleUpdateAsync;

        _interactionService.SlashCommandExecuted += async (_, context, result) =>
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
