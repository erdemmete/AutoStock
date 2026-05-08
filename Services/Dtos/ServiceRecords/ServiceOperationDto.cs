using AutoStock.Repositories.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Dtos.ServiceRecords
{
    public class ServiceOperationDto
    {
        public int Id { get; set; }

        public OperationType Type { get; set; }

        public string Description { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string? Note { get; set; }
    }
}
