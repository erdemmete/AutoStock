using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Services.Dtos.Vehicles
{
    public class VehicleModelDto
    {
        public int Id { get; set; }

        public int VehicleBrandId { get; set; }

        public string Name { get; set; } = null!;
    }
}
