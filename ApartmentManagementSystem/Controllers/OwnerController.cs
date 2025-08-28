using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "Owner,SuperAdmin")]
    public class OwnerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public OwnerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            var ownedFlats = await _context.Flats
                                           .Include(f => f.Tenants)
                                           .Include(f => f.Building)
                                           .Where(f => f.OwnerId == user.Id)
                                           .ToListAsync();

            var buildingName = ownedFlats.FirstOrDefault()?.Building.Name ?? "N/A";
            var buildingAddress = ownedFlats.FirstOrDefault()?.Building.Address ?? "N/A";

            // Get all expense allocations for flats owned by the user
            var allAllocations = await _context.ExpenseAllocations
                                               .Include(a => a.CommonBill)
                                               .Where(a => a.OwnerId == user.Id)
                                               .ToListAsync();

            var totalBillsDue = allAllocations.Where(a => !a.IsPaid).Sum(a => a.AmountDue);
            var totalBillsPaid = allAllocations.Where(a => a.IsPaid).Sum(a => a.AmountDue);

            // Get all rent collections for flats owned by the user
            var rentCollections = await _context.Rents
                                                .Include(r => r.Tenant)
                                                .Include(r => r.Tenant.Flat) // Eagerly load flat to display flat number
                                                .Where(r => r.Tenant.Flat.OwnerId == user.Id)
                                                .ToListAsync();

            var totalRentCollected = rentCollections.Sum(r => r.Amount);

            var viewModel = new OwnerDashboardViewModel
            {
                OwnerName = user.Fullname,
                BuildingName = buildingName,
                BuildingAddress = buildingAddress,
                TotalFlatsOwned = ownedFlats.Count,
                OccupiedFlats = ownedFlats.Count(f => f.IsOccupied),
                VacantFlats = ownedFlats.Count(f => !f.IsOccupied),
                TotalBillsDue = totalBillsDue,
                TotalBillsPaid = totalBillsPaid,
                TotalRentCollected = totalRentCollected,
                FinancialBalance = totalRentCollected - totalBillsPaid,
                ExpenseAllocations = allAllocations,
                RentCollections = rentCollections
            };

            return View(viewModel);
        }

        // GET: Owner/MyFlats
        public async Task<IActionResult> MyFlats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            // Get all flats owned by the current user and eagerly load the Building and Tenants
            var ownedFlats = await _context.Flats
                                           .Include(f => f.Building)
                                           .Include(f => f.Tenants)
                                           .Where(f => f.OwnerId == user.Id)
                                           .ToListAsync();

            var viewModel = ownedFlats.Select(f => new OwnerFlatsViewModel
            {
                Id = f.Id,
                FlatNumber = f.FlatNumber,
                BuildingName = f.Building.Name,
                IsOccupied = f.IsOccupied,
                Tenants = f.Tenants
            }).ToList();

            return View(viewModel);
        }

        // POST: Owner/ToggleFlatOccupancy/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFlatOccupancy(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FirstOrDefaultAsync(f => f.Id == id);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            flat.IsOccupied = !flat.IsOccupied; // Toggle the status
            _context.Update(flat);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyFlats));
        }

        // GET: Owner/MyTenants
        public async Task<IActionResult> MyTenants()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Forbid();
            }

            // Get all tenants from flats owned by the current user
            var myTenants = await _context.Tenants
                                          .Include(t => t.Flat)
                                          .ThenInclude(f => f.Building)
                                          .Where(t => t.Flat.OwnerId == user.Id)
                                          .OrderBy(t => t.Flat.FlatNumber)
                                          .ThenBy(t => t.Fullname)
                                          .ToListAsync();

            ViewData["OwnerName"] = user.Fullname;

            return View(myTenants);
        }
    }
}