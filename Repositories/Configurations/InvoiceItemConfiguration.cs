using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
    {
        public void Configure(EntityTypeBuilder<InvoiceItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Unit)
                .HasMaxLength(20);

            builder.Property(x => x.Quantity)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.UnitPrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.DiscountRate)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.DiscountAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.VatRate)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.VatAmount)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.LineTotal)
                .HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.StockItem)
    .WithMany()
    .HasForeignKey(x => x.StockItemId)
    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}