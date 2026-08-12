using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace AMS.Infrastructure.Services
{
    public interface IPhotoUploadService
    {
        Task<string> UploadAsync(IFormFile file);
    }
}
