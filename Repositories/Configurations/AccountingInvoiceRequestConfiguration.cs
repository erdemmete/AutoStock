using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class AccountingInvoiceRequestConfiguration : IEntityTypeConfiguration<AccountingInvoiceRequest>
    {
        public void Configure(EntityTypeBuilder<AccountingInvoiceRequest> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.AccountantEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Message)
                .HasMaxLength(1000);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.SentAt)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.Token)
                .IsUnique();

            builder.HasIndex(x => new { x.WorkshopId, x.InvoiceId, x.Status });

            builder.HasIndex(x => new { x.WorkshopId, x.AccountantEmail, x.SentAt });

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.OfficialInvoiceDocuments)
                .WithOne(x => x.AccountingInvoiceRequest)
                .HasForeignKey(x => x.AccountingInvoiceRequestId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
