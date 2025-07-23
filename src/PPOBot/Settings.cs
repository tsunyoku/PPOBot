using System.ComponentModel.DataAnnotations;

namespace PPOBot;

public class Settings
{
    [Required]
    public required string DiscordToken { get; init; }
    
    [Required]
    public required ulong DiscordGuildId { get; init; }
}