using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
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

        // GET: Tenant/ViewTenants/{flatId}
        public async Task<IActionResult> ViewTenants(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();

            // Load the flat (need BuildingId to authorize a President)
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

            // 1) Active assignment-based tenants (source of truth)
            var assignmentRows = await _context.TenantAssignments.AsNoTracking()
                .Include(a => a.TenantUser)
                .Where(a => a.FlatId == flat.Id && a.EndDate == null)
                .Select(a => new ApartmentManagementSystem.ViewModels.FlatTenantRow
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

            // 2) Legacy tenants that don’t duplicate current assignments
            var legacyRows = await _context.Tenants.AsNoTracking()
                .Where(t => t.FlatId == flat.Id && (t.UserId == null || !assignedUserIds.Contains(t.UserId)))
                .Select(t => new ApartmentManagementSystem.ViewModels.FlatTenantRow
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

        // GET: Tenant/Create/{flatId}
        public async Task<IActionResult> Create(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FirstOrDefaultAsync(x => x.Id == flatId);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["FlatId"] = flatId;
            return View();
        }

        // POST: Tenant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Fullname,Email,PhoneNumber,IsActive,FlatId")] Tenant tenant)
        {
            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FirstOrDefaultAsync(f => f.Id == tenant.FlatId);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                // 1) Create Identity user only if tenant has a valid email
                ApplicationUser? tenantUser = null;
                if (!string.IsNullOrWhiteSpace(tenant.Email))
                {
                    // Check if user already exists
                    tenantUser = await _userManager.FindByEmailAsync(tenant.Email);
                    if (tenantUser == null)
                    {
                        // Create new user
                        tenantUser = new ApplicationUser
                        {
                            Fullname = tenant.Fullname,
                            UserName = tenant.Email,
                            Email = tenant.Email,
                            EmailConfirmed = true
                        };

                        var defaultPassword = _config["Password"] ?? "TempPass@123";
                        var createResult = await _userManager.CreateAsync(tenantUser, defaultPassword);

                        if (createResult.Succeeded)
                        {
                            // Add Tenant role
                            await _userManager.AddToRoleAsync(tenantUser, "Tenant");
                        }
                        else
                        {
                            // Log the error but don't fail the tenant creation
                            var logger = HttpContext.RequestServices.GetRequiredService<ILogger<TenantController>>();
                            logger.LogError("Failed to create Identity user for tenant {Email}: {Errors}",
                                tenant.Email, string.Join("; ", createResult.Errors.Select(e => e.Description)));

                            // Set tenantUser to null so we proceed without Identity user
                            tenantUser = null;

                            // Add a model warning but don't fail
                            ModelState.AddModelError("Email",
                                "Tenant created successfully, but email account setup failed. The tenant can still receive emails at their direct email address.");
                        }
                    }
                    else
                    {
                        // User exists, ensure they have Tenant role
                        if (!await _userManager.IsInRoleAsync(tenantUser, "Tenant"))
                        {
                            await _userManager.AddToRoleAsync(tenantUser, "Tenant");
                        }
                    }
                }

                // 2) Create the domain Tenant record (regardless of Identity user creation success)
                tenant.Id = Guid.NewGuid();
                tenant.UserId = tenantUser?.Id; // This might be null, and that's okay

                _context.Add(tenant);

                // 3) Mark flat as occupied
                if (!flat.IsOccupied)
                {
                    flat.IsOccupied = true;
                    _context.Update(flat);
                }

                await _context.SaveChangesAsync();

                // 4) Success message
                if (tenantUser != null)
                {
                    TempData["Success"] = $"Tenant created successfully with email account. Login details will be provided separately.";
                }
                else if (!string.IsNullOrWhiteSpace(tenant.Email))
                {
                    TempData["Success"] = $"Tenant created successfully. Email notifications will be sent to {tenant.Email} directly.";
                }
                else
                {
                    TempData["Success"] = "Tenant created successfully. No email provided - notifications will not be sent.";
                }

                return RedirectToAction(nameof(ViewTenants), new { flatId = tenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // GET: Tenant/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // POST: Tenant/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Fullname,Email,PhoneNumber,IsActive,FlatId")] Tenant tenant)
        {
            if (id != tenant.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var existingTenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);
            if (existingTenant == null || (existingTenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the existing tenant with the new values from the form
                    existingTenant.Fullname = tenant.Fullname;
                    existingTenant.Email = tenant.Email;
                    existingTenant.PhoneNumber = tenant.PhoneNumber;
                    existingTenant.IsActive = tenant.IsActive;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tenants.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewTenants), new { flatId = existingTenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // GET: Tenant/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }
            return View(tenant);
        }


        // GET: Tenant/Delete/{id}
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            return View(tenant);
        }

        // POST: Tenant/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            // Check if any other tenants exist for this flat. If not, mark it as vacant.
            var remainingTenants = await _context.Tenants.AnyAsync(t => t.FlatId == tenant.FlatId);
            if (!remainingTenants)
            {
                var flat = await _context.Flats.FirstOrDefaultAsync(f => f.Id == tenant.FlatId);
                if (flat != null)
                {
                    flat.IsOccupied = false;
                    _context.Update(flat);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(ViewTenants), new { flatId = tenant.FlatId });
        }

        // GET: Tenant/BuildingTenants
        public async Task<IActionResult> BuildingTenants()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();
            if (User.IsInRole("President") && user.BuildingId == null) return Forbid();

            var buildingId = user.BuildingId!.Value;

            // Building info
            var building = await _context.Buildings.AsNoTracking().FirstOrDefaultAsync(b => b.Id == buildingId);
            if (building == null) return NotFound();

            // --- Assignments (source of truth) ---
            var assignmentRows = await _context.TenantAssignments
                .Include(a => a.Flat)!.ThenInclude(f => f.Owner)
                .Include(a => a.TenantUser)
                .Where(a => a.EndDate == null && a.Flat!.BuildingId == buildingId)
                .Select(a => new ApartmentManagementSystem.ViewModels.BuildingTenantRow
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

            // Track which flats already covered by assignments (avoid duplicates with legacy)
            var assignedFlatIds = assignmentRows.Select(r => r.FlatId).ToHashSet();

            // --- Legacy active tenants for flats that have no active assignment yet ---
            var legacyRows = await _context.Tenants
                .Include(t => t.Flat)!.ThenInclude(f => f.Owner)
                .Where(t => t.IsActive && t.Flat!.BuildingId == buildingId && !assignedFlatIds.Contains(t.FlatId))
                .Select(t => new ApartmentManagementSystem.ViewModels.BuildingTenantRow
                {
                    FlatId = t.FlatId,
                    FlatNumber = t.Flat!.FlatNumber,
                    TenantUserId = t.UserId ?? "", // may be blank if legacy didn't link to Identity
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