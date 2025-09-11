using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
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

        [Authorize(Roles = Roles.SuperAdmin)]
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
                var ownerUsers = await _userManager.GetUsersInRoleAsync(Roles.Owner);
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

        [Authorize(Roles = Roles.SuperAdmin)]
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
                    var ownerUsers = await _userManager.GetUsersInRoleAsync(Roles.Owner);
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
        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> OwnersForBuilding(Guid buildingId)
        {
            var ownerUsers = await _userManager.GetUsersInRoleAsync(Roles.Owner);
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
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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
        [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
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
                new("User", "User"),
                new("Staff", "Staff"),
                new("Tenant", "Tenant"),
                new("Owner", "Owner")
            };

            // Prefill building for President
            var vm = new CreateUserViewModel();
            if (User.IsInRole("President") && me?.BuildingId != null)
                vm.BuildingId = me.BuildingId.Value;

            return View(vm);
        }

        // POST: Admin/CreateUser
        [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
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
                    new("User", "User"),
                    new("Staff", "Staff"),
                    new("Tenant", "Tenant"),
                    new("Owner", "Owner")
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
                EmailConfirmed = true,
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
            //if (model.Role == "President")
            //{
            //    // President must also have Owner
            //    await EnsureOnlyRolesAsync(user, "President", "Owner");
            //    user.IsApproved = true;
            //    user.ApprovedAt = DateTime.UtcNow;
            //    user.ApprovedByUserId = me?.Id;
            //    await _userManager.UpdateAsync(user);
            //}
            if (model.Role == "Tenant")
            {
                await EnsureOnlyRolesAsync(user, "Tenant");
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = me?.Id;
                await _userManager.UpdateAsync(user);
            }
            else if (model.Role == "Owner")
            {
                await EnsureOnlyRolesAsync(user, "Owner");
                user.IsApproved = true;
                user.ApprovedAt = DateTime.UtcNow;
                user.ApprovedByUserId = me?.Id;
                await _userManager.UpdateAsync(user);
            }
            else if (model.Role == "Staff")
            {
                await EnsureOnlyRolesAsync(user, "Staff");
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
                foreach (var r in new[] { "User", "Staff", "Tenant", "Owner", "President" })
                    if (await _userManager.IsInRoleAsync(u, r))
                        await _userManager.RemoveFromRoleAsync(u, r);

                // Add requested roles
                foreach (var r in rolesToKeep.Distinct())
                    await _userManager.AddToRoleAsync(u, r);
            }
        }

        // GET: Admin/EditUser/{id}
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            // Presidents can only edit users from their building
            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null || user.BuildingId != me.BuildingId) return Forbid();
            }

            // Building select list
            var buildingItems = new List<SelectListItem>();
            if (User.IsInRole("SuperAdmin"))
            {
                buildingItems = await _context.Buildings
                    .OrderBy(b => b.Name)
                    .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                    .ToListAsync();
            }
            else if (user.BuildingId != null)
            {
                // President: show the current building only (read-only)
                var b = await _context.Buildings.FindAsync(user.BuildingId);
                if (b != null)
                    buildingItems.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
            }
            ViewBag.Buildings = buildingItems;

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Fullname = user.Fullname,
                Email = user.Email, // read-only in the view (changing email = changing username; handled elsewhere)
                PhoneNumber = user.PhoneNumber,
                BuildingId = user.BuildingId,
                BuildingName = user.Building?.Name,
                IsSuperAdminCaller = User.IsInRole("SuperAdmin")
            };

            return View(vm); // Views/Admin/EditUser.cshtml
        }

        // POST: Admin/EditUser
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Refill building list
                var buildingItems = new List<SelectListItem>();
                if (User.IsInRole("SuperAdmin"))
                {
                    buildingItems = await _context.Buildings
                        .OrderBy(b => b.Name)
                        .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                        .ToListAsync();
                }
                else if (model.BuildingId != null)
                {
                    var b = await _context.Buildings.FindAsync(model.BuildingId);
                    if (b != null)
                        buildingItems.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
                }
                ViewBag.Buildings = buildingItems;
                return View(model);
            }

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == model.Id);
            if (user == null) return NotFound();

            // Presidents can only edit within their building
            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null || user.BuildingId != me.BuildingId) return Forbid();
            }

            // Update basic fields
            user.Fullname = model.Fullname?.Trim() ?? user.Fullname;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

            // Building changes: only SuperAdmin can change
            if (User.IsInRole("SuperAdmin"))
            {
                if (model.BuildingId.HasValue)
                {
                    var targetBuilding = await _context.Buildings.FindAsync(model.BuildingId.Value);
                    if (targetBuilding == null)
                    {
                        ModelState.AddModelError(nameof(model.BuildingId), "Invalid building.");
                        // rebuild list
                        ViewBag.Buildings = await _context.Buildings
                            .OrderBy(b => b.Name)
                            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                            .ToListAsync();
                        return View(model);
                    }
                    user.BuildingId = model.BuildingId.Value;
                }
                else
                {
                    user.BuildingId = null;
                }
            }

            var res = await _userManager.UpdateAsync(user);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError(string.Empty, e.Description);
                // rebuild list
                var buildingItems = new List<SelectListItem>();
                if (User.IsInRole("SuperAdmin"))
                {
                    buildingItems = await _context.Buildings
                        .OrderBy(b => b.Name)
                        .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                        .ToListAsync();
                }
                else if (user.BuildingId != null)
                {
                    var b = await _context.Buildings.FindAsync(user.BuildingId);
                    if (b != null)
                        buildingItems.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
                }
                ViewBag.Buildings = buildingItems;
                return View(model);
            }

            TempData["Success"] = $"Updated user {user.Fullname}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        // GET: Admin/ApproveOwners
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id, string role, [FromServices] IEmailSender mail)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant" && role != "Staff")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Approvals));
            }

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            if (User.IsInRole("President") && !currentIsSuperAdmin)
            {
                if (me?.BuildingId == null || me.BuildingId != user.BuildingId) return Forbid();
            }

            var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");

            if (targetIsPresident)
            {
                if (currentIsSuperAdmin)
                {
                    // SuperAdmin may demote a President to Owner-only or Tenant-only
                    foreach (var r in new[] { "User", "Staff", "Owner", "Tenant", "President" })
                        if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);

                    await _userManager.AddToRoleAsync(user, role); // Owner OR Tenant
                }
                else
                {
                    // Non-superadmin cannot make a President Tenant; Owner keeps President+Owner
                    if (role == "Tenant")
                    {
                        TempData["Error"] = "A President cannot be assigned the Tenant role.";
                        return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
                    }

                    // Keep President + Owner
                    if (!await _userManager.IsInRoleAsync(user, "President"))
                        await _userManager.AddToRoleAsync(user, "President");
                    if (!await _userManager.IsInRoleAsync(user, "Owner"))
                        await _userManager.AddToRoleAsync(user, "Owner");
                }
            }
            else
            {
                // Non-president: exactly one of Owner/Tenant
                foreach (var r in new[] { "User", "Staff", "Owner", "Tenant" })
                    if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
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
                    var roleText = targetIsPresident && !currentIsSuperAdmin ? "President + " + role : role;
                    await mail.SendEmailAsync(user.Email, "Your account has been approved",
                        $@"<p>Hi {user.Fullname},</p><p>Your role is now <strong>{roleText}</strong>. You can log in now.</p>");
                }
                catch { /* ignore mail errors */ }
            }

            TempData["Success"] = $"Approved {user.Fullname} as {(targetIsPresident && !currentIsSuperAdmin ? "President + " + role : role)}.";
            return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
        }

        // POST: Admin/BulkApprove
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(string[] ids, string role, [FromServices] IEmailSender mail)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Approvals));
            }
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant" && role != "Staff")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Approvals));
            }

            var me = await _userManager.GetUserAsync(User);
            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");

            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int applied = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && !currentIsSuperAdmin)
                {
                    if (me?.BuildingId == null || me.BuildingId != user.BuildingId) continue;
                }

                var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");

                if (targetIsPresident)
                {
                    if (currentIsSuperAdmin)
                    {
                        foreach (var r in new[] { "User", "Staff", "Owner", "Tenant", "President" })
                            if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        if (role == "Tenant") continue; // presidents can't be made tenant by non-superadmin
                        if (!await _userManager.IsInRoleAsync(user, "President"))
                            await _userManager.AddToRoleAsync(user, "President");
                        if (!await _userManager.IsInRoleAsync(user, "Owner"))
                            await _userManager.AddToRoleAsync(user, "Owner");
                    }
                }
                else
                {
                    foreach (var r in new[] { "User", "Staff", "Owner", "Tenant" })
                        if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
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
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            if (User.IsInRole("President") && !currentIsSuperAdmin)
            {
                if (me?.BuildingId == null || me.BuildingId != user.BuildingId) return Forbid();
            }

            var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");
            if (targetIsPresident && !currentIsSuperAdmin)
            {
                TempData["Error"] = "Only SuperAdmin can reset a President.";
                return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
            }

            // Reset to pending: remove all managed roles incl. President, set 'User'
            foreach (var r in new[] { "Owner", "Tenant", "President", "User" })
                if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
            await _userManager.AddToRoleAsync(user, "User");

            user.IsApproved = false;
            user.ApprovedAt = null;
            user.ApprovedByUserId = null;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Reset {user.Fullname} to pending.";
            return RedirectToAction(nameof(Approvals), new { BuildingId = user.BuildingId });
        }

        // GET: Admin/Users
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpGet]
        public async Task<IActionResult> Users([FromQuery] ManageUsersFilterViewModel filter)
        {
            var me = await _userManager.GetUserAsync(User);

            // Base query: exclude self, only approved users
            IQueryable<ApplicationUser> q = _userManager.Users
                .Where(u => u.Id != me.Id && u.IsApproved)
                .Include(u => u.Building);

            // Presidents are scoped to their own building
            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null) return Forbid();
                filter.BuildingId ??= me.BuildingId;
            }

            if (filter.BuildingId.HasValue)
                q = q.Where(u => u.BuildingId == filter.BuildingId);

            if (filter.LockedOnly)
                q = q.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);

            if (!string.IsNullOrWhiteSpace(filter.Query))
            {
                var term = filter.Query.Trim().ToLower();
                q = q.Where(u =>
                    (u.Fullname ?? "").ToLower().Contains(term) ||
                    (u.Email ?? "").ToLower().Contains(term) ||
                    (u.PhoneNumber ?? "").ToLower().Contains(term));
            }

            // Role filter (All | President | Owner | Tenant | Staff)
            if (!string.Equals(filter.Role, "All", StringComparison.OrdinalIgnoreCase))
            {
                var roleUsers = await _userManager.GetUsersInRoleAsync(filter.Role);
                var ids = roleUsers.Select(u => u.Id).ToList();
                q = q.Where(u => ids.Contains(u.Id));
            }

            // Pagination
            var total = await q.CountAsync();
            var pageSize = Math.Clamp(filter.PageSize, 5, 100);
            var page = Math.Max(1, filter.Page);

            var users = await q.OrderBy(u => u.Fullname)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Roles for the page
            var roleMap = new Dictionary<string, IList<string>>();
            foreach (var u in users) roleMap[u.Id] = await _userManager.GetRolesAsync(u);

            // Building dropdown
            var buildings = new List<SelectListItem>();
            if (User.IsInRole("SuperAdmin"))
            {
                buildings = await _context.Buildings.OrderBy(b => b.Name)
                    .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                    .ToListAsync();
            }
            else if (me?.BuildingId != null)
            {
                var b = await _context.Buildings.FindAsync(me.BuildingId);
                if (b != null) buildings.Add(new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" });
            }

            var vm = new ManageUsersPageViewModel
            {
                Filter = filter,
                Buildings = buildings,
                Users = users.Select(u => new UserRowViewModel
                {
                    Id = u.Id,
                    Fullname = u.Fullname,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    BuildingId = u.BuildingId,
                    BuildingName = u.Building?.Name,
                    EmailConfirmed = u.EmailConfirmed,
                    IsApproved = u.IsApproved,
                    IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                    IsPresident = roleMap[u.Id].Contains("President"),
                    CreatedAt = u.CreatedAt,
                    Roles = roleMap[u.Id].ToList()
                }).ToList(),
                Total = total
            };

            return View(vm);
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();
            role = (role ?? "").Trim();
            if (role != "Owner" && role != "Tenant" && role != "Staff")
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            if (User.IsInRole("President") && !currentIsSuperAdmin)
            {
                if (me?.BuildingId == null || me.BuildingId != user.BuildingId) return Forbid();
            }

            var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");

            if (targetIsPresident)
            {
                if (currentIsSuperAdmin)
                {
                    // Demote to single role (Owner or Tenant)
                    foreach (var r in new[] { "User", "Staff", "Owner", "Tenant", "President" })
                        if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
                    await _userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    if (role == "Tenant")
                    {
                        TempData["Error"] = "A President cannot be assigned the Tenant role.";
                        return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
                    }
                    if (!await _userManager.IsInRoleAsync(user, "President"))
                        await _userManager.AddToRoleAsync(user, "President");
                    if (!await _userManager.IsInRoleAsync(user, "Owner"))
                        await _userManager.AddToRoleAsync(user, "Owner");
                }
            }
            else
            {
                foreach (var r in new[] { "Owner", "Tenant", "Staff", "User" })
                    if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
                await _userManager.AddToRoleAsync(user, role);
            }

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            TempData["Success"] = $"Changed role for {user.Fullname} to {(currentIsSuperAdmin && targetIsPresident ? role : (targetIsPresident ? "President + " + role : role))}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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
            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int changed = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && !currentIsSuperAdmin)
                {
                    if (me?.BuildingId == null || me.BuildingId != user.BuildingId) continue;
                }

                var targetIsPresident = await _userManager.IsInRoleAsync(user, "President");
                if (targetIsPresident)
                {
                    if (currentIsSuperAdmin)
                    {
                        foreach (var r in new[] { "User", "Owner", "Tenant", "President" })
                            if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
                        await _userManager.AddToRoleAsync(user, role);
                    }
                    else
                    {
                        if (role == "Tenant") continue;
                        if (!await _userManager.IsInRoleAsync(user, "President"))
                            await _userManager.AddToRoleAsync(user, "President");
                        if (!await _userManager.IsInRoleAsync(user, "Owner"))
                            await _userManager.AddToRoleAsync(user, "Owner");
                    }
                }
                else
                {
                    foreach (var r in new[] { "Owner", "Tenant", "User" })
                        if (await _userManager.IsInRoleAsync(user, r)) await _userManager.RemoveFromRoleAsync(user, r);
                    await _userManager.AddToRoleAsync(user, role);
                }

                user.IsApproved = true;
                await _userManager.UpdateAsync(user);
                changed++;
            }

            TempData["Success"] = $"Changed roles for {changed} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            if (User.IsInRole("President") && !currentIsSuperAdmin)
            {
                if (me?.BuildingId == null || me.BuildingId != user.BuildingId) return Forbid();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin"))
            {
                TempData["Error"] = "Cannot block a SuperAdmin.";
                return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
            }
            if (roles.Contains("President") && !currentIsSuperAdmin)
            {
                TempData["Error"] = "Only SuperAdmin can block a President.";
                return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
            }

            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            TempData["Success"] = $"Blocked {user.Fullname}.";
            return RedirectToAction(nameof(Users), new { BuildingId = user.BuildingId });
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkBlock(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var me = await _userManager.GetUserAsync(User);
            var currentIsSuperAdmin = User.IsInRole("SuperAdmin");
            var users = await _userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync();

            int blocked = 0;
            foreach (var user in users)
            {
                if (User.IsInRole("President") && !currentIsSuperAdmin)
                {
                    if (me?.BuildingId == null || me.BuildingId != user.BuildingId) continue;
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("SuperAdmin")) continue;
                if (roles.Contains("President") && !currentIsSuperAdmin) continue;

                await _userManager.SetLockoutEnabledAsync(user, true);
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                blocked++;
            }

            TempData["Success"] = $"Blocked {blocked} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
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

        [Authorize(Roles = Roles.SuperAdmin)]
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

        [Authorize(Roles = Roles.SuperAdmin)]
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
