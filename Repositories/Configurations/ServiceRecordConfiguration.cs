using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations;

public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
{
    public void Configure(EntityTypeBuilder<ServiceRecord> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordNumber)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(x => x.RecordNumber)
            .IsUnique();

        builder.Property(x => x.CustomerNameSnapshot)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.CustomerPhoneSnapshot)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.VehiclePlateSnapshot)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.VehicleBrandNameSnapshot)
            .HasMaxLength(100);

        builder.Property(x => x.VehicleModelNameSnapshot)
            .HasMaxLength(100);

        builder.Property(x => x.CustomerComplaint)
            .HasMaxLength(1000);

        builder.Property(x => x.ServiceReceptionNote)
            .HasMaxLength(1000);

        builder.Property(x => x.RepairNote)
            .HasMaxLength(2000);

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.Customer)
            .WithMany(x => x.ServiceRecords)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany(x => x.ServiceRecords)
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);


    }
}