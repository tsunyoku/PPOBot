using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PPOBot.Attributes;
using PPOBot.Entities;
using PPOBot.Enums;
using PPOBot.Services;

namespace PPOBot.Modules;

[Group("colour-role", "Colour role related commands")]
public class ColourRoleModule(IColourRoleService colourRoleService, PPODbContext dbContext) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set", "Sets a colour role for the user")]
    public async Task SetColourRole(
        [Summary(description: "The hex code of the colour to add")] string hexCode)
    {
        await DeferAsync(ephemeral: true);

        var normalisedColour = hexCode.TrimStart('#').ToLowerInvariant();

        await colourRoleService.AddColourRole(Context.User.Id, normalisedColour, Context);
        await FollowupAsync("Role assigned!", ephemeral: true);
    }

    [SlashCommand("copy", "Inherit the colour role of another user")]
    public async Task CopyColourRole(
        [Summary(description: "The user to copy from")] SocketUser user)
    {
        await DeferAsync(ephemeral: true);

        var guildUserToCopy = (IGuildUser)user;

        var colourRole = await dbContext.ColourRoleMembers
            .AsNoTracking()
            .Where(x => x.UserId == guildUserToCopy.Id)
            .Select(x => x.ColourRole)
            .SingleOrDefaultAsync();

        if (colourRole is null)
        {
            await FollowupAsync("This user does not have a colour role", ephemeral: true);
            return;
        }

        await colourRoleService.AddColourRole(Context.User.Id, colourRole.Colour, Context);
        await FollowupAsync("Role assigned!", ephemeral: true);
    }

    [SlashCommand("unset", "Unsets a colour role for the user")]
    public async Task UnsetColourRole()
    {
        await DeferAsync(ephemeral: true);
        
        var hadRole = await colourRoleService.UnsetColourRole(Context.User.Id, Context);

        if (hadRole)
            await FollowupAsync("Role unassigned!", ephemeral: true);
        else
            await FollowupAsync("You do not have a colour role assigned!", ephemeral: true);
    }

    [SlashCommand("sync", "Syncs the database with current colour role state")]
    [RequiresPPORole(PPORole.Mod)]
    public async Task SyncColourRoleState()
    {
        await DeferAsync(ephemeral: true);

        var colourRoleIds = await dbContext.ColourRoles
            .AsNoTracking()
            .Select(x => x.RoleId)
            .ToListAsync();

        var users = await Context.Guild.SearchUsersAsyncV2(args: new MemberSearchPropertiesV2
        {
            AndQuery = new MemberSearchFilter
            {
                RoleIds = new MemberSearchSnowflakeQuery
                {
                    OrQuery = colourRoleIds,
                }
            }
        });

        List<ColourRoleMember> colourRoleMembers = [];

        foreach (var member in users.Members)
        {
            var colourRoleId = member.User.RoleIds.Single(colourRoleIds.Contains);

            var colourRoleMember = new ColourRoleMember
            {
                UserId = member.User.Id,
                RoleId = colourRoleId,
            };
            
            colourRoleMembers.Add(colourRoleMember);
        }

        await dbContext.ColourRoleMembers.AddRangeAsync(colourRoleMembers);
        await dbContext.SaveChangesAsync();

        await FollowupAsync($"Added {colourRoleMembers.Count} members to the database", ephemeral: true);
    }
}