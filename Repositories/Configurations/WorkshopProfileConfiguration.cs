using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopProfileConfiguration : IEntityTypeConfiguration<WorkshopProfile>
    {
        public void Configure(EntityTypeBuilder<WorkshopProfile> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.WorkshopId)
                .IsUnique();

            builder.HasOne(x => x.Workshop)
                .WithOne(x => x.Profile)
                .HasForeignKey<WorkshopProfile>(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.DisplayName).HasMaxLength(200);
            builder.Property(x => x.LegalTitle).HasMaxLength(300);
            builder.Property(x => x.TaxOffice).HasMaxLength(100);
            builder.Property(x => x.TaxNumber).HasMaxLength(20);
            builder.Property(x => x.TradeRegistryNumber).HasMaxLength(50);
            builder.Property(x => x.MersisNumber).HasMaxLength(50);

            builder.Property(x => x.Email).HasMaxLength(150);
            builder.Property(x => x.PhoneNumber).HasMaxLength(30);
            builder.Property(x => x.FaxNumber).HasMaxLength(30);
            builder.Property(x => x.Website).HasMaxLength(150);

            builder.Property(x => x.AddressLine).HasMaxLength(500);
            builder.Property(x => x.City).HasMaxLength(100);
            builder.Property(x => x.District).HasMaxLength(100);
            builder.Property(x => x.PostalCode).HasMaxLength(20);
            builder.Property(x => x.Country).HasMaxLength(100);
        }
    }
}