using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class IncomeExpenseConfiguration : IEntityTypeConfiguration<IncomeExpense>
{
    public void Configure(EntityTypeBuilder<IncomeExpense> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TransactionCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        
        builder.HasOne(e => e.CashAccount)
               .WithMany()
               .HasForeignKey(e => e.CashAccountId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
