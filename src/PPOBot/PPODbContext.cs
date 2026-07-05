using Microsoft.EntityFrameworkCore;
using PPOBot.Entities;

namespace PPOBot;

public class PPODbContext(DbContextOptions<PPODbContext> options) : DbContext(options)
{
    public DbSet<ColourRole> ColourRoles { get; set; } = null!;

    public DbSet<ColourRoleMember> ColourRoleMembers { get; set; } = null!;
}