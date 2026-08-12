using AMS.Application.Features.Administration.Commands;
using AMS.Application.Features.Administration.DTOs;
using AMS.Application.Features.Administration.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AMS.Web.Controllers;

public class AdminController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public AdminController(UserManager<ApplicationUser> userManager, IMediator mediator)
    {
        _userManager = userManager;
        _mediator = mediator;
    }

    private static List<SelectListItem> GetUserRoleSelectItems() =>
    [
        new(Roles.User,   Roles.User),
        new(Roles.Staff,  Roles.Staff),
        new(Roles.Tenant, Roles.Tenant),
        new(Roles.Owner,  Roles.Owner)
    ];

    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> AssignPresident(Guid? buildingId)
    {
        var buildings = await _mediator.Send(new GetBuildingSelectItemsQuery());
        var owners = buildingId.HasValue
            ? await _mediator.Send(new GetOwnersForBuildingSelectQuery(buildingId.Value))
            : [];

        var vm = new AssignPresidentViewModel
        {
            BuildingId = buildingId,
            Buildings = buildings,
            Owners = owners
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPresident(AssignPresidentViewModel model)
    {
        if (!ModelState.IsValid || !model.BuildingId.HasValue)
        {
            ModelState.AddModelError(nameof(model.BuildingId), "Please select a building.");
            model.Buildings = await _mediator.Send(new GetBuildingSelectItemsQuery());
            model.Owners = model.BuildingId.HasValue
                ? await _mediator.Send(new GetOwnersForBuildingSelectQuery(model.BuildingId.Value))
                : [];
            return View(model);
        }

        var me = await _userManager.GetUserAsync(User);
        var (success, message) = await _mediator.Send(new AssignPresidentCommand(
            model.BuildingId!.Value, model.OwnerUserId!, me!.Id));

        if (!success)
        {
            ModelState.AddModelError(nameof(model.OwnerUserId), message);
            model.Buildings = await _mediator.Send(new GetBuildingSelectItemsQuery());
            model.Owners = await _mediator.Send(new GetOwnersForBuildingSelectQuery(model.BuildingId.Value));
            return View(model);
        }

        TempData["Success"] = message;
        return RedirectToAction(nameof(AssignPresident), new { buildingId = model.BuildingId });
    }

    [Authorize(Roles = Roles.SuperAdmin)]
    public async Task<IActionResult> OwnersForBuilding(Guid buildingId)
    {
        var owners = (await _mediator.Send(new GetOwnersForBuildingSelectQuery(buildingId)))
            .Select(o => new { value = o.Value, text = o.Text });
        return Json(owners);
    }

    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public async Task<IActionResult> CreateUser()
    {
        var ctx = await this.GetCallerContextAsync(_userManager);
        var buildingItems = ctx!.IsSuperAdmin
            ? await _mediator.Send(new GetBuildingSelectItemsQuery())
            : ctx.BuildingId != null
                ? await _mediator.Send(new GetBuildingSelectItemsQuery(ctx.BuildingId))
                : [];

        ViewBag.Buildings = buildingItems;
        ViewBag.Roles = GetUserRoleSelectItems();

        var vm = new CreateUserViewModel();
        if (User.IsInRole(Roles.President) && ctx.BuildingId != null)
            vm.BuildingId = ctx.BuildingId.Value;

        return View(vm);
    }

    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        var ctx = await this.GetCallerContextAsync(_userManager);

        async Task LoadListsAsync()
        {
            ViewBag.Buildings = ctx!.IsSuperAdmin
                ? await _mediator.Send(new GetBuildingSelectItemsQuery())
                : ctx.BuildingId != null
                    ? await _mediator.Send(new GetBuildingSelectItemsQuery(ctx.BuildingId))
                    : [];
            ViewBag.Roles = GetUserRoleSelectItems();
        }

        if (!ModelState.IsValid)
        {
            await LoadListsAsync();
            return View(model);
        }

        if (User.IsInRole(Roles.President) && (ctx?.BuildingId == null || ctx.BuildingId != model.BuildingId))
        {
            ModelState.AddModelError(nameof(model.BuildingId), "You can only create users in your building.");
            await LoadListsAsync();
            return View(model);
        }

        if (await _userManager.FindByEmailAsync(model.Email) != null)
        {
            ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
            await LoadListsAsync();
            return View(model);
        }

        var (success, errors) = await _mediator.Send(new CreateUserCommand(model, ctx!.Me.Id));
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            await LoadListsAsync();
            return View(model);
        }

        TempData["Success"] = $"Created user {model.Fullname} ({model.Email}).";
        return RedirectToAction(nameof(Users), new { BuildingId = model.BuildingId });
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public async Task<IActionResult> EditUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);
        var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
        if (user == null) return NotFound();

        if (User.IsInRole(Roles.President) && (ctx?.BuildingId == null || user.BuildingId != ctx.BuildingId))
            return Forbid();

        ViewBag.Buildings = ctx!.IsSuperAdmin
            ? await _mediator.Send(new GetBuildingSelectItemsQuery())
            : user.BuildingId != null
                ? await _mediator.Send(new GetBuildingSelectItemsQuery(user.BuildingId))
                : [];

        var vm = new EditUserViewModel
        {
            Id = user.Id,
            Fullname = user.Fullname,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            BuildingId = user.BuildingId,
            BuildingName = user.Building?.Name,
            IsSuperAdminCaller = ctx.IsSuperAdmin
        };

        return View(vm);
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        var ctx = await this.GetCallerContextAsync(_userManager);

        if (!ModelState.IsValid)
        {
            ViewBag.Buildings = ctx!.IsSuperAdmin
                ? await _mediator.Send(new GetBuildingSelectItemsQuery())
                : model.BuildingId != null
                    ? await _mediator.Send(new GetBuildingSelectItemsQuery(model.BuildingId))
                    : [];
            return View(model);
        }

        var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == model.Id);
        if (user == null) return NotFound();

        if (User.IsInRole(Roles.President) && (ctx?.BuildingId == null || user.BuildingId != ctx.BuildingId))
            return Forbid();

        var (success, errors) = await _mediator.Send(new UpdateUserCommand(model, ctx!.IsSuperAdmin));
        if (!success)
        {
            foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
            ViewBag.Buildings = ctx.IsSuperAdmin
                ? await _mediator.Send(new GetBuildingSelectItemsQuery())
                : user.BuildingId != null
                    ? await _mediator.Send(new GetBuildingSelectItemsQuery(user.BuildingId))
                    : [];
            return View(model);
        }

        TempData["Success"] = $"Updated user {model.Fullname}.";
        return RedirectToAction(nameof(Users), new { BuildingId = model.BuildingId });
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public async Task<IActionResult> Approvals([FromQuery] ApprovalsFilterViewModel filter)
    {
        var ctx = await this.GetCallerContextAsync(_userManager);
        if (User.IsInRole(Roles.President) && ctx?.BuildingId == null) return Forbid();

        var vm = await _mediator.Send(new GetApprovalsPageQuery(filter, ctx!.BuildingId, ctx.IsSuperAdmin));
        return View(vm);
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveUser(string id, string role)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);
        var (success, message) = await _mediator.Send(new ApproveUserCommand(
            id, role, ctx!.Me.Id, ctx.IsSuperAdmin, ctx.BuildingId));

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Approvals));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkApprove(string[] ids, string role)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["Error"] = "No users selected.";
            return RedirectToAction(nameof(Approvals));
        }

        var ctx = await this.GetCallerContextAsync(_userManager);
        var applied = await _mediator.Send(new BulkApproveCommand(ids, role, ctx!.Me.Id, ctx.IsSuperAdmin, ctx.BuildingId));

        TempData["Success"] = $"Applied {applied} update(s).";
        return RedirectToAction(nameof(Approvals));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);
        var (success, message) = await _mediator.Send(new ResetUserCommand(id, ctx!.IsSuperAdmin, ctx.BuildingId));

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Approvals));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpGet]
    public async Task<IActionResult> Users([FromQuery] ManageUsersFilterViewModel filter)
    {
        var ctx = await this.GetCallerContextAsync(_userManager);
        if (User.IsInRole(Roles.President) && ctx?.BuildingId == null) return Forbid();

        var vm = await _mediator.Send(new GetUsersPageQuery(filter, ctx!.BuildingId, ctx.IsSuperAdmin));
        return View(vm);
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string role)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);
        var (success, message) = await _mediator.Send(new ChangeRoleCommand(id, role, ctx!.IsSuperAdmin, ctx.BuildingId));

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users));
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

        var ctx = await this.GetCallerContextAsync(_userManager);
        var changed = await _mediator.Send(new BulkChangeRoleCommand(ids, role, ctx!.IsSuperAdmin, ctx.BuildingId));

        TempData["Success"] = $"Changed roles for {changed} user(s).";
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BlockUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);
        var (success, message) = await _mediator.Send(new BlockUserCommand(id, ctx!.IsSuperAdmin, ctx.BuildingId));

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users));
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

        var ctx = await this.GetCallerContextAsync(_userManager);
        var blocked = await _mediator.Send(new BulkBlockCommand(ids, ctx!.IsSuperAdmin, ctx.BuildingId));

        TempData["Success"] = $"Blocked {blocked} user(s).";
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UnblockUser(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return BadRequest();

        var ctx = await this.GetCallerContextAsync(_userManager);

        var callerBuildingId = ctx!.IsSuperAdmin ? null : ctx.BuildingId;
        var (success, message) = await _mediator.Send(new UnblockUserCommand(id, callerBuildingId));

        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users));
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

        var ctx = await this.GetCallerContextAsync(_userManager);
        var callerBuildingId = ctx!.IsSuperAdmin ? null : ctx.BuildingId;
        var unblocked = await _mediator.Send(new BulkUnblockCommand(ids, callerBuildingId));

        TempData["Success"] = $"Unblocked {unblocked} user(s).";
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var (success, message) = await _mediator.Send(new DeleteUserCommand(id));
        TempData[success ? "Success" : "Error"] = message;
        return RedirectToAction(nameof(Users));
    }

    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkDelete(string[] ids)
    {
        if (ids == null || ids.Length == 0)
        {
            TempData["Error"] = "No users selected.";
            return RedirectToAction(nameof(Users));
        }

        var deleted = await _mediator.Send(new BulkDeleteCommand(ids));
        TempData["Success"] = $"Deleted {deleted} user(s).";
        return RedirectToAction(nameof(Users));
    }
}
