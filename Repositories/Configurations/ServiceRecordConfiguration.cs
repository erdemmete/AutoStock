using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoStock.Repositories.Configurations
{
    public class ServiceRecordConfiguration : IEntityTypeConfiguration<ServiceRecord>
    {
        public void Configure(EntityTypeBuilder<ServiceRecord> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Complaint)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.Property(x => x.Diagnosis)
                   .HasMaxLength(500);

            builder.Property(x => x.Notes)
                   .HasMaxLength(500);

            builder.HasOne(x => x.Vehicle)
                   .WithMany(x => x.ServiceRecords)
                   .HasForeignKey(x => x.VehicleId);

            builder.HasOne(x => x.Employee)
                   .WithMany(x => x.ServiceRecords)
                   .HasForeignKey(x => x.EmployeeId);
        }
    }
}
