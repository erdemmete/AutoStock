namespace AutoStock.Services.Dtos.ServiceRecords;

public class DeleteServiceOperationResponse
{
    public int ServiceRecordId { get; set; }
    public int? ServiceRequestItemId { get; set; }
    public decimal RequestItemTotal { get; set; }
    public decimal RecordTotal { get; set; }
}