using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class VehicleVariantConfiguration : IEntityTypeConfiguration<VehicleVariant>
    {
        public void Configure(EntityTypeBuilder<VehicleVariant> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.FuelType)
                .HasMaxLength(50);

            builder.Property(x => x.TransmissionType)
                .HasMaxLength(50);

            builder.Property(x => x.BodyType)
                .HasMaxLength(50);

            builder.Property(x => x.EngineCode)
                .HasMaxLength(80);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.HasOne(x => x.VehicleBrand)
                .WithMany()
                .HasForeignKey(x => x.VehicleBrandId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.VehicleModel)
                .WithMany()
                .HasForeignKey(x => x.VehicleModelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.VehicleBrandId);

            builder.HasIndex(x => x.VehicleModelId);

            builder.HasIndex(x => new
            {
                x.VehicleModelId,
                x.Name
            });
        }
    }
}
