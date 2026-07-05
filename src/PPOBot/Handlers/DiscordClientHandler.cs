using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PPOBot.Modules;
using IResult = Discord.Interactions.IResult;

namespace PPOBot.Handlers;

public class DiscordClientHandler(
    DiscordSocketClient client,
    InteractionService interactionService,
    IServiceProvider serviceProvider,
    PPODbContext dbContext,
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

        await interactionService.AddModuleAsync<ModerationModule>(serviceProvider);
        await interactionService.AddModuleAsync<ColourRoleModule>(serviceProvider);

        client.Ready += HandleReady;
        client.InteractionCreated += HandleInteractionReceived;
        client.GuildMemberUpdated += HandleUserUpdated;
        interactionService.SlashCommandExecuted += HandleSlashCommandExecuted;

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

    private async Task HandleSlashCommandExecuted(
        SlashCommandInfo slashCommand,
        IInteractionContext context,
        IResult result)
    {
        if (result.IsSuccess)
            return;

        if (result.Error is InteractionCommandError.UnmetPrecondition)
            await context.Interaction.RespondAsync(result.ErrorReason, ephemeral: true);
    }

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
    
    private async Task HandleUserUpdated(Cacheable<SocketGuildUser, ulong> _, SocketGuildUser postUpdateUser)
    {
        var colourRoles = await dbContext.ColourRoles
            .AsNoTracking()
            .ToArrayAsync();
        
        var colourRoleIds = colourRoles.Select(x => x.RoleId).ToArray();

        foreach (var (roleId, count) in await postUpdateUser.Guild.GetRoleUserCountsAsync())
        {
            if (!colourRoleIds.Contains(roleId))
                continue;

            var discordRole = await postUpdateUser.Guild.GetRoleAsync(roleId);
            
            if (count == 0)
            {
                await discordRole.DeleteAsync();

                await dbContext.ColourRoleMembers
                    .AsNoTracking()
                    .Where(x => x.RoleId == discordRole.Id)
                    .ExecuteDeleteAsync();

                await dbContext.ColourRoles
                    .AsNoTracking()
                    .Where(x => x.RoleId == roleId)
                    .ExecuteDeleteAsync();   
            }
            else
            {
                List<string> usernames = [];

                await foreach (var users in postUpdateUser.Guild.GetUsersAsync())
                {
                    usernames.AddRange(users
                        .Where(x => x.RoleIds.Contains(discordRole.Id))
                        .Select(x => x.Username));
                }

                await discordRole.ModifyAsync(props =>
                {
                    props.Name = $"colour: {string.Join(" & ", usernames)}";
                });
            }
        }
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