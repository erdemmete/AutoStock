using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services
{
    public class ServicePdfItemDto
    {
        public string Description { get; set; } = null!;

        public decimal Price { get; set; }

        public int Quantity { get; set; } = 1;

        public decimal TotalPrice => Price * Quantity;
    }
}
