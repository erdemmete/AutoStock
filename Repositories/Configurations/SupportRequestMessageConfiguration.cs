using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class SupportRequestMessageConfiguration : IEntityTypeConfiguration<SupportRequestMessage>
    {
        public void Configure(EntityTypeBuilder<SupportRequestMessage> builder)
        {
            builder.ToTable("SupportRequestMessages");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(4000);

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => new { x.SupportRequestId, x.CreatedAt });

            builder.HasOne(x => x.SupportRequest)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.SupportRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.SenderUser)
                .WithMany()
                .HasForeignKey(x => x.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
