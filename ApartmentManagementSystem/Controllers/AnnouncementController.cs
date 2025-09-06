using System;
using System.Linq;
using System.Threading.Tasks;
using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President,SuperAdmin")]
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnnouncementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Announcement
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            var items = await _context.Announcements
                .AsNoTracking()
                .Where(a => a.BuildingId == buildingId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // GET: /Announcement/Create
        public IActionResult Create() => View(new Announcement());

        // POST: /Announcement/Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            if (!ModelState.IsValid) return View(model);

            model.Id = Guid.NewGuid();
            model.BuildingId = buildingId;
            model.CreatedAt = DateTime.UtcNow;

            _context.Announcements.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
