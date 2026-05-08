using AutoStock.Repositories.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

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
        public DbSet<ServiceRequestItem> ServiceRequestItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            builder.Entity<VehicleBrand>().HasData(
    new VehicleBrand { Id = 1, Name = "Toyota", IsActive = true },
    new VehicleBrand { Id = 2, Name = "Honda", IsActive = true },
    new VehicleBrand { Id = 3, Name = "Volkswagen", IsActive = true },
    new VehicleBrand { Id = 4, Name = "BMW", IsActive = true },
    new VehicleBrand { Id = 5, Name = "Mercedes-Benz", IsActive = true },
    new VehicleBrand { Id = 6, Name = "Ford", IsActive = true },
    new VehicleBrand { Id = 7, Name = "Renault", IsActive = true },
    new VehicleBrand { Id = 8, Name = "Fiat", IsActive = true },
    new VehicleBrand { Id = 9, Name = "Hyundai", IsActive = true },
    new VehicleBrand { Id = 10, Name = "Peugeot", IsActive = true }
);

            builder.Entity<VehicleModel>().HasData(
                new VehicleModel { Id = 1, VehicleBrandId = 1, Name = "Corolla", IsActive = true },
                new VehicleModel { Id = 2, VehicleBrandId = 1, Name = "Yaris", IsActive = true },
                new VehicleModel { Id = 3, VehicleBrandId = 1, Name = "C-HR", IsActive = true },

                new VehicleModel { Id = 4, VehicleBrandId = 2, Name = "Civic", IsActive = true },
                new VehicleModel { Id = 5, VehicleBrandId = 2, Name = "Jazz", IsActive = true },
                new VehicleModel { Id = 6, VehicleBrandId = 2, Name = "CR-V", IsActive = true },

                new VehicleModel { Id = 7, VehicleBrandId = 3, Name = "Golf", IsActive = true },
                new VehicleModel { Id = 8, VehicleBrandId = 3, Name = "Passat", IsActive = true },
                new VehicleModel { Id = 9, VehicleBrandId = 3, Name = "Polo", IsActive = true },

                new VehicleModel { Id = 10, VehicleBrandId = 4, Name = "3 Series", IsActive = true },
                new VehicleModel { Id = 11, VehicleBrandId = 4, Name = "5 Series", IsActive = true },
                new VehicleModel { Id = 12, VehicleBrandId = 4, Name = "X5", IsActive = true },

                new VehicleModel { Id = 13, VehicleBrandId = 5, Name = "C-Class", IsActive = true },
                new VehicleModel { Id = 14, VehicleBrandId = 5, Name = "E-Class", IsActive = true },
                new VehicleModel { Id = 15, VehicleBrandId = 5, Name = "Sprinter", IsActive = true },

                new VehicleModel { Id = 16, VehicleBrandId = 6, Name = "Focus", IsActive = true },
                new VehicleModel { Id = 17, VehicleBrandId = 6, Name = "Fiesta", IsActive = true },
                new VehicleModel { Id = 18, VehicleBrandId = 6, Name = "Transit", IsActive = true },

                new VehicleModel { Id = 19, VehicleBrandId = 7, Name = "Clio", IsActive = true },
                new VehicleModel { Id = 20, VehicleBrandId = 7, Name = "Megane", IsActive = true },
                new VehicleModel { Id = 21, VehicleBrandId = 7, Name = "Fluence", IsActive = true },

                new VehicleModel { Id = 22, VehicleBrandId = 8, Name = "Egea", IsActive = true },
                new VehicleModel { Id = 23, VehicleBrandId = 8, Name = "Linea", IsActive = true },
                new VehicleModel { Id = 24, VehicleBrandId = 8, Name = "Doblo", IsActive = true },

                new VehicleModel { Id = 25, VehicleBrandId = 9, Name = "i20", IsActive = true },
                new VehicleModel { Id = 26, VehicleBrandId = 9, Name = "i30", IsActive = true },
                new VehicleModel { Id = 27, VehicleBrandId = 9, Name = "Tucson", IsActive = true },

                new VehicleModel { Id = 28, VehicleBrandId = 10, Name = "208", IsActive = true },
                new VehicleModel { Id = 29, VehicleBrandId = 10, Name = "308", IsActive = true },
                new VehicleModel { Id = 30, VehicleBrandId = 10, Name = "2008", IsActive = true }
            );

            base.OnModelCreating(builder);

            
        }
    }
}
