namespace ApartmentManagementSystem.ViewModels.Reports
{
    public class VisitorReportViewModel
    {
        public string BuildingName { get; set; } = "";
        public Reports.DateRangeFilter Filter { get; set; } = new Reports.DateRangeFilter();

        public int TotalEntries { get; set; }
        public Dictionary<string, int> ByCategory { get; set; } = new();
        public List<(DateTime Day, int Count)> DailyCounts { get; set; } = new();
    }
}
