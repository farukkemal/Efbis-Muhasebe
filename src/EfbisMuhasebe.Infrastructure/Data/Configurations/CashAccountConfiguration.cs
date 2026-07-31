using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class CashAccountConfiguration : IEntityTypeConfiguration<CashAccount>
{
    public void Configure(EntityTypeBuilder<CashAccount> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.AccountCode).IsRequired().HasMaxLength(50);
        builder.Property(e => e.AccountName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.Balance).HasColumnType("decimal(18,2)");
        builder.Property(e => e.Currency).HasMaxLength(10);
        
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
