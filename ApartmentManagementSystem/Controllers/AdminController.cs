using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: View of Assign President to Building
        [HttpGet]
        public async Task<IActionResult> AssignPresident()
        {
            var buildings = await _context.Buildings.ToListAsync();
            var presidents = await _userManager.GetUsersInRoleAsync("President");

            ViewData["Buildings"] = new SelectList(buildings, "Id", "Name");
            ViewData["Presidents"] = new SelectList(presidents, "Id", "UserName");

            return View();
        }

        // POST: Admin/AssignPresident
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPresident(AssignPresidentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "User not found.");
                    return View(model);
                }

                // Unassign existing president from the building
                var oldPresident = await _context.Users.FirstOrDefaultAsync(u => u.BuildingId == model.BuildingId);
                if (oldPresident != null)
                {
                    oldPresident.BuildingId = null;
                    await _userManager.UpdateAsync(oldPresident);
                    // Optionally remove the "President" role from the old president
                    // await _userManager.RemoveFromRoleAsync(oldPresident, "President");
                }

                // Assign the new president to the building
                user.BuildingId = model.BuildingId;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    // Add the "President" role to the user
                    await _userManager.AddToRoleAsync(user, "President");

                    TempData["SuccessMessage"] = "President assigned successfully.";
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If model state is invalid, re-populate the view model
            var buildings = await _context.Buildings.ToListAsync();
            var users = await _userManager.GetUsersInRoleAsync("Owner");

            model.Buildings = new SelectList(buildings, "Id", "Name");
            model.Users = new SelectList(users, "Id", "Fullname");

            return View(model);
        }

        // GET: Admin/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            var roles = await _roleManager.Roles.Where(r => r.Name != "SuperAdmin").ToListAsync();
            ViewData["Roles"] = new SelectList(roles, "Name", "Name");
            return View();
        }

        // POST: Admin/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(RegisterUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    Fullname = model.Fullname,
                    UserName = model.Email,
                    Email = model.Email,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.SelectedRole);

                    // Add the 'Owner' role if the user is a President
                    if (model.SelectedRole == "President")
                    {
                        await _userManager.AddToRoleAsync(user, "Owner");
                    }

                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Re-populate the roles dropdown on validation failure
            var roles = _roleManager.Roles.Where(r => r.Name != "SuperAdmin").ToList();
            ViewData["Roles"] = new SelectList(roles, "Name", "Name");
            return View(model);
        }

        // GET: Admin/ApproveOwners
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> ApproveOwners()
        {
            // Find all users who are not assigned a role yet
            var users = await _userManager.GetUsersInRoleAsync("User");
            // Find users who are not in any of the specific roles
            //var users = _context.Users.Where(u => !u.Roles.Any()).ToListAsync();
            return View(users);
        }

        // POST: Admin/ApproveOwner/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> ApproveOwner(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Add the "Owner" role to the user
            var result = await _userManager.AddToRoleAsync(user, "Owner");

            // Remove the default "User" role if needed
            // await _userManager.RemoveFromRoleAsync(user, "User");

            if (!result.Succeeded)
            {
                // Handle errors if role assignment fails
                TempData["Error"] = "Failed to approve owner.";
            }

            return RedirectToAction(nameof(ApproveOwners));
        }
    }
}
