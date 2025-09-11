using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using ApartmentManagementSystem.ViewModels;
using ApartmentManagementSystem.ViewModels.Building;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class BuildingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBuildingCodeGenerator _codeGen;

        public BuildingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBuildingCodeGenerator codeGen)
        {
            _context = context;
            _userManager = userManager;
            _codeGen = codeGen;
        }

        // GET: Building Index with filtering, pagination, and aggregations
        public async Task<IActionResult> Index([FromQuery] BuildingIndexFilterViewModel filter)
        {
            // Base query + search
            IQueryable<Building> bq = _context.Buildings.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim().ToLower();
                bq = bq.Where(b =>
                    b.Name.ToLower().Contains(q) ||
                    b.Code.ToLower().Contains(q) ||
                    (b.Address != null && b.Address.ToLower().Contains(q)));
            }

            // ---------- HasPresident filter (distinct variable names, fix Guid? -> Guid) ----------
            var presidentRoleId_Filter = await _context.Roles
                .Where(r => r.Name == "President")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            var buildingsWithPresident_Filter = new List<Guid>();
            if (!string.IsNullOrEmpty(presidentRoleId_Filter))
            {
                buildingsWithPresident_Filter = await (
                    from u in _context.Users
                    join ur in _context.UserRoles on u.Id equals ur.UserId
                    where ur.RoleId == presidentRoleId_Filter && u.BuildingId != null
                    select u.BuildingId!.Value        // Guid? -> Guid
                )
                .Distinct()
                .ToListAsync();
            }

            if (filter.HasPresident == true)
                bq = bq.Where(b => buildingsWithPresident_Filter.Contains(b.Id));
            else if (filter.HasPresident == false)
                bq = bq.Where(b => !buildingsWithPresident_Filter.Contains(b.Id));

            // ---------- Pagination ----------
            var total = await bq.CountAsync();
            var pageSize = Math.Clamp(filter.PageSize, 5, 100);
            var page = Math.Max(1, filter.Page);

            var buildingsPage = await bq
                .OrderBy(b => b.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pageIds = buildingsPage.Select(b => b.Id).ToList();

            // ---------- Per-page aggregations ----------
            // Flats per building (if you don't have a Flats table, remove this whole block & set FlatsCount=0 below)
            var flatsDict = new Dictionary<Guid, int>();
            try
            {
                flatsDict = await _context.Flats
                    .Where(f => pageIds.Contains(f.BuildingId))
                    .GroupBy(f => f.BuildingId)
                    .Select(g => new { BuildingId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.BuildingId, x => x.Count);
            }
            catch
            {
                // No Flats table — leave dict empty
            }

            // Users for these buildings
            var usersPage = await _context.Users
                .Where(u => u.BuildingId != null && pageIds.Contains(u.BuildingId.Value))
                .Select(u => new { u.Id, u.BuildingId, u.Fullname, u.Email })
                .ToListAsync();

            var userIdsPage = usersPage.Select(u => u.Id).ToList();
            var userBuilding_Page = usersPage.ToDictionary(u => u.Id, u => u.BuildingId!.Value);
            var userNameOrEmail_Page = usersPage.ToDictionary(
                u => u.Id,
                u => string.IsNullOrWhiteSpace(u.Fullname) ? (u.Email ?? "-") : u.Fullname
            );

            // Role ids for this page (distinct names so they don't collide with the filter ones above)
            var roleDefs_Page = await _context.Roles
                .Where(r => r.Name == "Tenant" || r.Name == "Owner" || r.Name == "President")
                .Select(r => new { r.Id, r.Name })
                .ToListAsync();

            var tenantRoleId_Page = roleDefs_Page.FirstOrDefault(r => r.Name == "Tenant")?.Id;
            var ownerRoleId_Page = roleDefs_Page.FirstOrDefault(r => r.Name == "Owner")?.Id;
            var presidentRoleId_Page = roleDefs_Page.FirstOrDefault(r => r.Name == "President")?.Id;

            var userRoles_Page = await _context.UserRoles
                .Where(ur => userIdsPage.Contains(ur.UserId)
                             && (ur.RoleId == tenantRoleId_Page || ur.RoleId == ownerRoleId_Page || ur.RoleId == presidentRoleId_Page))
                .ToListAsync();

            var tenantsByBuilding_Page = userRoles_Page
                .Where(ur => ur.RoleId == tenantRoleId_Page)
                .GroupBy(ur => userBuilding_Page[ur.UserId])
                .ToDictionary(g => g.Key, g => g.Count());

            var ownersByBuilding_Page = userRoles_Page
                .Where(ur => ur.RoleId == ownerRoleId_Page)
                .GroupBy(ur => userBuilding_Page[ur.UserId])
                .ToDictionary(g => g.Key, g => g.Count());

            var presidentByBuilding_Page = userRoles_Page
                .Where(ur => ur.RoleId == presidentRoleId_Page)
                .GroupBy(ur => userBuilding_Page[ur.UserId])
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var firstUserId = g.Select(x => x.UserId).First();
                        return userNameOrEmail_Page[firstUserId];
                    });

            // ---------- Map to VM ----------
            var vm = new BuildingIndexPageViewModel
            {
                Filter = filter,
                Total = total,
                Buildings = buildingsPage.Select(b => new BuildingListItemViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Address = b.Address,
                    FlatsCount = flatsDict.TryGetValue(b.Id, out var fc) ? fc : 0,
                    TenantsCount = tenantsByBuilding_Page.TryGetValue(b.Id, out var tc) ? tc : 0,
                    OwnersCount = ownersByBuilding_Page.TryGetValue(b.Id, out var oc) ? oc : 0,
                    PresidentName = presidentByBuilding_Page.TryGetValue(b.Id, out var p) ? p : "-"
                }).ToList()
            };

            return View(vm); // Views/Building/Index.cshtml
        }

        // GET: Building Details
        public async Task<IActionResult> Details(Guid id)
        {
            var building = await _context.Buildings
                .Include(b => b.Flats)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (building == null) return NotFound();

            // Check for President can only see their assigned building
            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != building.Id) return Forbid();
            }
            return View(building);
        }

        // GET: Buildings Create View
        public async Task<IActionResult> Create()
        {
            ViewData["SuggestedCode"] = await _codeGen.GenerateAsync();
            return View();
        }

        // POST: Building Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,Code")] Building building)
        {
            if (string.IsNullOrWhiteSpace(building.Code))
                building.Code = await _codeGen.GenerateAsync();

            if (await _context.Buildings.AnyAsync(b => b.Code == building.Code))
                ModelState.AddModelError(nameof(Building.Code), "Building code already exists.");

            if (!ModelState.IsValid)
                return View(building);

            await _context.AddAsync(building);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Buildings Edit View
        public async Task<IActionResult> Edit(Guid id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: Building Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Address")] Building building)
        {
            if (id != building.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(building);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BuildingExists(building.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(building);
        }

        // GET: Buildings Delete View
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!User.IsInRole("SuperAdmin")) return Forbid();
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: Building Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!User.IsInRole("SuperAdmin")) return Forbid();
            var building = await _context.Buildings.FindAsync(id);
            if (building != null)
            {
                _context.Buildings.Remove(building);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if building exists
        private bool BuildingExists(Guid id)
        {
            return _context.Buildings.Any(e => e.Id == id);
        }
    }
}