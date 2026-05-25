namespace AutoStock.WEB.Models.Admin.Workshops
{
    public class UpdateAdminWorkshopProfileViewModel
    {
        public int WorkshopId { get; set; }

        public string? DisplayName { get; set; }

        public string? LegalTitle { get; set; }

        public string? TaxOffice { get; set; }

        public string? TaxNumber { get; set; }

        public string? TradeRegistryNumber { get; set; }

        public string? MersisNumber { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public string? FaxNumber { get; set; }

        public string? Website { get; set; }

        public string? AddressLine { get; set; }

        public string? City { get; set; }

        public string? District { get; set; }

        public string? PostalCode { get; set; }

        public string? Country { get; set; }
    }
}