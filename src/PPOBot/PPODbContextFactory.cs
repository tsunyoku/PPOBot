using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PPOBot;

public class PPODbContextFactory : IDesignTimeDbContextFactory<PPODbContext>
{
    public PPODbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PPODbContext>();

        optionsBuilder.UseNpgsql();
        
        return new PPODbContext(optionsBuilder.Options);
    }
}