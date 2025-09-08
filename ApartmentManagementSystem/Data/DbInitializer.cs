using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Identity;

namespace ApartmentManagementSystem.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            string superAdminPassword)
        {
            // Roles (added "User" for pending registrants)
            string[] roleNames = { "SuperAdmin", "President", "Owner", "Tenant", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed SuperAdmin
            if (await userManager.FindByEmailAsync("superadmin@ams.com") == null)
            {
                var superAdmin = new ApplicationUser
                {
                    UserName = "superadmin@ams.com",
                    Email = "superadmin@ams.com",
                    Fullname = "Super Admin",
                    EmailConfirmed = true,
                    IsApproved = true
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
