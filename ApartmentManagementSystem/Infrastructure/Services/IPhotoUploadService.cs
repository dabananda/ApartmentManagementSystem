namespace ApartmentManagementSystem.Infrastructure.Services
{
    public interface IPhotoUploadService
    {
        Task<string> UploadAsync(IFormFile file);
    }
}
