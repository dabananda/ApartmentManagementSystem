using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace ApartmentManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, string superAdminPassword)
        {
            // create roles
            string[] roleNames = { "SuperAdmin", "President", "Owner", "Tenant" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // create super admin user
            if (await userManager.FindByEmailAsync("superadmin@ams.com") == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = "superadmin@ams.com",
                    Email = "superadmin@ams.com",
                    Fullname = "Super Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");
                }
            }
        }
    }
}
