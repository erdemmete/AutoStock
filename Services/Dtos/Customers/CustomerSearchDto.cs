namespace AutoStock.Services.Dtos.Customers
{
    public class CustomerSearchDto
    {
        public int Id { get; set; }

        public int Type { get; set; }

        public string Name { get; set; } = null!;

        public string? CustomerName { get; set; }

        public string? CompanyName { get; set; }

        public string? AuthorizedPersonName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? NationalIdentityNumber { get; set; }

        public string? TaxOffice { get; set; }

        public string? TaxNumber { get; set; }

        public string? AddressCity { get; set; }

        public string? AddressDistrict { get; set; }

        public string? CustomerAddress { get; set; }
    }
}