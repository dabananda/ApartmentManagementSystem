using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AMS.Application.Features.Administration;

public static class UserRoleHelper
{
    private static readonly string[] AllRoles =
        [Roles.User, Roles.Staff, Roles.Tenant, Roles.Owner, Roles.President];

    public static readonly string[] ApprovableRoles =
        [Roles.Owner, Roles.Tenant, Roles.Staff];

    public static bool IsValidApprovalRole(string role) =>
        ApprovableRoles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public static async Task EnsureOnlyRolesAsync(UserManager<ApplicationUser> userManager, ApplicationUser user, params string[] rolesToKeep)
    {
        foreach (var r in AllRoles)
            if (await userManager.IsInRoleAsync(user, r))
                await userManager.RemoveFromRoleAsync(user, r);

        foreach (var r in rolesToKeep.Distinct())
            await userManager.AddToRoleAsync(user, r);
    }

    public static async Task HandlePresidentRoleChangeAsync(
        UserManager<ApplicationUser> userManager, ApplicationUser user, string role, bool callerIsSuperAdmin)
    {
        if (callerIsSuperAdmin)
        {
            await EnsureOnlyRolesAsync(userManager, user, role);
        }
        else
        {
            if (!await userManager.IsInRoleAsync(user, Roles.President))
                await userManager.AddToRoleAsync(user, Roles.President);
            if (!await userManager.IsInRoleAsync(user, Roles.Owner))
                await userManager.AddToRoleAsync(user, Roles.Owner);
        }
    }
}
