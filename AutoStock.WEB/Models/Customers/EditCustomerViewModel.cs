using AutoStock.Repositories.Enums;

namespace AutoStock.WEB.Models.Customers
{
    public class EditCustomerViewModel
    {
        public int Id { get; set; }

        public CustomerType Type { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FullName { get; set; }
        public string? CompanyName { get; set; }
        public string? AuthorizedPersonName { get; set; }

        public string? Email { get; set; }

        public string? TaxOffice { get; set; }
        public string? TaxNumber { get; set; }
        public string? NationalIdentityNumber { get; set; }

        public string? Address { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressDistrict { get; set; }
    }
}