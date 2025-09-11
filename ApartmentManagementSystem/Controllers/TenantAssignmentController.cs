using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
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

            var vm = new AssignTenantVM
            {
                Flats = await flats.OrderBy(f => f.FlatNumber).ToListAsync(),
                Tenants = await _db.Users
                    .Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Tenant")))
                    .OrderBy(u => u.Fullname ?? u.Email).ToListAsync()
            };
            return View(vm);
        }

        // POST: /TenantAssignment/Assign
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTenantVM vm)
        {
            if (!ModelState.IsValid)
            {
                // Reload lists for the form
                return await Assign();
            }

            var me = await _users.GetUserAsync(User);
            var flat = await _db.Flats.FindAsync(vm.FlatId);
            if (flat == null) return NotFound("Flat not found.");

            // Owner can only assign to their own flat
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            var tenantUser = await _users.FindByIdAsync(vm.TenantUserId);
            if (tenantUser == null) return NotFound("Tenant user not found.");

            var today = DateTime.Today;

            // End any active assignment(s) on this flat
            var activeAssignments = await _db.TenantAssignments
                .Where(a => a.FlatId == vm.FlatId && a.EndDate == null)
                .ToListAsync();

            foreach (var a in activeAssignments)
                a.EndDate = today.AddDays(-1);

            // Add the new assignment starting today
            await _db.TenantAssignments.AddAsync(new TenantAssignment
            {
                FlatId = vm.FlatId,
                TenantUserId = vm.TenantUserId,
                StartDate = today,
                EndDate = null
            });

            await _db.SaveChangesAsync();

            // === On-demand current-month bill generation ===
            var profile = await _db.FlatBillingProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.FlatId == vm.FlatId && p.IsActive);

            if (profile != null)
            {
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);

                // Only create if this assignment is effective on/before this month
                var assignmentStartMonth = new DateTime(today.Year, today.Month, 1); // StartDate = today
                if (assignmentStartMonth <= firstOfMonth)
                {
                    var exists = await _db.TenantBills.AnyAsync(b =>
                        b.FlatId == vm.FlatId &&
                        b.TenantUserId == vm.TenantUserId &&
                        b.BillDate == firstOfMonth);

                    if (!exists)
                    {
                        await _db.TenantBills.AddAsync(new TenantBill
                        {
                            FlatId = vm.FlatId,
                            TenantUserId = vm.TenantUserId,
                            Title = string.IsNullOrWhiteSpace(profile.Title) ? "Monthly Rent" : profile.Title,
                            BillDate = firstOfMonth,
                            Amount = profile.MonthlyAmount
                        });
                        await _db.SaveChangesAsync();
                    }
                }
            }

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

