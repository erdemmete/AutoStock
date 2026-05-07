namespace AutoStock.WEB.Models.ServiceRecords
{
    public class CreateServiceRecordResponseViewModel
    {
        public int ServiceRecordId { get; set; }

        public string Message { get; set; } = null!;

        public string RecordNumber { get; set; } = null!;
    }
}
