using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class VehicleModel
    {
        public int Id { get; set; }

        public int VehicleBrandId { get; set; }

        public VehicleBrand VehicleBrand { get; set; } = null!;

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;
    }
}
