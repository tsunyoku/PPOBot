using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PPOBot.Entities;

[Table("colour_role_members")]
public class ColourRoleMember
{
    [Key]
    [Column("user_id")]
    public required ulong UserId { get; init; }
    
    [Column("role_id")]
    public required ulong RoleId { get; init; }

    [ForeignKey(nameof(RoleId))]
    public ColourRole ColourRole { get; init; } = null!;
}