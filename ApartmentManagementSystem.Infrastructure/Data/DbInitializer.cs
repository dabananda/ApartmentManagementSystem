using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ApartmentManagementSystem.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string superAdminEmail,
            string superAdminPassword)
        {
            string[] roleNames = { Roles.SuperAdmin, Roles.President, Roles.Owner, Roles.Tenant, Roles.Staff, Roles.User };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
            if (await userManager.FindByEmailAsync(superAdminEmail) == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    Fullname = "Super Admin",
                    EmailConfirmed = true,
                    IsApproved = true
                };
                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, Roles.SuperAdmin);
                }
            }
        }
    }
}
