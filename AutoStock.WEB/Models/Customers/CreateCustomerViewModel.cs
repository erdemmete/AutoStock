using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.Customers
{
    public class CreateCustomerViewModel
    {
        public CustomerType Type { get; set; } = CustomerType.Individual;

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
    }
}