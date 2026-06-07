using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;

public class Customer
{
    public int Id { get; set; }

    public int WorkshopId { get; set; }

    public CustomerType Type { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string? FullName { get; set; }

    public string? CompanyName { get; set; }

    public string? AuthorizedPersonName { get; set; }

    public string? Email { get; set; }

    public string? NationalIdentityNumber { get; set; }

    public string? TaxNumber { get; set; }

    public string? TaxOffice { get; set; }

    public string? AddressCity { get; set; }

    public string? AddressDistrict { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    public ICollection<CurrentAccountTransaction> CurrentAccountTransactions { get; set; } = new List<CurrentAccountTransaction>();
}