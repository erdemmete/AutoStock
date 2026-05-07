using AutoStock.Repositories.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AutoStock.Repositories
{
    public class AppDbContext : IdentityDbContext<AppUser, IdentityRole<int>, int>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ServiceRecord> ServiceRecords { get; set; }
        
        public DbSet<Workshop> Workshops { get; set; }
        public DbSet<WorkshopUser> WorkshopUsers { get; set; }
        public DbSet<VehicleBrand> VehicleBrands { get; set; }

        public DbSet<VehicleModel> VehicleModels { get; set; }
        public DbSet<ServiceOperation> ServiceOperations { get; set; }

        public DbSet<ServiceRecordImage> ServiceRecordImages { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
