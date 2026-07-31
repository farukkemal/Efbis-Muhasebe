using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CustomerCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.AuthorizedPerson)
            .HasMaxLength(100);

        builder.Property(c => c.TaxOffice)
            .HasMaxLength(100);

        builder.Property(c => c.TaxNumber)
            .HasMaxLength(50);

        builder.Property(c => c.Phone)
            .HasMaxLength(30);

        builder.Property(c => c.Email)
            .HasMaxLength(100);

        builder.Property(c => c.Gsm)
            .HasMaxLength(30);

        builder.Property(c => c.City)
            .HasMaxLength(50);

        builder.Property(c => c.District)
            .HasMaxLength(50);

        builder.Property(c => c.Address)
            .HasMaxLength(500);

        builder.Property(c => c.Balance)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(c => c.RiskLimit)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(c => c.CustomerType)
            .HasConversion<int>();

        builder.Property(c => c.Status)
            .HasConversion<int>();

        // Indexes
        builder.HasIndex(c => c.CustomerCode)
            .IsUnique()
            .HasDatabaseName("IX_Customers_CustomerCode");

        builder.HasIndex(c => c.Title)
            .HasDatabaseName("IX_Customers_Title");

        builder.HasIndex(c => c.CustomerType)
            .HasDatabaseName("IX_Customers_CustomerType");

        // Computed properties ignored from EF mapping
        builder.Ignore(c => c.BalanceStatus);
    }
}
