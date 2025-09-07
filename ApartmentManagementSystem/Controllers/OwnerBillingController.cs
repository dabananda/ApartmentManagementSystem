using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "Owner,SuperAdmin")]
    public class OwnerBillingController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _users;

        public OwnerBillingController(ApplicationDbContext db, UserManager<ApplicationUser> users)
        {
            _db = db; _users = users;
        }

        // GET: /OwnerBilling/Profile/{flatId}
        public async Task<IActionResult> Profile(Guid? flatId)
        {
            if (flatId is null || flatId == Guid.Empty) return NotFound();

            var flat = await _db.Flats.Include(f => f.Building).FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            // NOTE: if Flat.OwnerId is Guid, cast/parse accordingly.
            if (!User.IsInRole("SuperAdmin") && flat.OwnerId != me.Id) return Forbid();

            var profile = await _db.OwnerBillingProfiles.FirstOrDefaultAsync(x => x.FlatId == flat.Id)
                          ?? new OwnerBillingProfile { FlatId = flat.Id };

            ViewData["FlatNumber"] = flat.FlatNumber;
            return View(profile);
        }

        // POST: /OwnerBilling/Profile
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(OwnerBillingProfile model)
        {
            var flat = await _db.Flats.FirstOrDefaultAsync(f => f.Id == model.FlatId);
            if (flat == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (!User.IsInRole("SuperAdmin") && flat.OwnerId != me.Id) return Forbid();

            var existing = await _db.OwnerBillingProfiles.FirstOrDefaultAsync(x => x.FlatId == model.FlatId);
            if (existing == null)
            {
                model.Id = Guid.NewGuid();
                model.CreatedAt = DateTime.UtcNow;
                _db.OwnerBillingProfiles.Add(model);
            }
            else
            {
                existing.RentAmount = model.RentAmount;
                existing.ElectricityAmount = model.ElectricityAmount;
                existing.GasAmount = model.GasAmount;
                existing.WaterAmount = model.WaterAmount;
                existing.CommonBillAmount = model.CommonBillAmount;
                existing.ServiceChargeAmount = model.ServiceChargeAmount;
                existing.OtherAmount = model.OtherAmount;
                existing.Notes = model.Notes;
                existing.IsActive = model.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Billing profile saved.";
            return RedirectToAction(nameof(Profile), new { flatId = model.FlatId });
        }

        // GET: /OwnerBilling/Bills/{flatId}
        public async Task<IActionResult> Bills(Guid? flatId)
        {
            if (flatId is null || flatId == Guid.Empty) return NotFound();

            var flat = await _db.Flats.Include(f => f.Building).FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (!User.IsInRole("SuperAdmin") && flat.OwnerId != me.Id) return Forbid();

            var bills = await _db.TenantBills
                .Where(b => b.FlatId == flat.Id)
                .OrderByDescending(b => b.Year).ThenByDescending(b => b.Month)
                .ToListAsync();

            ViewData["FlatId"] = flat.Id;
            ViewData["FlatNumber"] = flat.FlatNumber;
            return View(bills);
        }

        // GET: /OwnerBilling/CreateBill/{flatId}?year=2025&month=9
        public async Task<IActionResult> CreateBill(Guid? flatId, int? year, int? month)
        {
            if (flatId is null || flatId == Guid.Empty) return NotFound();

            var flat = await _db.Flats.Include(f => f.Tenants).FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (!User.IsInRole("SuperAdmin") && flat.OwnerId != me.Id) return Forbid();

            var tenant = flat.Tenants?.FirstOrDefault(t => t.IsActive);
            if (tenant == null) return BadRequest("No active tenant in this flat.");

            var profile = await _db.OwnerBillingProfiles.FirstOrDefaultAsync(x => x.FlatId == flat.Id);
            if (profile == null) return RedirectToAction(nameof(Profile), new { flatId = flat.Id });

            var now = DateTime.UtcNow;
            int y = year ?? now.Year, m = month ?? now.Month;

            bool exists = await _db.TenantBills.AnyAsync(b => b.FlatId == flat.Id && b.Year == y && b.Month == m);
            if (exists) return RedirectToAction(nameof(Bills), new { flatId = flat.Id });

            var total = profile.RentAmount + profile.ElectricityAmount + profile.GasAmount + profile.WaterAmount
                        + profile.CommonBillAmount + profile.ServiceChargeAmount + profile.OtherAmount;

            var bill = new TenantBill
            {
                Id = Guid.NewGuid(),
                FlatId = flat.Id,
                TenantId = tenant.Id,
                Year = y,
                Month = m,
                RentAmount = profile.RentAmount,
                ElectricityAmount = profile.ElectricityAmount,
                GasAmount = profile.GasAmount,
                WaterAmount = profile.WaterAmount,
                CommonBillAmount = profile.CommonBillAmount,
                ServiceChargeAmount = profile.ServiceChargeAmount,
                OtherAmount = profile.OtherAmount,
                TotalAmount = total,
                PaidAmount = 0m,
                Status = "Unpaid",
                CreatedAt = DateTime.UtcNow,
                DueDate = new DateTime(y, m, 1)
            };

            _db.TenantBills.Add(bill);
            await _db.SaveChangesAsync();

            TempData["Ok"] = "Bill created.";
            return RedirectToAction(nameof(Bills), new { flatId = flat.Id });
        }

        // POST: /OwnerBilling/ApplyPayment
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyPayment(Guid billId, DateTime paymentDate, decimal amount, string? notes)
        {
            var bill = await _db.TenantBills.Include(b => b.Tenant).FirstOrDefaultAsync(b => b.Id == billId);
            if (bill == null) return NotFound();

            var flat = await _db.Flats.FirstOrDefaultAsync(f => f.Id == bill.FlatId);
            if (flat == null) return NotFound();

            var me = await _users.GetUserAsync(User);
            if (!User.IsInRole("SuperAdmin") && flat.OwnerId != me.Id) return Forbid();

            // Use existing Rent as the "payment" record
            var rent = new Rent
            {
                Id = Guid.NewGuid(),
                PaymentDate = paymentDate,
                Amount = amount,
                Notes = string.IsNullOrWhiteSpace(notes) ? $"Payment for {bill.Year}-{bill.Month:D2}" : notes,
                TenantId = bill.TenantId,
                TenantBillId = bill.Id
            };
            _db.Rents.Add(rent);

            // Roll-up bill status
            bill.PaidAmount += amount;
            bill.Status = bill.PaidAmount <= 0 ? "Unpaid"
                       : bill.PaidAmount < bill.TotalAmount ? "PartiallyPaid"
                       : "Paid";

            await _db.SaveChangesAsync();
            TempData["Ok"] = "Payment applied.";
            return RedirectToAction(nameof(Bills), new { flatId = bill.FlatId });
        }
    }
}