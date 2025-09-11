using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using ApartmentManagementSystem.ViewModels.Owner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Owner/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();

            var ownerId = me.Id;
            var today = DateTime.Today;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // -------- Flats owned / occupied --------
            var flatsOwnedQ = _context.Flats.Where(f => f.OwnerId == ownerId);
            var flatsOwnedCount = await flatsOwnedQ.CountAsync();

            var occupiedFlatCount = await _context.TenantAssignments
                .Where(a => a.EndDate == null && _context.Flats.Any(f => f.Id == a.FlatId && f.OwnerId == ownerId))
                .Select(a => a.FlatId)
                .Distinct()
                .CountAsync();

            // -------- Tenant Rent totals (TenantBills / TenantPayments) --------
            var ownerBillsQ = _context.TenantBills
                .Include(b => b.Payments)
                .Include(b => b.Flat)
                .Where(b => b.Flat!.OwnerId == ownerId);

            var rentTotals = await ownerBillsQ
                .Select(b => new
                {
                    b.Amount,
                    Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
                }).ToListAsync();

            var rentTotalBilled = rentTotals.Sum(x => x.Amount);
            var rentTotalPaid = rentTotals.Sum(x => x.Paid);
            var rentPaidThisMonth = await _context.TenantPayments
                .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)
                .Where(p => p.PaymentDate >= monthStart && p.TenantBill!.Flat!.OwnerId == ownerId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // -------- Common Bill totals (ExpenseAllocations / ExpenseAllocationPayments) --------
            var commonAllocationsQ = _context.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Where(a => a.OwnerId == ownerId);

            var commonTotalBilled = await commonAllocationsQ.SumAsync(a => (decimal?)a.AmountDue) ?? 0m;

            var commonTotalPaid = await _context.ExpenseAllocationPayments
                .Where(p => p.OwnerId == ownerId)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            // -------- Active Tenants list (from TenantAssignments) --------
            var tenants = await _context.TenantAssignments
                .Include(a => a.TenantUser)
                .Include(a => a.Flat)
                .Where(a => a.EndDate == null && a.Flat!.OwnerId == ownerId)
                .OrderBy(a => a.Flat!.FlatNumber)
                .Select(a => new OwnerTenantRow
                {
                    TenantUserId = a.TenantUserId,
                    Name = a.TenantUser!.Fullname ?? a.TenantUser.UserName!,
                    Email = a.TenantUser!.Email!,
                    FlatNumber = a.Flat!.FlatNumber,
                    From = a.StartDate
                })
                .ToListAsync();

            // -------- Recent Rent Payments (10) --------
            var recentRent = await _context.TenantPayments
                .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)
                .Include(p => p.TenantBill)!.ThenInclude(b => b.TenantUser)
                .Where(p => p.TenantBill!.Flat!.OwnerId == ownerId)
                .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
                .Take(10)
                .Select(p => new OwnerRecentRentPaymentRow
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    TenantName = (p.TenantBill!.TenantUser!.Fullname ?? p.TenantBill!.TenantUser!.UserName)!,
                    FlatNumber = p.TenantBill!.Flat!.FlatNumber
                })
                .ToListAsync();

            // -------- Recent Common Bill Payments (10) --------
            var recentCommon = await _context.ExpenseAllocationPayments
                .Include(p => p.ExpenseAllocation)!.ThenInclude(a => a.CommonBill)
                .Where(p => p.OwnerId == ownerId)
                .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
                .Take(10)
                .Select(p => new OwnerRecentCommonPaymentRow
                {
                    PaymentId = p.Id,
                    PaymentDate = p.PaymentDate,
                    Amount = p.Amount,
                    Reference = p.Reference,
                    BillTitle = p.ExpenseAllocation!.CommonBill!.Name,
                    BillDate = p.ExpenseAllocation!.CommonBill!.BillDate
                })
                .ToListAsync();

            var vm = new OwnerDashboardVM
            {
                FlatsOwnedCount = flatsOwnedCount,
                FlatsOccupiedCount = occupiedFlatCount,

                RentTotalBilled = rentTotalBilled,
                RentTotalPaid = rentTotalPaid,
                RentPaidThisMonth = rentPaidThisMonth,

                CommonTotalBilled = commonTotalBilled,
                CommonTotalPaid = commonTotalPaid,

                Tenants = tenants,
                RecentRent = recentRent,
                RecentCommon = recentCommon
            };

            // If you had an existing larger Dashboard model, attach vm fields there instead.
            return View(vm);
        }

        // GET: Owner/OwnedFlats
        public async Task<IActionResult> OwnedFlats(string? ownerId = null)
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();

            // Scope: Owner can ONLY see their own flats; President/SA can optionally pass ownerId
            var targetOwnerId = User.IsInRole("Owner") ? me.Id : (ownerId ?? me.Id);

            var today = DateTime.Today;

            var rows = await _context.Flats
                .Include(f => f.Building)
                .Where(f => f.OwnerId == targetOwnerId)
                .Select(f => new OwnerOwnedFlatRow
                {
                    FlatId = f.Id,
                    FlatNumber = f.FlatNumber,
                    BuildingId = f.BuildingId,
                    BuildingName = f.Building!.Name,

                    CurrentTenantUserId = _context.TenantAssignments
                        .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                        .OrderByDescending(a => a.StartDate)
                        .Select(a => a.TenantUserId)
                        .FirstOrDefault(),

                    CurrentTenantName = _context.TenantAssignments
                        .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                        .OrderByDescending(a => a.StartDate)
                        .Select(a => a.TenantUser!.Fullname ?? a.TenantUser.UserName)
                        .FirstOrDefault(),

                    CurrentTenantEmail = _context.TenantAssignments
                        .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                        .OrderByDescending(a => a.StartDate)
                        .Select(a => a.TenantUser!.Email)
                        .FirstOrDefault(),

                    TenantFrom = _context.TenantAssignments
                        .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                        .OrderByDescending(a => a.StartDate)
                        .Select(a => (DateTime?)a.StartDate)
                        .FirstOrDefault()
                })
                .OrderBy(x => x.BuildingName).ThenBy(x => x.FlatNumber)
                .ToListAsync();

            ViewBag.TargetOwnerId = targetOwnerId;
            return View(rows);
        }
    }
}