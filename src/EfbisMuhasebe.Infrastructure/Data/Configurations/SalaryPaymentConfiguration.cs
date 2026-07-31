using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class SalaryPaymentConfiguration : IEntityTypeConfiguration<SalaryPayment>
{
    public void Configure(EntityTypeBuilder<SalaryPayment> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.PaymentCode)
            .IsRequired()
            .HasMaxLength(30);

        builder.HasIndex(x => x.PaymentCode)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EmployeeId, x.Year, x.Month })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.Property(x => x.GrossSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SgkDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherDeductions).HasColumnType("decimal(18,2)");
        builder.Property(x => x.BonusAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalPayment).HasColumnType("decimal(18,2)");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CashAccount)
            .WithMany()
            .HasForeignKey(x => x.CashAccountId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
