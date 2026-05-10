using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Dtos.Customers
{
    public class CustomerSearchDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }
    }
}
