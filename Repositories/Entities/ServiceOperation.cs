using AutoStock.Repositories.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class ServiceOperation
    {
        public int Id { get; set; }

        public int ServiceRecordId { get; set; }

        public ServiceRecord ServiceRecord { get; set; } = null!;

        public OperationType Type { get; set; }

        public string Description { get; set; } = null!;

        public int Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public string? Note { get; set; }
    }
}
