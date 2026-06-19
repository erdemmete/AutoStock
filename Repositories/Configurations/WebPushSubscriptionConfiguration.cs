using AutoStock.Repositories.Constants;
using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WebPushSubscriptionConfiguration : IEntityTypeConfiguration<WebPushSubscription>
    {
        public void Configure(EntityTypeBuilder<WebPushSubscription> builder)
        {
            builder.ToTable("WebPushSubscriptions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Endpoint)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(x => x.P256dh)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.Auth)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql(SqlDateTimeDefaults.TurkeyNow);

            builder.Property(x => x.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql(SqlDateTimeDefaults.TurkeyNow);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Endpoint).IsUnique();
            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => new { x.UserId, x.IsActive });
            builder.HasIndex(x => new { x.WorkshopId, x.IsActive });
        }
    }
}
