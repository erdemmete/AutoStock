using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class CurrentAccountTransactionConfiguration
        : IEntityTypeConfiguration<CurrentAccountTransaction>
    {
        public void Configure(EntityTypeBuilder<CurrentAccountTransaction> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Debit)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Credit)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.DocumentNumber)
                .HasMaxLength(100);

            builder.Property(x => x.TransactionDate)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.CurrentAccountTransactions)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Invoice)
                .WithMany()
                .HasForeignKey(x => x.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.WorkshopId, x.CustomerId, x.TransactionDate });

            builder.HasIndex(x => x.InvoiceId);
        }
    }
}