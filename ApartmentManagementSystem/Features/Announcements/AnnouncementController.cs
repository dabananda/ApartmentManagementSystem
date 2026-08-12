using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Announcements.Services;
using ApartmentManagementSystem.Application.Features.Announcements.DTOs;
using ApartmentManagementSystem.Application.Interfaces.Administration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.Announcements
{
    [Authorize(Roles = Roles.President)]
    public class AnnouncementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAnnouncementService _announcements;

        public AnnouncementController(UserManager<ApplicationUser> userManager, IAnnouncementService announcements)
        {
            _userManager = userManager;
            _announcements = announcements;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();

            var buildingId = user.BuildingId.Value;

            var items = await _announcements.GetForBuildingAsync(buildingId);

            return View(items);
        }

        public IActionResult Create() => View(new AnnouncementCreateViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AnnouncementCreateViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();

            if (!ModelState.IsValid) return View(model);

            var announcement = model.ToEntity();
            await _announcements.PublishAsync(announcement, user.BuildingId.Value);

            TempData["Ok"] = "Notice published to your building.";
            return RedirectToAction(nameof(Index));
        }
    }
}
