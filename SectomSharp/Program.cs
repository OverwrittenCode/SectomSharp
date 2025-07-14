using System.Text;
using Discord;
using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SectomSharp.Data;
using SectomSharp.Data.CompositeTypes;
using SectomSharp.Events;
using SectomSharp.Services;

Console.OutputEncoding = Encoding.UTF8;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", false, false).AddUserSecrets<Program>(true, true).AddEnvironmentVariables();

string connectionString = builder.Configuration["PostgreSQL:ConnectionString"] ?? throw new InvalidOperationException("Missing PostgreSQL connection string");

var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapComposite<CompositeEmbedField>(CompositeEmbedField.PgName);
NpgsqlDataSource dataSource = dataSourceBuilder.Build();

builder.Services.AddSingleton(dataSource);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();
builder.Services.AddDbContextFactory<ApplicationDbContext>((s, options) => options.UseNpgsql(s.GetRequiredService<NpgsqlDataSource>())
                                                                                  .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
);

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

builder.Services.AddSingleton<DiscordEvent>();
builder.Services.AddSingleton<InteractionService>();

builder.Services.AddHostedService<DiscordBotService>();
builder.Services.AddHostedService<InteractionHandler>();

IHost app = builder.Build();

await app.RunAsync();
