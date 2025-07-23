using Discord;
using Discord.Interactions;
using PPOBot.Attributes;
using PPOBot.Enums;

namespace PPOBot.Handlers;

public class DiscordSlashCommandHandler(ILogger<DiscordSlashCommandHandler> logger) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("purge", "Purge messages")]
    [RequiresPPORole(PPORole.Mod)]
    public async Task Purge([Summary(description: "The amount of messages to purge")] int amount)
    {
        logger.LogInformation(
            "User {@DiscordUsername} purged {@Amount} messages in channel {@ChannelName}",
            Context.User.Username,
            amount,
            Context.Channel.Name);

        var messages = await Context.Channel.GetMessagesAsync(amount).FlattenAsync();
        
        foreach (var message in messages)
            await Context.Channel.DeleteMessageAsync(message);
    }
}