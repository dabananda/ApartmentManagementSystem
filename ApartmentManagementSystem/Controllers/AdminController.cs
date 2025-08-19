using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
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

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
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
    }
}
