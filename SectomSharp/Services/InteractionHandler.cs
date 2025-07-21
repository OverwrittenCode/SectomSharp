using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SectomSharp.Extensions;
using SectomSharp.Managers.Pagination.SelectMenu;
using SectomSharp.TypeConverters;
using SectomSharp.Utils;

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
            _logger.DiscordNetUnhandledException(ex.Message, ex);
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
        switch (result.Error)
        {
            case null:
            case InteractionCommandError.UnknownCommand when interaction.Type == InteractionType.MessageComponent:
                return;
            case InteractionCommandError.UnmetPrecondition or InteractionCommandError.ConvertFailed:
                await interaction.RespondOrFollowupAsync(result.ErrorReason, ephemeral: true);
                return;
            default:
                _logger.DiscordNetInteractionCommandFailed(result.Error.Value, result.ErrorReason);
                await interaction.RespondOrFollowupAsync($"{result.Error.Value} {result.ErrorReason}", ephemeral: true);
                return;
        }
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _interactionService.AddTypeConverter<Color>(new ColorConverter());
        _interactionService.AddTypeConverter<IEmote>(new RichEmojiConverter());

        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        IEnumerable<ICommandInfo> commandInfos = _interactionService.Modules.SelectMany(module => module.SlashCommands.Cast<ICommandInfo>()
                                                                                                        .Concat(module.ContextCommands)
                                                                                                        .Concat(module.ComponentCommands)
                                                                                                        .Concat(module.AutocompleteCommands)
                                                                                                        .Concat(module.ModalCommands)
        );
        foreach (ICommandInfo cmd in commandInfos)
        {
            Storage.CommandInfoFullNameMap.Add(
                cmd,
                cmd.Module.IsSlashGroup
                    ? cmd.Module.Parent.IsSubModule
                        ? String.Join(' ', cmd.Module.Parent.SlashGroupName, cmd.Module.SlashGroupName, cmd.Name)
                        : String.Join(' ', cmd.Module.SlashGroupName, cmd.Name)
                    : cmd.Name
            );
        }

        HelpSelectMenuPaginationManager.Initialize(_interactionService);

        _client.InteractionCreated += HandleInteractionCreated;
        _interactionService.InteractionExecuted += HandleInteractionExecuted;
    }
}
