namespace AutoStock.Services.Dtos.Pdfs
{
    public class CreateServicePdfRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerEmail { get; set; }

        public string? Plate { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public string? ModelYear { get; set; }
        public string? FuelLevelText { get; set; }

        public string? Note { get; set; }

        public string? WorkshopName { get; set; }
        public string? WorkshopAddress { get; set; }
        public string? WorkshopPhone { get; set; }

        public string? RecordNumber { get; set; }
        public string? StatusText { get; set; }

        public string? VehicleVariantName { get; set; }
        public string? FuelType { get; set; }
        public string? TransmissionType { get; set; }
        public string? BodyType { get; set; }
        public int? EngineCapacityCc { get; set; }
        public int? EnginePowerHp { get; set; }
        public string? EngineCode { get; set; }
        public string? ChassisNumber { get; set; }

        public bool IsPublicMasked { get; set; }

        public List<ServicePdfItemDto> Operations { get; set; } = new();
        public List<ServicePdfRequestGroupDto> RequestGroups { get; set; } = new();
        public List<ServicePdfBankAccountDto> BankAccounts { get; set; } = new();
    }

    public class ServicePdfBankAccountDto
    {
        public int Id { get; set; }
        public string BankName { get; set; } = null!;
        public string AccountHolder { get; set; } = null!;
        public string Iban { get; set; } = null!;
        public string CurrencyCode { get; set; } = "TRY";
        public string? Description { get; set; }
        public bool IsDefault { get; set; }
        public int SortOrder { get; set; }
    }
}
