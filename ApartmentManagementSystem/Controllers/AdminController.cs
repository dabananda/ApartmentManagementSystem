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

        // POST: Assign President to Building
        [HttpPost]
        public async Task<IActionResult> AssignPresident(Guid buildingId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound("User not found.");

            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound("Building not found.");

            user.BuildingId = buildingId;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Building");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["Buildings"] = new SelectList(await _context.Buildings.ToListAsync(), "Id", "Name");
            ViewData["Presidents"] = new SelectList(await _userManager.GetUsersInRoleAsync("President"), "Id", "Fullname");
            return View();
        }

        // GET: Register a new user
        public async Task<IActionResult> RegisterUser()
        {
            var roles = await _roleManager.Roles.Where(r => r.Name != "SuperAdmin").ToListAsync();
            ViewData["Roles"] = new SelectList(roles, "Name", "Name");
            return View();
        }

        // POST: Admin/RegisterUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterUser(RegisterUserViewModel model)
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
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // THIS IS THE FIX: Re-populate the roles dropdown on validation failure
            var roles = _roleManager.Roles.Where(r => r.Name != "SuperAdmin").ToList();
            ViewData["Roles"] = new SelectList(roles, "Name", "Name");

            // Return the view with the model to display any errors
            return View(model);
        }
    }
}
