using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories
{
    public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
    {
        public void Configure(EntityTypeBuilder<Vehicle> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.PlateNumber)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.Brand)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.Model)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasOne(x => x.Customer)
                   .WithMany(x => x.Vehicles)
                   .HasForeignKey(x => x.CustomerId);

            builder.HasMany(x => x.ServiceRecords)
                   .WithOne(x => x.Vehicle)
                   .HasForeignKey(x => x.VehicleId);
        }
    }
}
