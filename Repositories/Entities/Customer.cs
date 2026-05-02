namespace AutoStock.Repositories.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string? Email { get; set; }

        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    }
}
