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
    public class FlatBillingProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        public FlatBillingProfileController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db; _users = users;
        }

        // GET: /FlatBillingProfile/Index
        public async Task<IActionResult> Index()
        {
            var me = await _users.GetUserAsync(User);

            var flats = _db.Flats.AsQueryable();
            if (User.IsInRole("Owner"))
                flats = flats.Where(f => f.OwnerId == me!.Id);

            var rows = await flats
                .GroupJoin(_db.FlatBillingProfiles,
                           f => f.Id, p => p.FlatId,
                           (f, ps) => new { f, p = ps.FirstOrDefault() })
                .OrderBy(x => x.f.FlatNumber)
                .Select(x => new FlatProfileRow
                {
                    FlatId = x.f.Id,
                    FlatNumber = x.f.FlatNumber,
                    HasProfile = x.p != null,
                    Title = x.p != null ? x.p.Title : "",
                    Amount = x.p != null ? x.p.MonthlyAmount : 0m,
                    DueDay = x.p != null ? x.p.DueDayOfMonth : 1,
                    IsActive = x.p != null && x.p.IsActive
                }).ToListAsync();

            return View(rows);
        }

        // GET: /FlatBillingProfile/Edit/{flatId}
        public async Task<IActionResult> Edit(Guid flatId)
        {
            var me = await _users.GetUserAsync(User);
            var flat = await _db.Flats.FindAsync(flatId);
            if (flat == null) return NotFound();
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            var p = await _db.FlatBillingProfiles.FirstOrDefaultAsync(x => x.FlatId == flatId)
                ?? new FlatBillingProfile { FlatId = flatId };

            return View(p);
        }

        // POST: /FlatBillingProfile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FlatBillingProfile vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var me = await _users.GetUserAsync(User);
            var flat = await _db.Flats.FindAsync(vm.FlatId);
            if (flat == null) return NotFound("Flat not found.");

            // Owner can only edit profile for their own flat
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            // Upsert the billing profile
            var existing = await _db.FlatBillingProfiles.FirstOrDefaultAsync(x => x.FlatId == vm.FlatId);
            if (existing == null)
            {
                existing = new FlatBillingProfile
                {
                    FlatId = vm.FlatId,
                    Title = string.IsNullOrWhiteSpace(vm.Title) ? "Monthly Rent" : vm.Title,
                    MonthlyAmount = vm.MonthlyAmount,
                    DueDayOfMonth = vm.DueDayOfMonth <= 0 ? 1 : vm.DueDayOfMonth,
                    IsActive = vm.IsActive
                };
                _db.FlatBillingProfiles.Add(existing);
            }
            else
            {
                existing.Title = string.IsNullOrWhiteSpace(vm.Title) ? "Monthly Rent" : vm.Title;
                existing.MonthlyAmount = vm.MonthlyAmount;
                existing.DueDayOfMonth = vm.DueDayOfMonth <= 0 ? 1 : vm.DueDayOfMonth;
                existing.IsActive = vm.IsActive;
            }

            await _db.SaveChangesAsync();

            // === On-demand current-month bill generation (when profile is active) ===
            if (existing.IsActive)
            {
                var today = DateTime.Today;
                var firstOfMonth = new DateTime(today.Year, today.Month, 1);

                // Find the current active tenant (if any) for this flat
                var assignment = await _db.TenantAssignments
                    .Where(a => a.FlatId == vm.FlatId && (a.EndDate == null || a.EndDate >= today))
                    .OrderByDescending(a => a.StartDate)
                    .FirstOrDefaultAsync();

                if (assignment != null)
                {
                    var assignmentStartMonth = new DateTime(assignment.StartDate.Year, assignment.StartDate.Month, 1);
                    if (assignmentStartMonth <= firstOfMonth)
                    {
                        var existsBill = await _db.TenantBills.AnyAsync(b =>
                            b.FlatId == vm.FlatId &&
                            b.TenantUserId == assignment.TenantUserId &&
                            b.BillDate == firstOfMonth);

                        if (!existsBill)
                        {
                            await _db.TenantBills.AddAsync(new TenantBill
                            {
                                FlatId = vm.FlatId,
                                TenantUserId = assignment.TenantUserId,
                                Title = string.IsNullOrWhiteSpace(existing.Title) ? "Monthly Rent" : existing.Title,
                                BillDate = firstOfMonth,
                                Amount = existing.MonthlyAmount
                            });
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }

            TempData["Success"] = "Billing profile saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}