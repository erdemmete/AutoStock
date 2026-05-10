using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations;

public class ServiceOperationConfiguration : IEntityTypeConfiguration<ServiceOperation>
{
    public void Configure(EntityTypeBuilder<ServiceOperation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Description)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Note)
            .HasMaxLength(1000);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2);

        builder.HasOne(x => x.ServiceRecord)
            .WithMany(x => x.Operations)
            .HasForeignKey(x => x.ServiceRecordId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ServiceRequestItem)
            .WithMany(x => x.Operations)
            .HasForeignKey(x => x.ServiceRequestItemId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}