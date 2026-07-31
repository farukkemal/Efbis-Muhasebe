using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class CashTransactionConfiguration : IEntityTypeConfiguration<CashTransaction>
{
    public void Configure(EntityTypeBuilder<CashTransaction> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TransactionCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.Amount).HasColumnType("decimal(18,2)");
        
        builder.HasOne(e => e.CashAccount)
               .WithMany(c => c.Transactions)
               .HasForeignKey(e => e.CashAccountId)
               .OnDelete(DeleteBehavior.Restrict);
               
        // If Customer exists
        // builder.HasOne(e => e.Customer).WithMany().HasForeignKey(e => e.CustomerId).OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
