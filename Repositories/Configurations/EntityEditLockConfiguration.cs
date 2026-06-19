using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class EntityEditLockConfiguration : IEntityTypeConfiguration<EntityEditLock>
    {
        public void Configure(EntityTypeBuilder<EntityEditLock> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EntityType)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.LockToken)
                .IsRequired()
                .HasMaxLength(128);

            builder.HasIndex(x => new { x.WorkshopId, x.EntityType, x.EntityId })
                .IsUnique();

            builder.HasIndex(x => new { x.LockedByUserId, x.ExpiresAt });

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LockedByUser)
                .WithMany()
                .HasForeignKey(x => x.LockedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
