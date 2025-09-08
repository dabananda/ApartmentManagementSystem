using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // -------------------- ASSIGN PRESIDENT --------------------

        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> AssignPresident(Guid? buildingId)
        {
            // Build buildings dropdown
            var buildings = await _context.Buildings
                .OrderBy(b => b.Name)
                .Select(b => new SelectListItem
                {
                    Value = b.Id.ToString(),
                    Text = $"{b.Name} ({b.Code})"
                })
                .ToListAsync();

            // Owners filtered by selected building
            var owners = new List<SelectListItem>();
            if (buildingId.HasValue)
            {
                // Get ONLY users in Owner role AND in the selected building
                var ownerUsers = await _userManager.GetUsersInRoleAsync("Owner");
                owners = ownerUsers
                    .Where(u => u.BuildingId == buildingId.Value)
                    .OrderBy(u => u.Fullname)
                    .Select(u => new SelectListItem
                    {
                        Value = u.Id,
                        Text = string.IsNullOrWhiteSpace(u.Fullname) ? u.Email! : $"{u.Fullname} ({u.Email})"
                    })
                    .ToList();
            }

            var vm = new AssignPresidentViewModel
            {
                BuildingId = buildingId,
                Buildings = buildings,
                Owners = owners
            };

            return View(vm); // Views/Admin/AssignPresident.cshtml
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPresident(AssignPresidentViewModel model)
        {
            // Rebuild lists if we need to return the view with errors
            async Task PopulateListsAsync()
            {
                model.Buildings = await _context.Buildings
                    .OrderBy(b => b.Name)
                    .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                    .ToListAsync();

                model.Owners = new List<SelectListItem>();
                if (model.BuildingId.HasValue)
                {
                    var ownerUsers = await _userManager.GetUsersInRoleAsync("Owner");
                    model.Owners = ownerUsers
                        .Where(u => u.BuildingId == model.BuildingId.Value)
                        .OrderBy(u => u.Fullname)
                        .Select(u => new SelectListItem
                        {
                            Value = u.Id,
                            Text = string.IsNullOrWhiteSpace(u.Fullname) ? u.Email! : $"{u.Fullname} ({u.Email})"
                        })
                        .ToList();
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateListsAsync();
                return View(model);
            }

            if (!model.BuildingId.HasValue)
            {
                ModelState.AddModelError(nameof(model.BuildingId), "Please select a building.");
                await PopulateListsAsync();
                return View(model);
            }

            var building = await _context.Buildings.FindAsync(model.BuildingId.Value);
            if (building == null)
            {
                ModelState.AddModelError(nameof(model.BuildingId), "Invalid building.");
                await PopulateListsAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.OwnerUserId!);
            if (user == null)
            {
                ModelState.AddModelError(nameof(model.OwnerUserId), "Invalid owner.");
                await PopulateListsAsync();
                return View(model);
            }

            // **Critical**: ensure the selected user belongs to the selected building
            if (user.BuildingId != model.BuildingId.Value)
            {
                ModelState.AddModelError(nameof(model.OwnerUserId), "Selected owner does not belong to the chosen building.");
                await PopulateListsAsync();
                return View(model);
            }

            // Must be in Owner role already (as per UI intent)
            if (!await _userManager.IsInRoleAsync(user, "Owner"))
            {
                ModelState.AddModelError(nameof(model.OwnerUserId), "Selected user is not an Owner.");
                await PopulateListsAsync();
                return View(model);
            }

            // Assign President role (keep Owner), remove Tenant/User if present
            foreach (var r in new[] { "User", "Tenant" })
                if (await _userManager.IsInRoleAsync(user, r))
                    await _userManager.RemoveFromRoleAsync(user, r);

            if (!await _userManager.IsInRoleAsync(user, "President"))
                await _userManager.AddToRoleAsync(user, "President");
            // Ensure Owner stays (should already be)
            if (!await _userManager.IsInRoleAsync(user, "Owner"))
                await _userManager.AddToRoleAsync(user, "Owner");

            // Mark approved
            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = (await _userManager.GetUserAsync(User))?.Id;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Assigned {user.Fullname} as President for building {building.Name}.";

            // Back to GET, keeping building filter to show the refined owner list again
            return RedirectToAction(nameof(AssignPresident), new { buildingId = model.BuildingId });
        }

        // Return owners for a given building (JSON) — no page reload needed
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet]
        public async Task<IActionResult> OwnersForBuilding(Guid buildingId)
        {
            var ownerUsers = await _userManager.GetUsersInRoleAsync("Owner");
            var owners = ownerUsers
                .Where(u => u.BuildingId == buildingId)
                .OrderBy(u => u.Fullname)
                .Select(u => new
                {
                    value = u.Id,
                    text = string.IsNullOrWhiteSpace(u.Fullname) ? u.Email! : $"{u.Fullname} ({u.Email})"
                })
                .ToList();

            return Json(owners);
        }

        // GET: Admin/Users
        public async Task<IActionResult> Users()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users
                .Where(x => x.UserName != currentUser.UserName)
                .Include(u => u.Building)
                .Include(u => u.OwnedFlats)
                .ToListAsync();

            var userViewModels = new List<UserDetailsViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var flatCount = user.OwnedFlats?.Count ?? 0;

                // Get tenant count for owned flats
                var tenantCount = 0;
                if (user.OwnedFlats != null && user.OwnedFlats.Any())
                {
                    var flatIds = user.OwnedFlats.Select(f => f.Id).ToList();
                    tenantCount = await _context.Tenants
                        .CountAsync(t => flatIds.Contains(t.FlatId) && t.IsActive);
                }

                // Get outstanding bills count
                var outstandingBills = await _context.ExpenseAllocations
                    .CountAsync(ea => ea.OwnerId == user.Id && !ea.IsPaid);

                // Get total outstanding amount
                var outstandingAmount = await _context.ExpenseAllocations
                    .Where(ea => ea.OwnerId == user.Id && !ea.IsPaid)
                    .SumAsync(ea => ea.AmountDue);

                userViewModels.Add(new UserDetailsViewModel
                {
                    Id = user.Id,
                    Fullname = user.Fullname,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    LockoutEnd = user.LockoutEnd,
                    AccessFailedCount = user.AccessFailedCount,
                    Roles = roles.ToList(),
                    BuildingName = user.Building?.Name,
                    BuildingAddress = user.Building?.Address,
                    FlatCount = flatCount,
                    TenantCount = tenantCount,
                    OutstandingBillsCount = outstandingBills,
                    OutstandingAmount = outstandingAmount,
                    LastLoginDate = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now ? null : DateTime.Now.AddDays(-30), // Placeholder for last login
                    AccountStatus = user.LockoutEnd != null && user.LockoutEnd > DateTime.Now ? "Locked" :
                                   user.EmailConfirmed ? "Active" : "Pending Verification"
                });
            }

            return View(userViewModels);
        }

        // GET: Admin/CreateUser
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpGet]
        public async Task<IActionResult> CreateUser()
        {
            var me = await _userManager.GetUserAsync(User);

            // Buildings list
            var buildingItems = new List<SelectListItem>();
            if (User.IsInRole("SuperAdmin"))
            {
                buildingItems = await _context.Buildings
                    .OrderBy(b => b.Name)
                    .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                    .ToListAsync();
            }
            else if (me?.BuildingId != null)
            {
                var b = await _context.Buildings.FindAsync(me.BuildingId);
                if (b != null)
                    buildingItems.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
            }

            ViewBag.Buildings = buildingItems;

            // Roles list (Owner is granted automatically with President)
            ViewBag.Roles = new List<SelectListItem>
    {
        new SelectListItem("User (pending)", "User"),
        new SelectListItem("Tenant", "Tenant"),
        new SelectListItem("President", "President")
    };

            // Prefill building for President
            var vm = new CreateUserViewModel();
            if (User.IsInRole("President") && me?.BuildingId != null)
                vm.BuildingId = me.BuildingId.Value;

            return View(vm); // Views/Admin/CreateUser.cshtml
        }

        // POST: Admin/CreateUser
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            var me = await _userManager.GetUserAsync(User);

            // Rebuild selects if returning view with errors
            async Task LoadListsAsync()
            {
                var buildingItems = new List<SelectListItem>();
                if (User.IsInRole("SuperAdmin"))
                {
                    buildingItems = await _context.Buildings
                        .OrderBy(b => b.Name)
                        .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                        .ToListAsync();
                }
                else if (me?.BuildingId != null)
                {
                    var b = await _context.Buildings.FindAsync(me.BuildingId);
                    if (b != null)
                        buildingItems.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
                }
                ViewBag.Buildings = buildingItems;

                ViewBag.Roles = new List<SelectListItem>
        {
            new SelectListItem("User (pending)", "User"),
            new SelectListItem("Tenant", "Tenant"),
            new SelectListItem("President", "President")
        };
            }

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return View(model);
            }

            // Validate building
            var building = await _context.Buildings.FindAsync(model.BuildingId);
            if (building == null)
            {
                ModelState.AddModelError(nameof(model.BuildingId), "Invalid building.");
                await LoadListsAsync();
                return View(model);
            }

            // Presidents can only create within their building
            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != model.BuildingId))
            {
                ModelState.AddModelError(nameof(model.BuildingId), "You can only create users in your building.");
                await LoadListsAsync();
                return View(model);
            }

            // Email uniqueness
            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
                await LoadListsAsync();
                return View(model);
            }

            var user = new ApplicationUser
            {
                Fullname = model.Fullname,
                Email = model.Email,
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                BuildingId = model.BuildingId,
                EmailConfirmed = true,          // admin-created: mark confirmed
                IsApproved = model.Role != "User", // pending only when role is "User"
                ApprovedAt = model.Role == "User" ? null : DateTime.UtcNow,
                ApprovedByUserId = model.Role == "User" ? null : me?.Id,
                CreatedAt = DateTime.UtcNow
            };

            var createRes = await _userManager.CreateAsync(user, model.Password);
            if (!createRes.Succeeded)
            {
                foreach (var e in createRes.Errors) ModelState.AddModelError(string.Empty, e.Description);
                await LoadListsAsync();
                return View(model);
            }

            // Apply role rules
            if (model.Role == "President")
            {
                // President must also have Owner
                await EnsureOnlyRolesAsync(user, "President", "Owner");
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = me?.Id;
                await _userManager.UpdateAsync(user);
            }
            else if (model.Role == "Tenant")
            {
                await EnsureOnlyRolesAsync(user, "Tenant");
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = me?.Id;
                await _userManager.UpdateAsync(user);
            }
            else // "User" (pending)
            {
                await EnsureOnlyRolesAsync(user, "User");
                user.IsApproved = false;
                user.ApprovedAt = null;
                user.ApprovedByUserId = null;
                await _userManager.UpdateAsync(user);
            }

            TempData["Success"] = $"Created user {user.Fullname} ({user.Email}).";
            return RedirectToAction(nameof(Users), new { BuildingId = model.BuildingId });

            // ---- local helper enforces single-role policy (President gets +Owner) ----
            async Task EnsureOnlyRolesAsync(ApplicationUser u, params string[] rolesToKeep)
            {
                // Remove all managed roles
                foreach (var r in new[] { "User", "Tenant", "Owner", "President" })
                    if (await _userManager.IsInRoleAsync(u, r))
                        await _userManager.RemoveFromRoleAsync(u, r);

                // Add requested roles
                foreach (var r in rolesToKeep.Distinct())
                    await _userManager.AddToRoleAsync(u, r);
            }
        }

        // GET: Admin/ApproveOwners
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> ApproveOwners()
        {
            // Find all users who are not assigned a role yet
            var users = await _userManager.GetUsersInRoleAsync("User");
            // Find users who are not in any of the specific roles
            //var users = _context.Users.Where(u => !u.Roles.Any()).ToListAsync();
            return View(users);
        }

        // POST: Admin/ApproveOwner/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> ApproveOwner(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Add the "Owner" role to the user
            var result = await _userManager.AddToRoleAsync(user, "Owner");

            // Remove the default "User" role if needed
            // await _userManager.RemoveFromRoleAsync(user, "User");

            if (!result.Succeeded)
            {
                // Handle errors if role assignment fails
                TempData["Error"] = "Failed to approve owner.";
            }

            return RedirectToAction(nameof(ApproveOwners));
        }

        // GET: Admin/Approvals
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpGet]
        public async Task<IActionResult> Approvals([FromQuery] ApprovalsFilterViewModel filter)
        {
            var me = await _userManager.GetUserAsync(User);

            var pendingIds = (await _userManager.GetUsersInRoleAsync("User"))
                .Select(u => u.Id)
                .ToHashSet();

            IQueryable<ApplicationUser> q = _userManager.Users
                .Include(u => u.Building)
                .Where(u => pendingIds.Contains(u.Id) || !u.IsApproved);

            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null) return Forbid();
                filter.BuildingId ??= me.BuildingId;
            }
            if (filter.BuildingId != null)
                q = q.Where(u => u.BuildingId == filter.BuildingId);

            if (filter.OnlyEmailConfirmed)
                q = q.Where(u => u.EmailConfirmed);

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var term = filter.Query.Trim().ToLower();
                q = q.Where(u =>
                    (u.Fullname ?? "").ToLower().Contains(term) ||
                    (u.Email ?? "").ToLower().Contains(term) ||
                    (u.PhoneNumber ?? "").ToLower().Contains(term));
            }

            var total = await q.CountAsync();
            var pageSize = Math.Clamp(filter.PageSize, 5, 100);
            var page = Math.Max(1, filter.Page);

            var users = await q.OrderBy(u => u.Fullname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // roles for the current page
            var roleMap = new Dictionary<string, IList<string>>();
            foreach (var u in users) roleMap[u.Id] = await _userManager.GetRolesAsync(u);

            // building dropdown
            var buildings = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            if (User.IsInRole("SuperAdmin"))
            {
                buildings = await _context.Buildings.OrderBy(b => b.Name)
                    .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                    .ToListAsync();
            }
            else if (me?.BuildingId != null)
            {
                var b = await _context.Buildings.FindAsync(me.BuildingId);
                if (b != null)
                    buildings.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
            }

            var vm = new ApprovalsPageViewModel
            {
                Filter = filter,
                Buildings = buildings,
                Total = total,
                PendingUsers = users.Select(u => new PendingUserItemViewModel
                {
                    Id = u.Id,
                    Fullname = u.Fullname,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    EmailConfirmed = u.EmailConfirmed,
                    IsApproved = u.IsApproved,
                    BuildingId = u.BuildingId,
                    BuildingName = u.Building?.Name,
                    CreatedAt = u.CreatedAt,
                    CurrentStatus = u.IsApproved ? "Approved" : "Pending",
                    IsPresident = roleMap[u.Id].Contains("President")
                }).ToList()
            };

            return View(vm);
        }

        // POST: Admin/ApproveUser
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id, string role, [FromServices] IEmailSender mail)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Approvals));
            }

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                return Forbid();

            var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");

            // If target is President:
            // - Allow Owner (ensure Owner present)
            // - Disallow Tenant (presidents cannot be demoted here)
            if (targetIsPresident && role == "Tenant")
            {
                TempData["Error"] = "A President cannot be assigned the Tenant role.";
                return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
            }

            // Clean end-state roles for non-presidents
            foreach (var r in new[] { "User", "Owner", "Tenant" })
                if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

            if (targetIsPresident)
            {
                // Presidents should end with President + Owner (if Owner selected) or keep President only if Tenant was requested (blocked above)
                if (!await _userManager.IsInRoleAsync(user, "President"))
                    await _userManager.AddToRoleAsync(user, "President");
                if (role == "Owner" && !await _userManager.IsInRoleAsync(user, "Owner"))
                    await _userManager.AddToRoleAsync(user, "Owner");
            }
            else
            {
                // Non-presidents: exactly one of Owner/Tenant
                await _userManager.AddToRoleAsync(user, role);
            }

            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = me?.Id;
            await _userManager.UpdateAsync(user);

            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    await mail.SendEmailAsync(user.Email, "Your account has been approved",
                        $@"<p>Hi {user.Fullname},</p><p>Your role is now <strong>{(targetIsPresident ? "President + " + role : role)}</strong>. You can log in now.</p>");
                }
                catch { }
            }

            TempData["Success"] = $"Approved {user.Fullname} as {(targetIsPresident ? "President + " + role : role)}.";
            return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
        }

        // POST: Admin/BulkApprove
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(string[] ids, string role, [FromServices] IEmailSender mail)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Approvals));
            }
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Approvals));
            }

            var me = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int applied = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                    continue;

                var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");
                if (targetIsPresident && role == "Tenant")
                    continue; // cannot make a president a tenant

                foreach (var r in new[] { "User", "Owner", "Tenant" })
                    if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

                if (targetIsPresident)
                {
                    if (!await _userManager.IsInRoleAsync(user, "President"))
                        await _userManager.AddToRoleAsync(user, "President");
                    if (role == "Owner" && !await _userManager.IsInRoleAsync(user, "Owner"))
                        await _userManager.AddToRoleAsync(user, "Owner");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = me?.Id;
                await _userManager.UpdateAsync(user);
                applied++;
            }

            TempData["Success"] = $"Applied {applied} update(s).";
            return RedirectToAction(nameof(Approvals));
        }

        // POST: Admin/ResetUser
        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                return Forbid();

            if (await _userManager.IsInRoleAsync(user, "President"))
            {
                TempData["Error"] = "Cannot reset a President from here.";
                return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
            }

            foreach (var r in new[] { "Owner", "Tenant" })
                if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

            if (!await _userManager.IsInRoleAsync(user, "User"))
                await _userManager.AddToRoleAsync(user, "User");

            user.IsApproved = false;
            user.ApprovedAt = null;
            user.ApprovedByUserId = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Reset {user.Fullname} to pending.";
            return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
        }

        // -------------------- APPROVED USERS MANAGEMENT --------------------

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpGet]
        public async Task<IActionResult> Users([FromQuery] ManageUsersFilterViewModel filter)
        {
            var me = await _userManager.GetUserAsync(User);

            // Approved users = IsApproved && NOT in 'User' (pending) role
            var pendingIds = (await _userManager.GetUsersInRoleAsync("User")).Select(u => u.Id).ToHashSet();

            var query = _userManager.Users
                .Include(u => u.Building)
                .Where(u => u.IsApproved && !pendingIds.Contains(u.Id));

            // Presidents only see/manage their building
            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null) return Forbid();
                filter.BuildingId ??= me.BuildingId;
            }
            if (filter.BuildingId != null)
                query = query.Where(u => u.BuildingId == filter.BuildingId);

            // Search
            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var q = filter.Query.Trim().ToLower();
                query = query.Where(u =>
                    (u.Fullname ?? "").ToLower().Contains(q) ||
                    (u.Email ?? "").ToLower().Contains(q) ||
                    (u.PhoneNumber ?? "").ToLower().Contains(q));
            }

            // Execute base page
            var totalBeforeRoleFilter = await query.CountAsync();
            var pageSize = Math.Clamp(filter.PageSize, 5, 100);
            var page = Math.Max(1, filter.Page);

            var pageUsers = await query
                .OrderBy(u => u.Fullname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Roles for the page (portable across stores)
            var roleMap = new Dictionary<string, IList<string>>();
            foreach (var u in pageUsers)
                roleMap[u.Id] = await _userManager.GetRolesAsync(u);

            // Role filter (apply after role fetch for simplicity of LINQ to Objects)
            IEnumerable<ApplicationUser> filteredUsers = pageUsers;
            if (filter.Role == "President")
                filteredUsers = pageUsers.Where(u => roleMap[u.Id].Contains("President"));
            else if (filter.Role == "Owner")
                filteredUsers = pageUsers.Where(u => roleMap[u.Id].Contains("Owner"));
            else if (filter.Role == "Tenant")
                filteredUsers = pageUsers.Where(u => roleMap[u.Id].Contains("Tenant"));

            // Locked filter
            if (filter.LockedOnly)
                filteredUsers = filteredUsers.Where(u => (u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow));

            var finalUsers = filteredUsers.ToList();
            var vm = new ManageUsersPageViewModel
            {
                Filter = filter,
                Total = totalBeforeRoleFilter, // total before role filter; pagination stays stable
                Buildings = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>()
            };

            // Buildings dropdown
            if (User.IsInRole("SuperAdmin"))
            {
                vm.Buildings = await _context.Buildings
                    .OrderBy(b => b.Name)
                    .Select(b => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Name} ({b.Code})"
                    })
                    .ToListAsync();
            }
            else if (me?.BuildingId != null)
            {
                var b = await _context.Buildings.FindAsync(me.BuildingId);
                if (b != null)
                {
                    vm.Buildings.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = b.Id.ToString(),
                        Text = $"{b.Name} ({b.Code})"
                    });
                }
            }

            // Map rows
            vm.Users = finalUsers.Select(u =>
            {
                var roles = roleMap[u.Id].ToList();
                return new UserRowViewModel
                {
                    Id = u.Id,
                    Fullname = u.Fullname,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    BuildingId = u.BuildingId,
                    BuildingName = u.Building?.Name,
                    EmailConfirmed = u.EmailConfirmed,
                    IsApproved = u.IsApproved,
                    IsPresident = roles.Contains("President"),
                    Roles = roles,
                    CreatedAt = u.CreatedAt,
                    IsLockedOut = u.LockoutEnabled && u.LockoutEnd.HasValue && u.LockoutEnd.Value > DateTimeOffset.UtcNow
                };
            }).ToList();

            return View(vm); // Views/Admin/Users.cshtml
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                return Forbid();

            var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");
            if (targetIsPresident && role == "Tenant")
            {
                TempData["Error"] = "A President cannot be assigned the Tenant role.";
                return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
            }

            // Enforce single end-state role for non-presidents
            foreach (var r in new[] { "Owner", "Tenant", "User" })
                if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

            if (targetIsPresident)
            {
                if (!await _userManager.IsInRoleAsync(user, "President"))
                    await _userManager.AddToRoleAsync(user, "President");
                if (role == "Owner" && !await _userManager.IsInRoleAsync(user, "Owner"))
                    await _userManager.AddToRoleAsync(user, "Owner");
            }
            else
            {
                await _userManager.AddToRoleAsync(user, role);
            }

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Changed role for {user.Fullname} to {(targetIsPresident ? "President + " + role : role)}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRole(string[] ids, string role)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int changed = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                    continue;

                var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");
                if (targetIsPresident && role == "Tenant")
                    continue;

                foreach (var r in new[] { "Owner", "Tenant", "User" })
                    if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

                if (targetIsPresident)
                {
                    if (!await _userManager.IsInRoleAsync(user, "President"))
                        await _userManager.AddToRoleAsync(user, "President");
                    if (role == "Owner" && !await _userManager.IsInRoleAsync(user, "Owner"))
                        await _userManager.AddToRoleAsync(user, "Owner");
                }
                else
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                changed++;
            }

            TempData["Success"] = $"Changed roles for {changed} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                return Forbid();

            // Don't allow blocking SuperAdmin or Presidents via this path
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin") || roles.Contains("President"))
            {
                TempData["Error"] = "Cannot block SuperAdmin or President here.";
                return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            TempData["Success"] = $"Blocked {user.Fullname}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                return Forbid();

            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.SetLockoutEnabledAsync(user, true);

            TempData["Success"] = $"Unblocked {user.Fullname}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkBlock(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int blocked = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                    continue;

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin") || roles.Contains("President"))
                    continue;

                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                blocked++;
            }

            TempData["Success"] = $"Blocked {blocked} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "SuperAdmin,President")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUnblock(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int unblocked = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != user.BuildingId))
                    continue;

                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.SetLockoutEnabledAsync(user, true);
                unblocked++;
            }

            TempData["Success"] = $"Unblocked {unblocked} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin") || roles.Contains("President"))
            {
                TempData["Error"] = "Cannot delete SuperAdmin or President.";
                return RedirectToAction(nameof(Users));
            }

            var result = await _userManager.DeleteAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] =
                result.Succeeded ? $"Deleted {user.Fullname}." : string.Join("; ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = "SuperAdmin")]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var users = await _userManager.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

            int deleted = 0;
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin") || roles.Contains("President"))
                    continue;

                var res = await _userManager.DeleteAsync(user);
                if (res.Succeeded) deleted++;
            }

            TempData["Success"] = $"Deleted {deleted} user(s).";
            return RedirectToAction(nameof(Users));
        }
    }
}
