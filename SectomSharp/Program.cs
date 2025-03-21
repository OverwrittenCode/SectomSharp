using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SectomSharp.Data;
using SectomSharp.Events;
using SectomSharp.Services;
using Serilog;

RunAsync().GetAwaiter().GetResult();
return;

static async Task RunAsync()
{
    var services = new ServiceCollection();
    RegisterServices(services);

    ServiceProvider provider = services.BuildServiceProvider();
    provider.GetRequiredService<LoggingService>();

    await provider.GetRequiredService<StartupService>().StartAsync();

    await Task.Delay(-1);
}

static IConfiguration BuildConfiguration()
    => new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddUserSecrets<Program>(false, true).AddEnvironmentVariables().Build();

static void RegisterServices(IServiceCollection services)
{
    IConfiguration configuration = BuildConfiguration();

    services.AddDbContext<ApplicationDbContext>()
            .AddSingleton(configuration)
            .AddSingleton(
                 new DiscordSocketClient(
                     new DiscordSocketConfig
                     {
                         LogLevel = LogSeverity.Info,
                         MessageCacheSize = 100,
                         GatewayIntents = GatewayIntents.Guilds
                                        | GatewayIntents.GuildMembers
                                        | GatewayIntents.GuildBans
                                        | GatewayIntents.GuildEmojis
                                        | GatewayIntents.GuildVoiceStates
                                        | GatewayIntents.GuildPresences
                                        | GatewayIntents.GuildMessages
                                        | GatewayIntents.GuildMessageReactions
                                        | GatewayIntents.MessageContent
                     }
                 )
             )
            .AddSingleton(s => new InteractionService(s.GetRequiredService<DiscordSocketClient>()))
            .AddSingleton<ILogger>(new LoggerConfiguration().MinimumLevel.Verbose().Enrich.FromLogContext().WriteTo.Console().CreateLogger())
            .AddSingleton<DiscordEvent>()
            .AddSingleton<LoggingService>()
            .AddSingleton<StartupService>()
            .AddSingleton<EventService>();
}
