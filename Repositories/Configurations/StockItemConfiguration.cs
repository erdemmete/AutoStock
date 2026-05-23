using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class StockItemConfiguration : IEntityTypeConfiguration<StockItem>
    {
        public void Configure(EntityTypeBuilder<StockItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Code)
                .HasMaxLength(50);

            builder.Property(x => x.Barcode)
                .HasMaxLength(100);

            builder.Property(x => x.Brand)
                .HasMaxLength(100);

            builder.Property(x => x.Unit)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Quantity)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.PurchasePrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.SalePrice)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.MinimumQuantity)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasMany(x => x.Movements)
                .WithOne(x => x.StockItem)
                .HasForeignKey(x => x.StockItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.WorkshopId, x.Name });
            builder.HasIndex(x => new { x.WorkshopId, x.Code });
            builder.HasIndex(x => new { x.WorkshopId, x.Barcode });
        }
    }
}