using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace SectomSharp.Services;

internal sealed class StartupService
{
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _configuration;
    private readonly EventService _eventService;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _services;

    public StartupService(IServiceProvider services, DiscordSocketClient client, EventService eventService, InteractionService interactionService, IConfiguration configuration)
    {
        _services = services;
        _client = client;
        _eventService = eventService;
        _interactionService = interactionService;
        _configuration = configuration;
    }

    public async Task StartAsync()
    {
        _eventService.RegisterEvents();

        string token = _configuration["Discord:BotToken"] ?? throw new InvalidOperationException("Bot token not found in configuration.");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
    }
}
