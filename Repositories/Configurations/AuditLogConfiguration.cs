using AutoStock.Repositories.Constants;
using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserFullName)
                .HasMaxLength(150);

            builder.Property(x => x.UserRole)
                .HasMaxLength(50);

            builder.Property(x => x.Description)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.OldValuesJson)
                .HasMaxLength(4000);

            builder.Property(x => x.NewValuesJson)
                .HasMaxLength(4000);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(500);

            builder.Property(x => x.CreatedAt)
    .HasDefaultValueSql(SqlDateTimeDefaults.TurkeyNow);

            builder.HasIndex(x => new { x.WorkshopId, x.CreatedAt });

            builder.HasIndex(x => new { x.UserId, x.CreatedAt });

            builder.HasIndex(x => new { x.EntityType, x.EntityId });

            builder.HasIndex(x => new { x.ActionType, x.CreatedAt });
        }
    }
}