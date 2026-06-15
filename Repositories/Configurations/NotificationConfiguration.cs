using AutoStock.Repositories.Constants;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.ToTable("Notifications");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Type)
                .IsRequired()
                .HasDefaultValue(NotificationType.General);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(180);

            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.RelatedEntityType)
                .IsRequired()
                .HasDefaultValue(NotificationRelatedEntityType.None);

            builder.Property(x => x.ActionUrl)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql(SqlDateTimeDefaults.TurkeyNow);

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.WorkshopId);
            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.CreatedAt);
            builder.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId });
        }
    }
}
