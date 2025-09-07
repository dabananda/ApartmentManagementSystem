using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class MonthlyBillGenerator : BackgroundService
    {
        private readonly IServiceProvider _sp;

        public MonthlyBillGenerator(IServiceProvider sp) => _sp = sp;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Simple scheduler: wait until the next 00:05 UTC on the 1st, then run monthly
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var next = new DateTime(now.Year, now.Month, 1, 0, 5, 0, DateTimeKind.Utc);
                if (now >= next) next = next.AddMonths(1);

                var delay = next - now;
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                try { await GenerateCurrentMonth(stoppingToken); }
                catch { /* swallow to keep background loop alive; add logging if you like */ }
            }
        }

        private async Task GenerateCurrentMonth(CancellationToken ct)
        {
            using var scope = _sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var now = DateTime.UtcNow;
            int y = now.Year, m = now.Month;

            // Pull all active profiles with a flat that has an active tenant
            var profiles = await db.OwnerBillingProfiles
                .Include(p => p.Flat)
                .Where(p => p.IsActive)
                .ToListAsync(ct);

            foreach (var p in profiles)
            {
                // Find active tenant for the flat
                var tenant = await db.Tenants
                    .Where(t => t.FlatId == p.FlatId && t.IsActive)
                    .FirstOrDefaultAsync(ct);

                if (tenant == null) continue;

                bool exists = await db.TenantBills
                    .AnyAsync(b => b.FlatId == p.FlatId && b.Year == y && b.Month == m, ct);

                if (exists) continue;

                var total = p.RentAmount + p.ElectricityAmount + p.GasAmount + p.WaterAmount +
                            p.CommonBillAmount + p.ServiceChargeAmount + p.OtherAmount;

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
                    DueDate = new DateTime(y, m, 1)
                };

                db.TenantBills.Add(bill);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
