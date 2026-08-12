using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Features.Announcements.ViewModels;

public class AnnouncementCreateViewModel
{
    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public Announcement ToEntity()
    {
        return new Announcement
        {
            Title = Title,
            Body = Body,
            CreatedAt = DateTime.UtcNow
        };
    }
}
