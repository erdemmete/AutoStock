using AutoStock.Repositories.Enums;

namespace AutoStock.Repositories.Entities
{
    public class Customer
    {
        public int Id { get; set; }

        public int WorkshopId { get; set; }

        public CustomerType Type { get; set; } = CustomerType.Individual;

        public string PhoneNumber { get; set; } = null!;

        public string? FullName { get; set; }

        public string? CompanyName { get; set; }

        public string? AuthorizedPersonName { get; set; }

        public string? Email { get; set; }

        public string? TaxNumber { get; set; }

        public string? TaxOffice { get; set; }

        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    }
}
