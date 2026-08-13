using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Announcements.DTOs;

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
