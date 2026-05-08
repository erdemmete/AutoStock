using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations;

public class ServiceRequestItemConfiguration : IEntityTypeConfiguration<ServiceRequestItem>
{
    public void Configure(EntityTypeBuilder<ServiceRequestItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.RepairDetail)
            .HasMaxLength(2000);

        builder.HasOne(x => x.ServiceRecord)
            .WithMany(x => x.RequestItems)
            .HasForeignKey(x => x.ServiceRecordId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.EstimatedAmount)
    .HasPrecision(18, 2);

        builder.Property(x => x.FinalAmount)
            .HasPrecision(18, 2);
    }
}