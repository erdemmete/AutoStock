namespace AutoStock.WEB.Models
{
    public class AuthResponseViewModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int WorkshopId { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}