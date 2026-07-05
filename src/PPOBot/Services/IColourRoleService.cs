using Discord.Interactions;

namespace PPOBot.Services;

public interface IColourRoleService
{
    Task AddColourRole(ulong userId, string colour, SocketInteractionContext context);
    Task<bool> UnsetColourRole(ulong userId, SocketInteractionContext context);
}