using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations;

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Plate)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.VinNumber)
            .HasMaxLength(50);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.Vehicles)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleBrand)
            .WithMany()
            .HasForeignKey(x => x.VehicleBrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.VehicleModel)
            .WithMany()
            .HasForeignKey(x => x.VehicleModelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}