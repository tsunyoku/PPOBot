using Discord.Interactions;
using PPOBot.Enums;

namespace PPOBot.Attributes;

public class RequiresPPORole : RequireRoleAttribute
{
    private RequiresPPORole(ulong roleId) : base(roleId)
    {
    }

    private RequiresPPORole(string roleName) : base(roleName)
    {
    }

    public RequiresPPORole(PPORole role) : this((ulong)role)
    {
    }

    public override string ErrorMessage => "You do not have the required permissions to use this command.";
}