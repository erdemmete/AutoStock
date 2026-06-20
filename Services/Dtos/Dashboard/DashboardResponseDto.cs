namespace AutoStock.Services.Dtos.Dashboard
{
    public class DashboardResponseDto
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public int WorkshopId { get; set; }

        public string WorkshopName { get; set; } = string.Empty;

        public int ActiveServiceRecordCount { get; set; }

        public int TodayServiceRecordCount { get; set; }

        public int TodayCompletedServiceRecordCount { get; set; }

        public string TodayFilterDate { get; set; } = string.Empty;

        public string TodayDisplayDate { get; set; } = string.Empty;

        public int InProgressServiceRecordCount { get; set; }

        public int DraftInvoiceCount { get; set; }

        public int CriticalStockItemCount { get; set; }

        public List<DashboardActivityDto> RecentActivities { get; set; } = new();
    }

    public class DashboardActivityDto
    {
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
