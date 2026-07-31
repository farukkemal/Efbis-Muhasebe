using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

/// <summary>
/// AutoMapper profili — Entity ↔ DTO dönüşümleri
/// </summary>
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        // Product → ProductDto
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.ProductTypeDisplay,
                opt => opt.MapFrom(src => GetProductTypeDisplay(src.ProductType)))
            .ForMember(dest => dest.UnitDisplay,
                opt => opt.MapFrom(src => GetUnitDisplay(src.Unit)))
            .ForMember(dest => dest.StockStatusDisplay,
                opt => opt.MapFrom(src => GetStockStatusDisplay(src.StockStatus)))
            .ForMember(dest => dest.StatusDisplay,
                opt => opt.MapFrom(src => src.Status == ProductStatus.Active ? "Aktif" : "Pasif"))
            .ForMember(dest => dest.ProfitMarginPercent,
                opt => opt.MapFrom(src => src.ProfitMarginPercent));

        // CreateProductDto → Product
        CreateMap<CreateProductDto, Product>()
            .ForMember(dest => dest.CurrentStock, opt => opt.MapFrom(src => src.InitialStock))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ProductStatus.Active))
            .ForMember(dest => dest.CreatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => false));

        // UpdateProductDto → Product
        CreateMap<UpdateProductDto, Product>()
            .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CurrentStock, opt => opt.Ignore())
            .ForMember(dest => dest.InitialStock, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedDate, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());

        // Product → UpdateProductDto (düzenleme formu için)
        CreateMap<Product, UpdateProductDto>();

        // Category → CategoryDto
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ParentName,
                opt => opt.MapFrom(src => src.Parent != null ? src.Parent.Name : null))
            .ForMember(dest => dest.ProductCount,
                opt => opt.MapFrom(src => src.Products != null ? src.Products.Count(p => !p.IsDeleted) : 0));

        // Product → SaleProductDto (Satışta Olan Ürünler ekranı)
        CreateMap<Product, SaleProductDto>()
            .ForMember(dest => dest.CategoryName,
                opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
            .ForMember(dest => dest.ProductTypeDisplay,
                opt => opt.MapFrom(src => GetProductTypeDisplay(src.ProductType)))
            .ForMember(dest => dest.UnitDisplay,
                opt => opt.MapFrom(src => GetUnitDisplay(src.Unit)))
            .ForMember(dest => dest.StockStatusDisplay,
                opt => opt.MapFrom(src => GetStockStatusDisplay(src.StockStatus)))
            .ForMember(dest => dest.StatusDisplay,
                opt => opt.MapFrom(src => src.Status == ProductStatus.Active ? "Aktif" : "Pasif"))
            .ForMember(dest => dest.SalePriceWithVat,
                opt => opt.MapFrom(src => src.SalePriceWithVat))
            .ForMember(dest => dest.ProfitMarginPercent,
                opt => opt.MapFrom(src => src.ProfitMarginPercent));
    }

    private static string GetProductTypeDisplay(ProductType type) => type switch
    {
        ProductType.StockedProduct => "Stoklu Ürün",
        ProductType.Service => "Hizmet",
        ProductType.RawMaterial => "Hammadde",
        ProductType.FinishedProduct => "Mamul",
        ProductType.SemiFinished => "Yarı Mamul",
        _ => type.ToString()
    };

    private static string GetUnitDisplay(Unit unit) => unit switch
    {
        Unit.Piece => "Adet",
        Unit.Kg => "Kg",
        Unit.Liter => "Litre",
        Unit.Package => "Paket",
        Unit.Box => "Koli",
        Unit.Meter => "Metre",
        Unit.Pair => "Çift",
        _ => unit.ToString()
    };

    private static string GetStockStatusDisplay(StockStatus status) => status switch
    {
        StockStatus.Sufficient => "Yeterli",
        StockStatus.Low => "Az Stok",
        StockStatus.Critical => "Kritik Stok",
        StockStatus.OutOfStock => "Stok Yok",
        _ => status.ToString()
    };
}
