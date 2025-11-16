using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class CommonBillController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CommonBillController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

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

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = buildingId;
            return View(new CommonBill
            {
                BuildingId = buildingId.Value,
                BillDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,TotalAmount,Notes,BuildingId")] CommonBill bill)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            bill.BillDate = DateTime.Today;

            if (ModelState.IsValid)
            {
                await _context.AddAsync(bill);
                await _context.SaveChangesAsync();

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

        public async Task<IActionResult> Details(Guid id)
        {
            var bill = await _context.CommonBills
                .Include(b => b.Building)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var bill = await _context.CommonBills.FindAsync(id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,TotalAmount,Notes,BuildingId")] CommonBill input)
        {
            if (id != input.Id) return NotFound();

            var bill = await _context.CommonBills.FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewData["BuildingId"] = bill.BuildingId;
                return View(bill);
            }

            bill.Name = input.Name;
            bill.TotalAmount = input.TotalAmount;
            bill.Notes = input.Notes;

            _context.Update(bill);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Common bill updated successfully.";
            return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var bill = await _context.CommonBills
                .Include(b => b.Building)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var hasPayments = await _context.ExpensePayments.AnyAsync(p => p.CommonBillId == bill.Id);
            ViewData["HasPayments"] = hasPayments;

            return View(bill);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var bill = await _context.CommonBills.FirstOrDefaultAsync(b => b.Id == id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var hasPayments = await _context.ExpensePayments.AnyAsync(p => p.CommonBillId == bill.Id);
            if (hasPayments)
            {
                TempData["Error"] = "Cannot delete this common bill because there are recorded payments against it.";
                return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
            }

            var allocations = _context.ExpenseAllocations.Where(a => a.CommonBillId == bill.Id);
            _context.ExpenseAllocations.RemoveRange(allocations);

            _context.CommonBills.Remove(bill);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Common bill deleted.";
            return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
        }
    }
}
