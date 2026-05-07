using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class CreateServiceRecordResponse
    {
        public int ServiceRecordId { get; set; }

        public string Message { get; set; } = "Servis kaydı başarıyla oluşturuldu.";
        public string RecordNumber { get; set; } = null!;
    }
}