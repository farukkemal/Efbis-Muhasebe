using EfbisMuhasebe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EfbisMuhasebe.Infrastructure.Data.Configurations;

/// <summary>Product entity Fluent API konfigürasyonu</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ProductCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.ProductCode)
            .IsUnique();

        builder.Property(p => p.Barcode)
            .HasMaxLength(100);

        builder.HasIndex(p => p.Barcode)
            .IsUnique()
            .HasFilter("[Barcode] IS NOT NULL"); // NULL barkodlara unique uygulanmaz

        builder.Property(p => p.PurchasePrice)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.SalePrice)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.SpecialTaxValue)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.CommunicationTaxRate)
            .HasColumnType("decimal(5,2)");

        builder.Property(p => p.InitialStock)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.CurrentStock)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.MinimumStock)
            .HasColumnType("decimal(18,4)");

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.ProductType)
            .HasConversion<int>();

        builder.Property(p => p.Unit)
            .HasConversion<int>();

        builder.Property(p => p.PurchaseVatRate)
            .HasConversion<int>();

        builder.Property(p => p.SaleVatRate)
            .HasConversion<int>();

        builder.Property(p => p.DiscountType)
            .HasConversion<int>();

        builder.Property(p => p.SpecialTaxType)
            .HasConversion<int>();

        builder.Property(p => p.Status)
            .HasConversion<int>();

        // İlişki
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Hesaplanan kolonlar — veritabanında saklanmaz, domain'de hesaplanır
        builder.Ignore(p => p.StockStatus);
        builder.Ignore(p => p.ProfitMarginPercent);
        builder.Ignore(p => p.PurchasePriceWithVat);
        builder.Ignore(p => p.SalePriceWithVat);

        // Satış yönetimi alanları
        builder.Property(p => p.IsAvailableForSale)
            .HasDefaultValue(true);

        builder.Property(p => p.SaleStatusUpdatedBy)
            .HasMaxLength(100);

        // Satış durumu index (Satışta Olan Ürünler sorguları için hızlandırır)
        builder.HasIndex(p => p.IsAvailableForSale)
            .HasDatabaseName("IX_Products_IsAvailableForSale");

        builder.HasIndex(p => new { p.IsAvailableForSale, p.Status })
            .HasDatabaseName("IX_Products_SaleStatus_Status");
    }
}
