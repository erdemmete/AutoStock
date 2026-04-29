using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Entities
{
    public class Employee
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;

        public string? Role { get; set; } // Usta, danışman, admin vs.
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ServiceRecord> ServiceRecords { get; set; } = new List<ServiceRecord>();
    }
}
