namespace AutoStock.WEB.Models.ServiceRecords;

public class CreateVehicleCatalogSupportRequestViewModel
{
    public string? MissingVehicleInfo { get; set; }

    public string? SelectedBrandText { get; set; }

    public string? SelectedModelText { get; set; }

    public string? SelectedVariantText { get; set; }

    public string? Plate { get; set; }

    public string? ModelYear { get; set; }

    public string? ChassisNumber { get; set; }
}
