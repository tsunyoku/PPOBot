using Discord;
using Discord.Interactions;
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

        var existingColourRole = await dbContext.ColourRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Colour.Equals(hexCode, StringComparison.InvariantCultureIgnoreCase));

        if (existingColourRole is null)
        {
            await SetupNewColourRole(hexCode);
        }
        else
        {
            await AmendExistingColourRole(existingColourRole);
        }

        await FollowupAsync("Role assigned!", ephemeral: true);
    }

    [SlashCommand("unset", "Unsets a colour role for the user")]
    public async Task UnsetColourRole()
    {
        await DeferAsync(ephemeral: true);
        
        var guildUser = (IGuildUser)Context.User;
        
        var existingColourRole = await dbContext.ColourRoles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => guildUser.RoleIds.Contains(x.RoleId));

        if (existingColourRole is null)
        {
            await FollowupAsync("You do not have a colour role assigned!", ephemeral: true);
            return;
        }

        await guildUser.RemoveRoleAsync(existingColourRole.RoleId);
        await FollowupAsync("Role unassigned!", ephemeral: true);
    }

    private async Task SetupNewColourRole(string hexCode)
    {
        var discordRole = await Context.Guild.CreateRoleAsync(
            $"colour: {Context.User.Username}",
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