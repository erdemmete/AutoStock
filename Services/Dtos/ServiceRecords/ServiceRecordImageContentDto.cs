namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class ServiceRecordImageContentDto
    {
        public byte[] Content { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } = "application/octet-stream";

        public string FileName { get; set; } = "image";
    }
}