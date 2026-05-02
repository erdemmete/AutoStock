namespace AutoStock.Repositories.Entities
{
    public class WorkshopUser
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }
        public Workshop Workshop { get; set; } = null!;

        public int UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public string Role { get; set; } = "Owner";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}