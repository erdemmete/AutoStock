using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopBankAccountConfiguration : IEntityTypeConfiguration<WorkshopBankAccount>
    {
        public void Configure(EntityTypeBuilder<WorkshopBankAccount> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.BankName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.AccountHolder)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Iban)
                .IsRequired()
                .HasMaxLength(34);

            builder.Property(x => x.CurrencyCode)
                .IsRequired()
                .HasMaxLength(3)
                .HasDefaultValue("TRY");

            builder.Property(x => x.BranchName)
                .HasMaxLength(150);

            builder.Property(x => x.AccountNumber)
                .HasMaxLength(50);

            builder.Property(x => x.Description)
                .HasMaxLength(250);

            builder.Property(x => x.IsDefault)
                .HasDefaultValue(false);

            builder.Property(x => x.ShowOnInvoices)
                .HasDefaultValue(true);

            builder.Property(x => x.ShowOnServiceForms)
                .HasDefaultValue(false);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.SortOrder)
                .HasDefaultValue(0);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.WorkshopId);

            builder.HasIndex(x => new
            {
                x.WorkshopId,
                x.IsActive,
                x.ShowOnInvoices
            });

            builder.HasIndex(x => new
            {
                x.WorkshopId,
                x.IsDefault
            });
        }
    }
}