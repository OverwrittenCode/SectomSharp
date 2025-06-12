using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SectomSharp.Data;
using SectomSharp.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;

Console.OutputEncoding = Encoding.UTF8;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddUserSecrets<Program>(true, true).AddEnvironmentVariables();

Logger loggerConfig = new LoggerConfiguration().MinimumLevel.Debug()
                                               .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Warning)
                                               .Enrich.FromLogContext()
                                               .WriteTo.Console()
                                               .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(loggerConfig, true);

builder.Services.AddDbContext<ApplicationDbContext>();

builder.Services.AddSingleton(
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
                       | GatewayIntents.MessageContent,
        FormatUsersInBidirectionalUnicode = false
    }
);

builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<IRestClientProvider>(s => s.GetRequiredService<DiscordSocketClient>());

builder.Services.AddSingleton(
    new InteractionServiceConfig
    {
        LogLevel = LogSeverity.Info,
        DefaultRunMode = RunMode.Async
    }
);

builder.Services.AddSingleton<InteractionService>();

builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<InteractionHandler>();

IHost app = builder.Build();

await app.RunAsync();
