using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class VehicleBrand
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public bool IsActive { get; set; } = true;

        public ICollection<VehicleModel> Models { get; set; } = new List<VehicleModel>();
    }
}
