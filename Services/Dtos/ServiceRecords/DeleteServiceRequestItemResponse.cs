namespace AutoStock.Services.Dtos.ServiceRecords;

public class DeleteServiceRequestItemResponse
{
    public int ServiceRecordId { get; set; }
    public int ServiceRequestItemId { get; set; }
    public decimal RecordTotal { get; set; }
}