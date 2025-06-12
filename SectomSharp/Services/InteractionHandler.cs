using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SectomSharp.Extensions;

namespace SectomSharp.Services;

public sealed class InteractionHandler : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;
    private readonly ILogger<InteractionHandler> _logger;

    public InteractionHandler(DiscordSocketClient client, InteractionService interactionService, IServiceProvider services, ILogger<InteractionHandler> logger)
    {
        _client = client;
        _interactionService = interactionService;
        _services = services;
        _logger = logger;
    }

    private async Task HandleInteractionCreated(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(_client, interaction);

            IResult result = await _interactionService.ExecuteCommandAsync(context, _services);

            if (!result.IsSuccess)
            {
                await HandleInteractionExecutionResult(interaction, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{Message}", ex.Message);
        }
    }

    private async Task HandleInteractionExecuted(ICommandInfo command, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
        {
            await HandleInteractionExecutionResult(context.Interaction, result);
        }
    }

    private async Task HandleInteractionExecutionResult(IDiscordInteraction interaction, IResult result)
    {
        if (result.Error == InteractionCommandError.UnknownCommand && interaction.Type == InteractionType.MessageComponent)
        {
            return;
        }

        string message = result.Error == InteractionCommandError.UnmetPrecondition ? result.ErrorReason : $"{result.Error}: {result.ErrorReason}";
        _logger.LogInformation("{Message}", message);
        await interaction.RespondOrFollowupAsync(message, ephemeral: true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

        _client.InteractionCreated += HandleInteractionCreated;
        _interactionService.InteractionExecuted += HandleInteractionExecuted;
    }
}
