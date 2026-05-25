using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class WorkshopPartnerConfiguration : IEntityTypeConfiguration<WorkshopPartner>
    {
        public void Configure(EntityTypeBuilder<WorkshopPartner> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Workshop)
                .WithMany(x => x.Partners)
                .HasForeignKey(x => x.WorkshopId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.FullName)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Title).HasMaxLength(100);
            builder.Property(x => x.PhoneNumber).HasMaxLength(30);
            builder.Property(x => x.Email).HasMaxLength(150);
            builder.Property(x => x.Note).HasMaxLength(500);
        }
    }
}