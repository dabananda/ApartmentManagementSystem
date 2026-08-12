using System.ComponentModel.DataAnnotations;

namespace AMS.Application.Features.Reports.DTOs;

public class DateRangeFilter
{
    [DataType(DataType.Date)]
    public DateTime? From { get; set; }

    [DataType(DataType.Date)]
    public DateTime? To { get; set; }

    public (DateTime start, DateTime endExclusive) ToBoundsOrDefault(int defaultDays = 30)
    {
        var today = DateTime.Today;
        var start = From?.Date ?? today.AddDays(-defaultDays);
        var endExclusive = (To?.Date ?? today).AddDays(1);
        return (start, endExclusive);
    }
}
