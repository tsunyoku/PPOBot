using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PPOBot;
using PPOBot.Handlers;
using PPOBot.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services
    .AddOptions<Settings>()
    .Bind(builder.Configuration)
    .ValidateOnStart();

var settings = builder.Configuration.Get<Settings>()!;

builder.Services.AddSingleton(new DiscordSocketConfig
{
    LogLevel = LogSeverity.Warning,
    GatewayIntents = GatewayIntents.All &
                     ~(GatewayIntents.GuildPresences | GatewayIntents.GuildScheduledEvents | GatewayIntents.GuildInvites)
});
builder.Services.AddSingleton<DiscordSocketClient>();

builder.Services.AddSingleton(new InteractionServiceConfig
{
    LogLevel = LogSeverity.Warning,
    DefaultRunMode = Discord.Interactions.RunMode.Async,
    UseCompiledLambda = true
});

builder.Services.AddSingleton<InteractionService>(sp =>
    new InteractionService(
        sp.GetRequiredService<DiscordSocketClient>(),
        sp.GetRequiredService<InteractionServiceConfig>()));

builder.Services.AddSingleton(new CommandServiceConfig
{
    LogLevel = LogSeverity.Warning,
    DefaultRunMode = Discord.Commands.RunMode.Async,
});
builder.Services.AddSingleton<CommandService>();

builder.Services.AddScoped<IColourRoleService, ColourRoleService>();

builder.Services.AddDbContext<PPODbContext>(options =>
    options.UseNpgsql(settings.DatabaseConnectionString));

builder.Services.Configure<HostOptions>(opts =>
    opts.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);

builder.Services.AddHostedService<DiscordClientHandler>();

var host = builder.Build();

using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PPODbContext>();
    dbContext.Database.Migrate();
}

host.Run();