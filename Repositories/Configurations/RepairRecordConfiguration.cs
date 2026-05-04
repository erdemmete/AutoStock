using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class RepairRecordConfiguration : IEntityTypeConfiguration<RepairRecord>
    {
        public void Configure(EntityTypeBuilder<RepairRecord> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RepairDescription)
                   .IsRequired()
                   .HasMaxLength(300);

            builder.HasOne(x => x.ServiceRecord)
                   .WithMany(x => x.RepairRecords)
                   .HasForeignKey(x => x.ServiceRecordId);
            builder.Property(x => x.LaborCost)
                    .HasPrecision(18, 2);

            builder.Property(x => x.PartCost)
                    .HasPrecision(18, 2);
        }
    }
}
