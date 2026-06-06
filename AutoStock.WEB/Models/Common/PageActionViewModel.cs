namespace AutoStock.WEB.Models.Common
{
    public class PageActionViewModel
    {
        public bool ShowBack { get; set; } = true;
        public bool ShowHome { get; set; } = true;

        public string BackText { get; set; } = "← Geri";
        public string BackController { get; set; } = "Dashboard";
        public string BackAction { get; set; } = "Index";
        public Dictionary<string, string?>? BackRouteValues { get; set; }

        public string HomeText { get; set; } = "🏠 Ana Sayfa";
        public string HomeController { get; set; } = "Dashboard";
        public string HomeAction { get; set; } = "Index";
        public Dictionary<string, string?>? HomeRouteValues { get; set; }

        public string? PrimaryText { get; set; }
        public string? PrimaryController { get; set; }
        public string? PrimaryAction { get; set; }
        public Dictionary<string, string?>? PrimaryRouteValues { get; set; }
    }
}