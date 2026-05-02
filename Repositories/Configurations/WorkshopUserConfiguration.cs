using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopUserConfiguration : IEntityTypeConfiguration<WorkshopUser>
    {
        public void Configure(EntityTypeBuilder<WorkshopUser> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Role)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasOne(x => x.Workshop)
                .WithMany(x => x.WorkshopUsers)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.WorkshopId, x.UserId })
                .IsUnique();
        }
    }
}