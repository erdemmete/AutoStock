using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services
{
    public class ServicePdfItemDto
    {
        public string? Name { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
