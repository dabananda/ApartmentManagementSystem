using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    /// <summary>
    /// Runs daily at ~02:00 UTC. Ensures a bill exists for the current month per active OwnerBillingProfile.
    /// Idempotent via (FlatId,Year,Month) unique index.
    /// Also emails tenant if a new bill is created.
    /// </summary>
    public class MonthlyBillGenerator : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<MonthlyBillGenerator> _logger;

        public MonthlyBillGenerator(IServiceProvider services, ILogger<MonthlyBillGenerator> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    var nextRun = new DateTime(now.Year, now.Month, now.Day, 2, 0, 0, DateTimeKind.Utc);
                    if (now > nextRun) nextRun = nextRun.AddDays(1);
                    await Task.Delay(nextRun - now, stoppingToken);
                    await RunOnce(stoppingToken);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MonthlyBillGenerator failed; retrying in 1h");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        public async Task RunOnce(CancellationToken ct = default)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var mail = scope.ServiceProvider.GetRequiredService<IEmailSender>();

            var today = DateTime.UtcNow;
            int y = today.Year, m = today.Month;

            var profiles = await db.OwnerBillingProfiles
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new { p.FlatId, p.RentAmount, p.ElectricityAmount, p.GasAmount, p.WaterAmount, p.CommonBillAmount, p.ServiceChargeAmount, p.OtherAmount })
                .ToListAsync(ct);

            if (profiles.Count == 0) return;

            var flatIds = profiles.Select(p => p.FlatId).Distinct().ToList();

            var existing = await db.TenantBills
                .Where(b => flatIds.Contains(b.FlatId) && b.Year == y && b.Month == m)
                .Select(b => b.FlatId)
                .ToListAsync(ct);
            var exist = existing.ToHashSet();

            var newBills = new List<TenantBill>();

            // map active tenant per flat
            var activeTenants = await db.Tenants
                .AsNoTracking()
                .Where(t => flatIds.Contains(t.FlatId) && t.IsActive)
                .Select(t => new { t.Id, t.FlatId, t.UserId })
                .ToListAsync(ct);

            var userEmails = await db.Users
                .AsNoTracking()
                .Where(u => activeTenants.Select(t => t.UserId).Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.UserName })
                .ToListAsync(ct);
            var emailByUserId = userEmails.ToDictionary(x => x.Id, x => x.Email);

            foreach (var p in profiles)
            {
                if (exist.Contains(p.FlatId)) continue;

                var tenant = activeTenants.FirstOrDefault(t => t.FlatId == p.FlatId);
                if (tenant == null) continue; // no active tenant

                var total = p.RentAmount + p.ElectricityAmount + p.GasAmount + p.WaterAmount
                            + p.CommonBillAmount + p.ServiceChargeAmount + p.OtherAmount;

                var due = new DateTime(y, m, 1);

                var bill = new TenantBill
                {
                    Id = Guid.NewGuid(),
                    FlatId = p.FlatId,
                    TenantId = tenant.Id,
                    Year = y,
                    Month = m,
                    RentAmount = p.RentAmount,
                    ElectricityAmount = p.ElectricityAmount,
                    GasAmount = p.GasAmount,
                    WaterAmount = p.WaterAmount,
                    CommonBillAmount = p.CommonBillAmount,
                    ServiceChargeAmount = p.ServiceChargeAmount,
                    OtherAmount = p.OtherAmount,
                    TotalAmount = total,
                    PaidAmount = 0m,
                    Status = "Unpaid",
                    CreatedAt = DateTime.UtcNow,
                    DueDate = due
                };

                db.TenantBills.Add(bill);
                newBills.Add(bill);
            }

            if (newBills.Count == 0)
            {
                _logger.LogInformation("MonthlyBillGenerator: nothing to create for {Y}-{M:00}", y, m);
                return;
            }

            await db.SaveChangesAsync(ct);

            // Send emails (best-effort; ignore failures)
            // Replace the email sending logic in MonthlyBillGenerator.RunOnce method

            // Send emails (best-effort; ignore failures)
            foreach (var bill in newBills)
            {
                try
                {
                    var tenant = activeTenants.FirstOrDefault(t => t.Id == bill.TenantId);
                    if (tenant == null) continue;

                    string? emailToSend = null;

                    // First, try to get email from the associated Identity user
                    if (!string.IsNullOrEmpty(tenant.UserId) && emailByUserId.TryGetValue(tenant.UserId, out var userEmail))
                    {
                        emailToSend = userEmail;
                    }

                    // If no Identity user email, get the tenant's direct email from the database
                    if (string.IsNullOrWhiteSpace(emailToSend))
                    {
                        var tenantWithEmail = await db.Tenants.AsNoTracking()
                            .FirstOrDefaultAsync(t => t.Id == tenant.Id, ct);
                        emailToSend = tenantWithEmail?.Email;
                    }

                    // Send email if we have a valid email address
                    if (!string.IsNullOrWhiteSpace(emailToSend))
                    {
                        var subject = $"Your {bill.Year}-{bill.Month:D2} bill is ready";
                        var body = $@"
                                    <p>Dear Tenant,</p>
                                    <p>Your monthly bill for <strong>{bill.Year}-{bill.Month:D2}</strong> has been generated.</p>
                                    <ul>
                                        <li>Total Amount: <strong>{bill.TotalAmount:C}</strong></li>
                                        <li>Due Date: <strong>{bill.DueDate:yyyy-MM-dd}</strong></li>
                                        <li>Status: <strong>{bill.Status}</strong></li>
                                    </ul>
                                    <p>Please log in to your tenant portal to view details and make payments.</p>
                                    <p>Best regards,<br/>Apartment Management</p>";

                        await mail.SendEmailAsync(emailToSend, subject, body);
                        _logger.LogInformation("Bill notification email sent to {Email} for bill {BillId}", emailToSend, bill.Id);
                    }
                    else
                    {
                        _logger.LogWarning("No email address found for tenant {TenantId} - cannot send bill notification", tenant.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email notification for bill {BillId}", bill.Id);
                }
            }

            _logger.LogInformation("MonthlyBillGenerator: created {Count} bills for {Y}-{M:00}", newBills.Count, y, m);
        }
    }
}
