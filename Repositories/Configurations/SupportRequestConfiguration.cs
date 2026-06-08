using AutoStock.Repositories.Constants;
using AutoStock.Repositories.Entities;
using AutoStock.Repositories.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class SupportRequestConfiguration : IEntityTypeConfiguration<SupportRequest>
    {
        public void Configure(EntityTypeBuilder<SupportRequest> builder)
        {
            builder.ToTable("SupportRequests");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.RequestType)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasDefaultValue(SupportRequestStatus.Open);

            builder.Property(x => x.Priority)
                .IsRequired()
                .HasDefaultValue(SupportRequestPriority.Normal);

            builder.Property(x => x.Subject)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.RequestedUserFullName)
                .HasMaxLength(150);

            builder.Property(x => x.RequestedUserPhone)
                .HasMaxLength(30);

            builder.Property(x => x.RequestedUserEmail)
                .HasMaxLength(150);

            builder.Property(x => x.AdminResponse)
                .HasMaxLength(4000);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql(SqlDateTimeDefaults.TurkeyNow);

            builder.Property(x => x.UpdatedAt);

            builder.Property(x => x.RespondedAt);

            builder.Property(x => x.ClosedAt);

            builder.HasOne(x => x.Workshop)
                .WithMany()
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany()
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.RespondedByUser)
                .WithMany()
                .HasForeignKey(x => x.RespondedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.WorkshopId);

            builder.HasIndex(x => x.CreatedByUserId);

            builder.HasIndex(x => x.RespondedByUserId);

            builder.HasIndex(x => x.Status);

            builder.HasIndex(x => x.RequestType);

            builder.HasIndex(x => x.CreatedAt);

            builder.HasIndex(x => new
            {
                x.WorkshopId,
                x.Status,
                x.RequestType
            });
        }
    }
}