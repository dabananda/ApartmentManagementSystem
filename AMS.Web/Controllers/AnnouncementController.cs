using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Announcements.Queries;
using AMS.Application.Features.Announcements.Commands;
using AMS.Application.Features.Announcements.DTOs;
using AMS.Application.Interfaces.Administration;
using AMS.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.President)]
    public class AnnouncementController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;

        public AnnouncementController(UserManager<ApplicationUser> userManager, IMediator mediator)
        {
            _userManager = userManager;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();

            var buildingId = user.BuildingId.Value;

            var items = await _mediator.Send(new GetAnnouncementsForBuildingQuery(buildingId));

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
            await _mediator.Send(new PublishAnnouncementCommand(announcement, user.BuildingId.Value));

            TempData["Ok"] = "Notice published to your building.";
            return RedirectToAction(nameof(Index));
        }
    }
}
