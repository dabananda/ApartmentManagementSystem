using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class TenantMonthlyBillGenerator : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<TenantMonthlyBillGenerator> _logger;
        public TenantMonthlyBillGenerator(IServiceProvider sp, ILogger<TenantMonthlyBillGenerator> logger)
        {
            _sp = sp; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // run once a day ~00:10
                    var now = DateTime.Now;
                    var delay = DateTime.Today.AddDays(1).AddMinutes(10) - now;
                    if (delay < TimeSpan.Zero) delay = TimeSpan.FromHours(24);
                    await Task.Delay(delay, stoppingToken);

                    if (DateTime.Today.Day != 1) continue;

                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // active assignments with active profile
                    var q = await db.TenantAssignments
                        .Include(a => a.Flat)
                        .Where(a => a.EndDate == null)
                        .Select(a => new { a.FlatId, a.TenantUserId })
                        .ToListAsync(stoppingToken);

                    if (q.Count == 0) continue;

                    var profiles = await db.FlatBillingProfiles.Where(p => p.IsActive).ToListAsync(stoppingToken);
                    var pByFlat = profiles.ToDictionary(x => x.FlatId, x => x);

                    foreach (var a in q)
                    {
                        if (!pByFlat.TryGetValue(a.FlatId, out var prof)) continue;

                        var firstOfMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

                        // avoid duplicates
                        var exists = await db.TenantBills.AnyAsync(tb =>
                            tb.FlatId == a.FlatId && tb.TenantUserId == a.TenantUserId && tb.BillDate == firstOfMonth, stoppingToken);
                        if (exists) continue;

                        await db.TenantBills.AddAsync(new TenantBill
                        {
                            FlatId = a.FlatId,
                            TenantUserId = a.TenantUserId,
                            Title = prof.Title,
                            BillDate = firstOfMonth,
                            Amount = prof.MonthlyAmount
                        }, stoppingToken);
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("TenantMonthlyBillGenerator: bills generated for {Date}", DateTime.Today);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "TenantMonthlyBillGenerator failed");
                }
            }
        }
    }
}
