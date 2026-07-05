using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using PPOBot.Entities;

namespace PPOBot.Modules;

[Group("colour-role", "Colour role related commands")]
public class ColourRoleModule(PPODbContext dbContext) : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("set", "Sets a colour role for the user")]
    public async Task SetColourRole(
        [Summary(description: "The hex code of the colour to add")] string hexCode)
    {
        await DeferAsync(ephemeral: true);

        var normalisedColour = hexCode.TrimStart('#').ToLowerInvariant();

        var guildUser = (IGuildUser)Context.User;

        var otherColourRoleIds = await dbContext.ColourRoles
            .AsNoTracking()
            .Where(x => guildUser.RoleIds.Contains(x.RoleId) && x.Colour != normalisedColour)
            .Select(x => x.RoleId)
            .ToArrayAsync();

        await guildUser.RemoveRolesAsync(otherColourRoleIds);

        var existingColourRole = await dbContext.ColourRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Colour == normalisedColour);

        if (existingColourRole is null)
        {
            await SetupNewColourRole(normalisedColour);
        }
        else
        {
            await AmendExistingColourRole(existingColourRole);
        }

        await FollowupAsync("Role assigned!", ephemeral: true);
    }

    [SlashCommand("copy", "Inherit the colour role of another user")]
    public async Task CopyColourRole(
        [Summary(description: "The user to copy from")] SocketUser user)
    {
        await DeferAsync(ephemeral: true);

        var guildUserToCopy = (IGuildUser)user;

        var colourRole = await dbContext.ColourRoles
            .AsNoTracking()
            .Where(x => guildUserToCopy.RoleIds.Contains(x.RoleId))
            .SingleOrDefaultAsync();

        if (colourRole is null)
        {
            await FollowupAsync("This user does not have a colour role", ephemeral: true);
            return;
        }
        
        await AmendExistingColourRole(colourRole);
        await FollowupAsync("Role assigned!", ephemeral: true);
    }

    [SlashCommand("unset", "Unsets a colour role for the user")]
    public async Task UnsetColourRole()
    {
        await DeferAsync(ephemeral: true);
        
        var guildUser = (IGuildUser)Context.User;
        
        var existingColourRoleIds = await dbContext.ColourRoles
            .AsNoTracking()
            .Where(x => guildUser.RoleIds.Contains(x.RoleId))
            .Select(x => x.RoleId)
            .ToListAsync();

        if (!existingColourRoleIds.Any())
        {
            await FollowupAsync("You do not have a colour role assigned!", ephemeral: true);
            return;
        }

        await guildUser.RemoveRolesAsync(existingColourRoleIds);
        await FollowupAsync("Role unassigned!", ephemeral: true);
    }

    private async Task SetupNewColourRole(string hexCode)
    {
        var discordRole = await Context.Guild.CreateRoleAsync(
            $"colour: {Context.User.Username}",
            permissions: GuildPermissions.None,
            color: RoleColors.Solid(Color.Parse(hexCode, ColorType.CssHexColor)));
        
        var guildUser = (IGuildUser)Context.User;
        await guildUser.AddRoleAsync(discordRole.Id);

        var databaseRole = new ColourRole
        {
            RoleId = discordRole.Id,
            Colour = hexCode,
        };
        
        await dbContext.ColourRoles.AddAsync(databaseRole);
        await dbContext.SaveChangesAsync();
    }

    private async Task AmendExistingColourRole(ColourRole existingColourRole)
    {
        var discordRole = await Context.Guild.GetRoleAsync(existingColourRole.RoleId);

        List<string> usernames = [];

        await foreach (var users in Context.Guild.GetUsersAsync())
        {
            usernames.AddRange(users
                .Where(x => x.RoleIds.Contains(discordRole.Id))
                .Select(x => x.Username));
        }
        
        // add so new user appears last
        usernames.Add(Context.User.Username);

        await discordRole.ModifyAsync(props =>
        {
            props.Name = $"colour: {string.Join(" & ", usernames)}";
        });
        
        
        var guildUser = (IGuildUser)Context.User;
        await guildUser.AddRoleAsync(discordRole.Id);
    }
}