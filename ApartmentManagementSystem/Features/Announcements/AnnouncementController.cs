using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagementSystem.Features.Announcements.Services;

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

        public IActionResult Create() => View(new Announcement());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Title,Body")] Announcement model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();

            ModelState.Remove(nameof(Announcement.BuildingId));

            if (!ModelState.IsValid) return View(model);

            await _announcements.PublishAsync(model, user.BuildingId.Value);

            TempData["Ok"] = "Notice published to your building.";
            return RedirectToAction(nameof(Index));
        }
    }
}
