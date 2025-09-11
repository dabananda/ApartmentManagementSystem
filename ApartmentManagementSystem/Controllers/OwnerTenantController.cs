using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "Owner,SuperAdmin,President")]
    public class OwnerTenantController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly RoleManager<IdentityRole> _roles;

        public OwnerTenantController(ApplicationDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles)
        {
            _db = db; _users = users; _roles = roles;
        }

        // GET: /OwnerTenant/Create
        [Authorize(Roles = "Owner,SuperAdmin,President")]
        public IActionResult Create() => View(new CreateTenantVM());

        // POST: /OwnerTenant/Create
        [HttpPost, ValidateAntiForgeryToken]
        [Authorize(Roles = "Owner,SuperAdmin,President")]
        public async Task<IActionResult> Create(CreateTenantVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var existing = await _users.FindByEmailAsync(vm.Email);
            if (existing != null)
            {
                ModelState.AddModelError("", "A user with that email already exists.");
                return View(vm);
            }

            // ensure Tenant role exists
            if (!await _roles.RoleExistsAsync("Tenant"))
                await _roles.CreateAsync(new IdentityRole("Tenant"));

            var tenant = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                Fullname = vm.Fullname,   // matches your ApplicationUser
                EmailConfirmed = true     // optional
            };

            var result = await _users.CreateAsync(tenant, vm.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            await _users.AddToRoleAsync(tenant, "Tenant");

            TempData["Success"] = "Tenant user created.";
            return RedirectToAction(nameof(List));
        }

        // GET: /OwnerTenant/List  -> all TENANT users (for selection/assignment)
        [Authorize(Roles = "Owner,SuperAdmin,President")]
        public async Task<IActionResult> List()
        {
            // only show pure Tenant users
            var tenants = await _db.Users
                .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Tenant")))
                .OrderBy(u => u.Fullname ?? u.Email)
                .ToListAsync();

            return View(tenants);
        }
    }
}
