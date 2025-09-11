using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.President)]
    public class PresidentUsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;
        private readonly IEmailSender _mail;

        public PresidentUsersController(ApplicationDbContext db, UserManager<ApplicationUser> users, IEmailSender mail)
        {
            _db = db;
            _users = users;
            _mail = mail;
        }

        // List pending users in my building
        public async Task<IActionResult> Pending()
        {
            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId == null) return Forbid();

            var pending = await _users.Users
                .Where(u => u.BuildingId == me.BuildingId)
                .Select(u => new
                {
                    User = u,
                    Roles = _db.UserRoles.Where(ur => ur.UserId == u.Id).Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                })
                .ToListAsync();

            var model = pending
                .Where(x => x.User.EmailConfirmed && (!x.User.IsApproved || (x.Roles.Count() == 1 && x.Roles.Contains("User"))))
                .Select(x => x.User)
                .ToList();

            return View(model); // simple table view
        }

        // Approve user and assign role
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAndAssign(string userId, string role) // role is ignored; presidents can only assign Tenant
        {
            var me = await _users.GetUserAsync(User);
            if (me?.BuildingId == null) return Forbid();

            var target = await _users.FindByIdAsync(userId);
            if (target == null || target.BuildingId != me.BuildingId) return Forbid();

            // Presidents can approve ONLY as Tenant (Owner is reserved for Presidents)
            const string finalRole = "Tenant";

            // Remove all “end-state” roles first
            if (await _users.IsInRoleAsync(target, "Owner"))
                await _users.RemoveFromRoleAsync(target, "Owner");
            if (await _users.IsInRoleAsync(target, "Tenant"))
                await _users.RemoveFromRoleAsync(target, "Tenant");
            if (await _users.IsInRoleAsync(target, "User"))
                await _users.RemoveFromRoleAsync(target, "User");

            await _users.AddToRoleAsync(target, finalRole);

            target.IsApproved = true;
            target.ApprovedAt = DateTime.UtcNow;
            target.ApprovedByUserId = me.Id;
            await _users.UpdateAsync(target);

            if (!string.IsNullOrWhiteSpace(target.Email))
            {
                var subject = "Your account has been approved";
                var body = $@"
            <p>Hi {target.Fullname},</p>
            <p>Your account has been approved and your role is now <strong>{finalRole}</strong>. You can log in now.</p>";
                await _mail.SendEmailAsync(target.Email, subject, body);
            }

            TempData["Success"] = "User approved as Tenant.";
            return RedirectToAction(nameof(Pending));
        }

    }
}
