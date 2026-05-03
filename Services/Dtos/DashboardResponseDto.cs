namespace AutoStock.Services.Dtos.Dashboard
{
    public class DashboardResponseDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;

        public int WorkshopId { get; set; }
        public string WorkshopName { get; set; } = string.Empty;

        public int TodayServiceCount { get; set; }
        public int TotalCustomerCount { get; set; }
        public int PendingJobCount { get; set; }
    }
}