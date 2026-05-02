using AutoStock.Repositories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoStock.Repositories.Configurations
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FullName)
             .IsRequired()
             .HasMaxLength(100);

            builder.Property(x => x.PhoneNumber)
                   .HasMaxLength(20);

            builder.Property(x => x.Role)
                   .HasMaxLength(50);

            builder.Property(x => x.CreatedAt)
                   .HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
