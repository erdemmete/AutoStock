using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.InvoiceNumber)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.CustomerTitle)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.CustomerTaxOffice)
                .HasMaxLength(100);

            builder.Property(x => x.CustomerTaxNumber)
                .HasMaxLength(50);

            builder.Property(x => x.CustomerTckn)
                .HasMaxLength(11);

            builder.Property(x => x.CustomerAddress)
                .HasMaxLength(500);

            builder.Property(x => x.Plate)
                .HasMaxLength(20);

            builder.Property(x => x.ChassisNumber)
                .HasMaxLength(100);

            builder.Property(x => x.Subtotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.DiscountTotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.VatTotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.GrandTotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.ExternalInvoiceId)
                .HasMaxLength(100);

            builder.Property(x => x.ExternalInvoiceNumber)
                .HasMaxLength(100);

            builder.Property(x => x.ExternalUuid)
                .HasMaxLength(200);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Customer)
                .WithMany()
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ServiceRecord)
                .WithMany()
                .HasForeignKey(x => x.ServiceRecordId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.Items)
                .WithOne(x => x.Invoice)
                .HasForeignKey(x => x.InvoiceId);
        }
    }
}