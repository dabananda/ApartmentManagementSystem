using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Features.Payments;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Flats.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AMS.Application.Features.Tenancy.Services;

namespace AMS.Web.Features.Flats
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class FlatBillingProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IFlatBillingProfileService _profiles;
        public FlatBillingProfileController(UserManager<ApplicationUser> users, IFlatBillingProfileService profiles)
        {
            _users = users; _profiles = profiles;
        }

        public async Task<IActionResult> Index()
        {
            var me = await _users.GetUserAsync(User);

            var rows = await _profiles.GetRowsAsync(User.IsInRole("Owner") ? me!.Id : null);

            return View(rows);
        }

        public async Task<IActionResult> Edit(Guid flatId)
        {
            var me = await _users.GetUserAsync(User);
            var flat = await _profiles.GetFlatAsync(flatId);
            if (flat == null) return NotFound();
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            var p = await _profiles.GetProfileAsync(flatId)
                ?? new FlatBillingProfile { FlatId = flatId };

            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FlatBillingProfile vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var me = await _users.GetUserAsync(User);
            var flat = await _profiles.GetFlatAsync(vm.FlatId);
            if (flat == null) return NotFound("Flat not found.");

            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            await _profiles.SaveAsync(vm);

            TempData["Success"] = "Billing profile saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
