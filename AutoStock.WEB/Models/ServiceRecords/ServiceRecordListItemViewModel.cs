namespace AutoStock.Web.Models.ServiceRecords;

public class ServiceRecordListItemViewModel
{
    public int Id { get; set; }

    public string RecordNumber { get; set; } = null!;

    public int Status { get; set; }

    public string CustomerName { get; set; } = null!;

    public string CustomerPhone { get; set; } = null!;

    public string VehiclePlate { get; set; } = null!;

    public string? VehicleBrandName { get; set; }

    public string? VehicleModelName { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
}