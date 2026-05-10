using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class CreateServiceRecordRequest
    {
        public string CustomerPhoneNumber { get; set; } = null!;

        public string CustomerName { get; set; } = null!;
        public string? CustomerEmail { get; set; }

        public string Plate { get; set; } = null!;

        public int? VehicleBrandId { get; set; }

        public int? VehicleModelId { get; set; }

        public int? ModelYear { get; set; }

        public int? Mileage { get; set; }

        public string? CustomerComplaint { get; set; }

        public string? ServiceReceptionNote { get; set; }
        public decimal? EstimatedAmount { get; set; }

        public string? EstimatedAmountNote { get; set; }

        public List<CreateServiceRequestItemDto> RequestItems { get; set; } = new();
    }
}
