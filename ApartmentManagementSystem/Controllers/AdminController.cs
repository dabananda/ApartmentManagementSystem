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
            var owners = await _userManager.GetUsersInRoleAsync("Owner");

            // Display both Fullname and Email in the dropdown
            var ownersSelectList = owners.Select(o => new SelectListItem
            {
                Value = o.Id,
                Text = $"{o.Fullname} ({o.Email})"
            }).ToList();

            ViewData["Buildings"] = new SelectList(buildings, "Id", "Name");
            ViewData["Owners"] = ownersSelectList;

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

                var isPresident = await _userManager.IsInRoleAsync(user, "President");
                if (isPresident)
                {
                    ModelState.AddModelError(string.Empty, "User is already a president.");
                    return View(model);
                }

                var result = await _userManager.AddToRoleAsync(user, "President");
                if (!result.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Failed to assign president role.");
                    return View(model);
                }

                user.BuildingId = model.BuildingId;
                var updateResult = await _userManager.UpdateAsync(user);

                if (updateResult.Succeeded)
                {
                    TempData["Success"] = "President assigned successfully.";
                    return RedirectToAction(nameof(AssignPresident));
                }
                else
                {
                    TempData["Error"] = "Failed to update user's building.";
                    await _userManager.RemoveFromRoleAsync(user, "President");
                    return View(model);
                }
            }

            var buildings = await _context.Buildings.ToListAsync();
            var owners = await _userManager.GetUsersInRoleAsync("Owner");

            // Repopulate the dropdown with both Fullname and Email on validation failure
            var ownersSelectListOnFail = owners.Select(o => new SelectListItem
            {
                Value = o.Id,
                Text = $"{o.Fullname} ({o.Email})"
            }).ToList();

            ViewData["Buildings"] = new SelectList(buildings, "Id", "Name", model.BuildingId);
            ViewData["Owners"] = ownersSelectListOnFail;

            return View(model);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users
                .Where(x => x.UserName != currentUser.UserName)
                .Include(u => u.Building)
                .Include(u => u.OwnedFlats)
                .ToListAsync();

            var userViewModels = new List<UserDetailsViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var flatCount = user.OwnedFlats?.Count ?? 0;

                // Get tenant count for owned flats
                var tenantCount = 0;
                if (user.OwnedFlats != null && user.OwnedFlats.Any())
                {
                    var flatIds = user.OwnedFlats.Select(f => f.Id).ToList();
                    tenantCount = await _context.Tenants
                        .CountAsync(t => flatIds.Contains(t.FlatId) && t.IsActive);
                }

                // Get outstanding bills count
                var outstandingBills = await _context.ExpenseAllocations
                    .CountAsync(ea => ea.OwnerId == user.Id && !ea.IsPaid);

                // Get total outstanding amount
                var outstandingAmount = await _context.ExpenseAllocations
                    .Where(ea => ea.OwnerId == user.Id && !ea.IsPaid)
                    .SumAsync(ea => ea.AmountDue);

                userViewModels.Add(new UserDetailsViewModel
                {
                    Id = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList(),
                    BuildingName = user.Building?.Name,
                    BuildingAddress = user.Building?.Address,
                    FlatCount = flatCount,
                    TenantCount = tenantCount,
                    OutstandingBillsCount = outstandingBills,
                    OutstandingAmount = outstandingAmount,
                    LastLoginDate = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now ? null : DateTime.Now.AddDays(-30), // Placeholder for last login
                    AccountStatus = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now ? "Locked" :
                                   user.EmailConfirmed ? "Active" : "Pending Verification"
                });
            }

            return View(userViewModels);
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
