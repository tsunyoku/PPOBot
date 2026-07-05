using Discord;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using PPOBot.Entities;

namespace PPOBot.Services;

public class ColourRoleService(PPODbContext dbContext) : IColourRoleService
{
    public async Task AddColourRole(ulong userId, string colour, SocketInteractionContext context)
    {
        await UnsetColourRole(userId, context);

        var colourRole = await dbContext.ColourRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Colour == colour);
        
        var existingColourRole = colourRole != null;

        colourRole ??= await SetupNewColourRole(colour, context);

        var colourRoleMember = new ColourRoleMember
        {
            UserId = userId,
            RoleId = colourRole.RoleId,
        };
        
        await dbContext.ColourRoleMembers.AddAsync(colourRoleMember);
        await dbContext.SaveChangesAsync();
        
        var guildUser = (IGuildUser)context.User;
        await guildUser.AddRoleAsync(colourRole.RoleId);
        
        if (existingColourRole)
            await UpdateColourRole(colourRole.RoleId, context);
    }

    public async Task<bool> UnsetColourRole(ulong userId, SocketInteractionContext context)
    {
        var existingColourRole = await dbContext.ColourRoleMembers
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.UserId == context.User.Id);

        if (existingColourRole is null)
            return false;

        var guildUser = (IGuildUser)context.User;
        await guildUser.RemoveRoleAsync(existingColourRole.RoleId);

        await dbContext.ColourRoleMembers
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .ExecuteDeleteAsync();
        
        await UpdateColourRole(existingColourRole.RoleId, context);

        return true;
    }
    
    private async Task<ColourRole> SetupNewColourRole(string hexCode, SocketInteractionContext context)
    {
        var discordRole = await context.Guild.CreateRoleAsync(
            $"colour: {context.User.Username}",
            permissions: GuildPermissions.None,
            color: RoleColors.Solid(Color.Parse(hexCode, ColorType.CssHexColor)));

        var databaseRole = new ColourRole
        {
            RoleId = discordRole.Id,
            Colour = hexCode,
        };
        
        await dbContext.ColourRoles.AddAsync(databaseRole);
        await dbContext.SaveChangesAsync();

        return databaseRole;
    }

    private async Task UpdateColourRole(ulong roleId, SocketInteractionContext context)
    {
        var discordRole = await context.Guild.GetRoleAsync(roleId);

        var userIds = await dbContext.ColourRoleMembers
            .AsNoTracking()
            .Where(x => x.RoleId == roleId)
            .Select(x => x.UserId)
            .ToArrayAsync();

        if (!userIds.Any())
        {
            await discordRole.DeleteAsync();

            await dbContext.ColourRoles
                .AsNoTracking()
                .Where(x => x.RoleId == roleId)
                .ExecuteDeleteAsync();

            return;
        }

        var users = await context.Guild.SearchUsersAsyncV2(args: new MemberSearchPropertiesV2
        {
            AndQuery = new MemberSearchFilter
            {
                UserId = new MemberSearchSnowflakeQuery
                {
                    OrQuery = userIds,
                }
            }
        });

        var usernames = users.Members
            .Select(x => x.User.Username)
            .ToArray();

        await discordRole.ModifyAsync(props =>
        {
            props.Name = $"colour: {string.Join(" & ", usernames)}";
        });
    }
}