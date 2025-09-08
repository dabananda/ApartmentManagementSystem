using ApartmentManagementSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Services
{
    public class BuildingCodeGenerator : IBuildingCodeGenerator
    {
        private readonly ApplicationDbContext _db;
        public BuildingCodeGenerator(ApplicationDbContext db) => _db = db;

        public async Task<string> GenerateAsync(CancellationToken ct = default)
        {
            // Codes: BID1001, BID1002, ...
            var last = await _db.Buildings
                .AsNoTracking()
                .Where(b => b.Code != null && b.Code.StartsWith("BID"))
                .OrderByDescending(b => b.Code)
                .Select(b => b.Code!)
                .FirstOrDefaultAsync(ct);

            var nextNum = 1000;
            if (!string.IsNullOrWhiteSpace(last) && int.TryParse(last[3..], out var n))
                nextNum = n + 1;

            return $"BID{nextNum}";
        }
    }
}
