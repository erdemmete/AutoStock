using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations;

public class VehicleQrCodeConfiguration : IEntityTypeConfiguration<VehicleQrCode>
{
    public void Configure(EntityTypeBuilder<VehicleQrCode> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Status)
            .HasDefaultValue(VehicleQrCodeStatus.Available)
            .HasConversion<int>();

        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.HasIndex(x => x.VehicleId)
            .IsUnique()
            .HasFilter("[Status] = 2 AND [VehicleId] IS NOT NULL");

        builder.HasOne(x => x.Workshop)
            .WithMany()
            .HasForeignKey(x => x.WorkshopId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Vehicle)
            .WithMany()
            .HasForeignKey(x => x.VehicleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_VehicleQrCodes_Status_Valid",
                "[Status] IN (1, 2, 3, 4)");

            table.HasCheckConstraint(
                "CK_VehicleQrCodes_Assigned_State",
                "([Status] <> 2 OR ([WorkshopId] IS NOT NULL AND [VehicleId] IS NOT NULL AND [AssignedAt] IS NOT NULL))");

            table.HasCheckConstraint(
                "CK_VehicleQrCodes_Available_State",
                "([Status] <> 1 OR ([VehicleId] IS NULL AND [AssignedAt] IS NULL AND [RetiredAt] IS NULL))");

            table.HasCheckConstraint(
                "CK_VehicleQrCodes_Retired_State",
                "([Status] <> 3 OR [RetiredAt] IS NOT NULL)");
        });
    }
}
