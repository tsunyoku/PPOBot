using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PPOBot.Entities;

[Table("colour_roles")]
public class ColourRole
{
    [Key]
    [Column("role_id")]
    public required ulong RoleId { get; init; }
    
    [Column("colour")]
    [MaxLength(100)]
    public required string Colour { get; init; }
}