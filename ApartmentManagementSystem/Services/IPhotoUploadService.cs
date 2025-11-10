namespace ApartmentManagementSystem.Services
{
    public interface IPhotoUploadService
    {
        Task<string> UploadAsync(IFormFile file);
    }
}
