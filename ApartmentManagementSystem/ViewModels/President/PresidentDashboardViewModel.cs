namespace ApartmentManagementSystem.ViewModels.President
{
    public class PresidentDashboardViewModel
    {
        // Header
        public string BuildingName { get; set; } = "";

        // Financial
        public decimal TotalBills { get; set; }
        public decimal TotalCollected { get; set; }   // from paid ExpenseAllocations
        public decimal TotalPayments { get; set; }    // ExpensePayments
        public decimal Balance => TotalCollected - TotalPayments;

        // Flats / occupancy
        public int TotalFlats { get; set; }
        public int OccupiedFlats { get; set; }
        public int VacantFlats => Math.Max(TotalFlats - OccupiedFlats, 0);

        // Entry logs
        public int TodayEntries { get; set; }
        public int Last7dEntries { get; set; }
        public Dictionary<string, int> EntryByCategory { get; set; } = new();

        // Maintenance & announcements (placeholder lists)
        public List<string> RecentAnnouncements { get; set; } = new();
        public List<string> OpenMaintenance { get; set; } = new();
        public List<TransactionRowViewModel> RecentTransactions { get; set; } = new();
    }
}
