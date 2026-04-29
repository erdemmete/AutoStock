using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services
{
    public class CreateServicePdfRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }

        public string? PlateNumber { get; set; }
        public string? VehicleBrand { get; set; }
        public string? VehicleModel { get; set; }

        public string? Note { get; set; }

        public List<ServicePdfItemDto> Items { get; set; } = new();
    }
}
