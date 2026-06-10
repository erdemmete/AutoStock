namespace AutoStock.Services.Dtos.ServiceRecords;

public class RestoreServiceRequestItemResponse
{
    public int ServiceRecordId { get; set; }

    public int ServiceRequestItemId { get; set; }

    public decimal RecordTotal { get; set; }
}