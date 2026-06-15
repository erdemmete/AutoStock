using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopEmailRecipientConfiguration : IEntityTypeConfiguration<WorkshopEmailRecipient>
    {
        public void Configure(EntityTypeBuilder<WorkshopEmailRecipient> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.RecipientType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.WorkshopId, x.RecipientType, x.Email })
                .IsUnique();

            builder.HasIndex(x => new { x.WorkshopId, x.RecipientType, x.IsActive });

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
