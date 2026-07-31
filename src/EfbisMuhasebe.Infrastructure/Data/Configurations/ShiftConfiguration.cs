using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ShiftCode)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(x => x.ShiftCode)
            .IsUnique();

        builder.Property(x => x.OvertimeHours)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.StartTime)
            .IsRequired();

        builder.Property(x => x.EndTime)
            .IsRequired();

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.EmployeeId, x.ShiftDate });
        
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
