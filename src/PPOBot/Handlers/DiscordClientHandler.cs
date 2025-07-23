using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace PPOBot.Handlers;

public class DiscordClientHandler(
    DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider serviceProvider,
    IOptions<Settings> options,
    ILogger<DiscordClientHandler> logger) 
    : BackgroundService()
{
    private readonly Settings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var _ = logger.BeginScope("Discord");

        client.Log += Log;
        interactionService.Log += Log;

        await interactionService.AddModuleAsync<DiscordSlashCommandHandler>(serviceProvider);

        client.Ready += HandleReady;
        client.InteractionCreated += HandleInteractionReceived;

        logger.LogInformation("Starting discord service...");

        await client.LoginAsync(TokenType.Bot, _settings.DiscordToken);
        await client.StartAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            // do stuff?
            await Task.Delay(1000, stoppingToken);
        }

        await client.StopAsync();
    }
    
    private async Task HandleReady()
        => await interactionService.RegisterCommandsGloballyAsync();

    private async Task HandleInteractionReceived(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(client, interaction);

        if (context.Guild?.Id != _settings.DiscordGuildId)
            return;

        if (interaction.User.IsBot)
            return;

        var result = await interactionService.ExecuteCommandAsync(context, serviceProvider);

        if (!result.IsSuccess)
            logger.LogError("Failed to process interaction: {@ErrorReason}", result.ErrorReason);
    }

    private Task Log(LogMessage message)
    {
        var severity = message.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => throw new ArgumentException()
        };

        logger.Log(severity, message.Exception, "[{Source}] {Message}", message.Source, message.Message);

        return Task.CompletedTask;
    }
}