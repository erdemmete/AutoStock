using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class OfficialInvoiceDocumentConfiguration : IEntityTypeConfiguration<OfficialInvoiceDocument>
    {
        public void Configure(EntityTypeBuilder<OfficialInvoiceDocument> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OfficialInvoiceNumber)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.EttnOrUuid)
                .HasMaxLength(100);

            builder.Property(x => x.OriginalFileName)
                .IsRequired()
                .HasMaxLength(260);

            builder.Property(x => x.StoredFileName)
                .IsRequired()
                .HasMaxLength(260);

            builder.Property(x => x.RelativePath)
                .IsRequired()
                .HasMaxLength(600);

            builder.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.UploadedByEmail)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Note)
                .HasMaxLength(1000);

            builder.Property(x => x.ShareToken)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.CustomerDeliveryChannel)
                .HasMaxLength(40);

            builder.HasIndex(x => new { x.WorkshopId, x.InvoiceId });

            builder.HasIndex(x => new { x.WorkshopId, x.OfficialInvoiceNumber });

            builder.HasIndex(x => x.ShareToken)
                .IsUnique();

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
