using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopConfiguration : IEntityTypeConfiguration<Workshop>
    {
        public void Configure(EntityTypeBuilder<Workshop> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}