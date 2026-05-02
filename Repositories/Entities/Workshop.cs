namespace AutoStock.Repositories.Entities
{
    public class Workshop
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ilişkiler
        public ICollection<WorkshopUser> WorkshopUsers { get; set; } = new List<WorkshopUser>();
    }
}