using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class Vehicle
    {
        public int Id { get; set; }

        public string PlateNumber { get; set; } = null!;
        public string Brand { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int? Year { get; set; }

        public string? VinNumber { get; set; } // Şasi no
        public string? EngineNumber { get; set; }
        public int? Kilometer { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    }
}
