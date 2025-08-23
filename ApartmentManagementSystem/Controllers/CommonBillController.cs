using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class CommonBillController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommonBillController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: CommonBill/Index/{buildingId}
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var bills = await _context.CommonBills
                                      .Where(b => b.BuildingId == buildingId)
                                      .OrderByDescending(b => b.BillDate)
                                      .ToListAsync();

            ViewData["BuildingId"] = buildingId;
            return View(bills);
        }

        // GET: CommonBill/Create/{buildingId}
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = buildingId;
            return View();
        }

        // POST: CommonBill/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,BillDate,TotalAmount,Notes,BuildingId")] CommonBill bill)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            if (ModelState.IsValid)
            {
                await _context.AddAsync(bill);
                await _context.SaveChangesAsync();

                // Allocate the bill to all flat owners in the building
                var owners = await _context.Flats
                    .Where(f => f.BuildingId == bill.BuildingId && f.OwnerId != null)
                    .Select(f => f.Owner)
                    .Distinct()
                    .ToListAsync();

                var totalFlats = await _context.Flats
                    .CountAsync(f => f.BuildingId == bill.BuildingId && f.OwnerId != null);

                if (totalFlats > 0)
                {
                    var amountPerFlat = bill.TotalAmount / totalFlats;
                    foreach (var owner in owners)
                    {
                        var ownerFlatCount = await _context.Flats.CountAsync(f => f.OwnerId == owner.Id);
                        var amountDue = amountPerFlat * ownerFlatCount;

                        var allocation = new ExpenseAllocation
                        {
                            CommonBillId = bill.Id,
                            OwnerId = owner.Id,
                            AmountDue = amountDue
                        };
                        await _context.AddAsync(allocation);
                    }
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
            }

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }
    }
}
