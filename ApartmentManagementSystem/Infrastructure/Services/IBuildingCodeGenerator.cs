namespace ApartmentManagementSystem.Infrastructure.Services
{
    public interface IBuildingCodeGenerator
    {
        Task<string> GenerateAsync(CancellationToken ct = default);
    }
}
