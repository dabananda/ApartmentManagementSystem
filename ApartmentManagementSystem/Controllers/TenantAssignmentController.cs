using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantAssignmentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public TenantAssignmentController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db; _users = users;
        }

        // GET: /TenantAssignment/Assign
        public async Task<IActionResult> Assign()
        {
            var me = await _users.GetUserAsync(User);

            // Owner's flats only (SuperAdmin/President see all)
            var flats = _db.Flats.AsQueryable();
            if (User.IsInRole("Owner"))
                flats = flats.Where(f => f.OwnerId == me!.Id);

            // Tenants who DO NOT have an active assignment anywhere (EndDate == null)
            var tenantsQ = _db.Users
                .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id &&
                             _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Tenant")))
                .Where(u => !_db.TenantAssignments.Any(a => a.TenantUserId == u.Id && a.EndDate == null));

            var vm = new AssignTenantVM
            {
                Flats = await flats.OrderBy(f => f.FlatNumber).ToListAsync(),
                Tenants = await tenantsQ.OrderBy(u => u.Fullname ?? u.Email).ToListAsync()
            };
            return View(vm);
        }

        // POST: /TenantAssignment/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTenantVM vm)
        {
            if (!ModelState.IsValid) return await Assign();

            var me = await _users.GetUserAsync(User);
            var flat = await _db.Flats.FindAsync(vm.FlatId);
            if (flat == null) return NotFound("Flat not found.");

            // Owner can only assign to their own flat
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            var tenantUser = await _users.FindByIdAsync(vm.TenantUserId);
            if (tenantUser == null) return NotFound("Tenant user not found.");

            // 🚫 Block tenants who already have an active assignment anywhere
            var activeForTenant = await _db.TenantAssignments
                .Where(a => a.TenantUserId == vm.TenantUserId && a.EndDate == null)
                .FirstOrDefaultAsync();

            if (activeForTenant != null)
            {
                if (activeForTenant.FlatId == vm.FlatId)
                    ModelState.AddModelError(string.Empty, "This tenant is already assigned to this flat.");
                else
                    ModelState.AddModelError(string.Empty, "This tenant is already assigned to another flat.");
                return await Assign(); // reload lists & show error
            }

            var today = DateTime.Today;

            // End any active assignment(s) on THIS flat (keeps one active tenant per flat)
            var activeAssignmentsOnThisFlat = await _db.TenantAssignments
                .Where(a => a.FlatId == vm.FlatId && a.EndDate == null)
                .ToListAsync();
            foreach (var a in activeAssignmentsOnThisFlat)
                a.EndDate = today.AddDays(-1);

            // Add the new assignment
            await _db.TenantAssignments.AddAsync(new TenantAssignment
            {
                FlatId = vm.FlatId,
                TenantUserId = vm.TenantUserId,
                StartDate = today,
                EndDate = null
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("IX_TenantAssignments_TenantUserId_Active") == true ||
                ex.InnerException?.Message.Contains("IX_TenantAssignments_FlatId_Active") == true)
            {
                // Friendly message if a race condition slipped through and DB blocked it
                ModelState.AddModelError(string.Empty, "Another assignment already exists. Please refresh and try again.");
                return await Assign();
            }

            // keep your existing on-demand bill generation block here...

            TempData["Success"] = "Tenant assigned to flat.";
            return RedirectToAction(nameof(MyTenants));
        }

        // GET: /TenantAssignment/MyTenants
        public async Task<IActionResult> MyTenants()
        {
            var me = await _users.GetUserAsync(User);

            var q = _db.TenantAssignments
                .Include(a => a.Flat)
                .Include(a => a.TenantUser)
                .Where(a => a.EndDate == null);

            if (User.IsInRole("Owner"))
                q = q.Where(a => a.Flat!.OwnerId == me!.Id);

            var data = await q
                .OrderBy(a => a.Flat!.FlatNumber)
                .Select(a => new MyTenantRow
                {
                    TenantUserId = a.TenantUserId,
                    TenantName = a.TenantUser!.Fullname ?? a.TenantUser.UserName!,
                    Email = a.TenantUser!.Email!,
                    FlatNumber = a.Flat!.FlatNumber,
                    From = a.StartDate
                }).ToListAsync();

            return View(data);
        }
    }
}

