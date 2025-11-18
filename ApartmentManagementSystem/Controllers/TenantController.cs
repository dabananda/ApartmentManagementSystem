using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Building;
using ApartmentManagementSystem.ViewModels.Flat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _config;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration config)
        {
            _context = context;
            _userManager = userManager;
            _config = config;
        }

        public async Task<IActionResult> ViewTenants(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();

            var flat = await _context.Flats
                .AsNoTracking()
                .Include(f => f.Building)
                .FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            var isOwner = flat.OwnerId == me.Id;
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isPresidentOfThisBuilding = User.IsInRole("President") && me.BuildingId == flat.BuildingId;

            if (!(isOwner || isSuperAdmin || isPresidentOfThisBuilding))
                return Forbid();

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["FlatId"] = flat.Id;

            var assignmentRows = await _context.TenantAssignments.AsNoTracking()
                .Include(a => a.TenantUser)
                .Where(a => a.FlatId == flat.Id && a.EndDate == null)
                .Select(a => new FlatTenantRow
                {
                    FlatId = a.FlatId,
                    FlatNumber = flat.FlatNumber,
                    TenantUserId = a.TenantUserId,
                    TenantName = a.TenantUser!.Fullname ?? a.TenantUser.UserName!,
                    Email = a.TenantUser!.Email!,
                    PhoneNumber = a.TenantUser!.PhoneNumber,
                    IsActive = true,
                    Source = "Assignment"
                })
                .ToListAsync();

            var assignedUserIds = assignmentRows
                .Select(r => r.TenantUserId)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .ToHashSet();

            var legacyRows = await _context.Tenants.AsNoTracking()
                .Where(t => t.FlatId == flat.Id && (t.UserId == null || !assignedUserIds.Contains(t.UserId)))
                .Select(t => new FlatTenantRow
                {
                    FlatId = t.FlatId,
                    FlatNumber = flat.FlatNumber,
                    LegacyTenantId = t.Id,
                    TenantUserId = t.UserId,
                    TenantName = t.Fullname,
                    Email = t.Email,
                    PhoneNumber = t.PhoneNumber,
                    IsActive = t.IsActive,
                    Source = "Legacy"
                })
                .ToListAsync();

            var rows = assignmentRows.Concat(legacyRows)
                .OrderBy(r => r.TenantName)
                .ToList();

            return View(rows);
        }

        public async Task<IActionResult> BuildingTenants()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();
            if (User.IsInRole("President") && user.BuildingId == null) return Forbid();

            var buildingId = user.BuildingId!.Value;

            var building = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == buildingId);
            if (building == null) return NotFound();

            var assignmentRows = await _context.TenantAssignments
                .Include(a => a.Flat)!.ThenInclude(f => f.Owner)
                .Include(a => a.TenantUser)
                .Where(a => a.EndDate == null && a.Flat!.BuildingId == buildingId)
                .Select(a => new BuildingTenantRow
                {
                    FlatId = a.FlatId,
                    FlatNumber = a.Flat!.FlatNumber,
                    TenantUserId = a.TenantUserId,
                    TenantName = a.TenantUser!.Fullname ?? a.TenantUser.UserName!,
                    Email = a.TenantUser!.Email!,
                    PhoneNumber = a.TenantUser!.PhoneNumber,
                    OwnerName = a.Flat!.Owner != null ? (a.Flat.Owner.Fullname ?? a.Flat.Owner.UserName!) : "",
                    IsActive = true,
                    Source = "Assignment"
                })
                .ToListAsync();

            var assignedFlatIds = assignmentRows.Select(r => r.FlatId).ToHashSet();

            var legacyRows = await _context.Tenants
                .Include(t => t.Flat)!.ThenInclude(f => f.Owner)
                .Where(t => t.IsActive && t.Flat!.BuildingId == buildingId && !assignedFlatIds.Contains(t.FlatId))
                .Select(t => new BuildingTenantRow
                {
                    FlatId = t.FlatId,
                    FlatNumber = t.Flat!.FlatNumber,
                    TenantUserId = t.UserId ?? "",
                    TenantName = t.Fullname,
                    Email = t.Email,
                    PhoneNumber = t.PhoneNumber,
                    OwnerName = t.Flat!.Owner != null ? (t.Flat.Owner.Fullname ?? t.Flat.Owner.UserName!) : "",
                    IsActive = t.IsActive,
                    Source = "Legacy"
                })
                .ToListAsync();

            var rows = assignmentRows
                .Concat(legacyRows)
                .OrderBy(r => r.FlatNumber)
                .ThenBy(r => r.TenantName)
                .ToList();

            ViewData["BuildingName"] = building.Name;
            ViewData["BuildingId"] = building.Id;

            return View(rows);
        }
    }
}