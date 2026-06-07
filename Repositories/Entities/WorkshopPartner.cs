namespace AutoStock.Repositories.Entities
{
    public class WorkshopPartner
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public Workshop Workshop { get; set; } = null!;

        public string FullName { get; set; } = null!;

        public string? Title { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public bool IsPrimary { get; set; }

        public string? Note { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}