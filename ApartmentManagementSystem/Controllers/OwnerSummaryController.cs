using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    // Presidents can see their building; SuperAdmin can see any building (via route)
    [Authorize(Roles = "President,SuperAdmin")]
    public class OwnerSummaryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerSummaryController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /OwnerSummary/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            // Same building-scope pattern used in CommonBillController
            if (me?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid(); // :contentReference[oaicite:2]{index=2}

            // ---- Owners for this building (from flats) ----
            // Flat has FlatNumber and OwnerId (nullable) and belongs to a Building. :contentReference[oaicite:3]{index=3} :contentReference[oaicite:4]{index=4}
            var owners = await _context.Flats
                .Include(f => f.Owner)
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .Select(f => new { f.OwnerId, f.Owner!.Fullname })
                .Distinct()
                .ToListAsync();

            // Flats per owner -> "1A, 2B" etc. (Flat.FlatNumber) :contentReference[oaicite:5]{index=5}
            var flatGroups = await _context.Flats
                .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
                .GroupBy(f => f.OwnerId!)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    FlatsCsv = string.Join(", ", g.OrderBy(x => x.FlatNumber).Select(x => x.FlatNumber))
                })
                .ToListAsync();

            var flatsByOwner = flatGroups.ToDictionary(x => x.OwnerId, x => x.FlatsCsv);

            // ---- Allocations per owner for this building ----
            // ExpenseAllocation -> CommonBill (which has BuildingId). We filter by CommonBill.BuildingId. :contentReference[oaicite:6]{index=6} :contentReference[oaicite:7]{index=7}
            var allocAgg = await _context.ExpenseAllocations
                .Include(a => a.CommonBill)
                .Where(a => a.CommonBill!.BuildingId == buildingId)
                .GroupBy(a => a.OwnerId)
                .Select(g => new
                {
                    OwnerId = g.Key,
                    Total = g.Sum(x => x.AmountDue),
                    Paid = g.Where(x => x.IsPaid).Sum(x => x.AmountDue)
                })
                .ToListAsync();

            var totalsByOwner = allocAgg.ToDictionary(x => x.OwnerId, x => (x.Total, x.Paid));

            // Combine
            var rows = owners
                .Select(o => new OwnerSummaryRowViewModel
                {
                    OwnerId = o.OwnerId!,
                    OwnerName = o.Fullname ?? "(no name)",
                    FlatsCsv = flatsByOwner.TryGetValue(o.OwnerId!, out var flats) ? flats : "",
                    TotalCommonBills = totalsByOwner.TryGetValue(o.OwnerId!, out var t) ? t.Total : 0m,
                    TotalPaid = totalsByOwner.TryGetValue(o.OwnerId!, out t) ? t.Paid : 0m
                })
                .OrderBy(r => r.OwnerName)
                .ToList();

            ViewData["BuildingId"] = buildingId;
            return View(rows);
        }
    }
}
