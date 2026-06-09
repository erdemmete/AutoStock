using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class UserSecurityTokenConfiguration : IEntityTypeConfiguration<UserSecurityToken>
    {
        public void Configure(EntityTypeBuilder<UserSecurityToken> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(x => x.CodeHash)
    .HasMaxLength(128);

            builder.HasIndex(x => x.CodeHash);

            builder.Property(x => x.Purpose)
                .IsRequired();

            builder.Property(x => x.DeliveryChannel)
                .IsRequired();

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.ConsumedIpAddress)
                .HasMaxLength(64);

            builder.Property(x => x.ConsumedUserAgent)
                .HasMaxLength(512);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.UserId,
                x.Purpose,
                x.UsedAt,
                x.RevokedAt,
                x.ExpiresAt
            });
        }
    }
}