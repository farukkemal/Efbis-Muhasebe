using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class StockTransactionMappingProfile : Profile
{
    public StockTransactionMappingProfile()
    {
        CreateMap<StockTransaction, StockTransactionDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty))
            .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductCode : string.Empty))
            .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
            .ForMember(dest => dest.CustomerTitle, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Title : string.Empty))
            .ForMember(dest => dest.FormattedDate, opt => opt.MapFrom(src => src.TransactionDate.ToString("dd.MM.yyyy HH:mm")))
            .ForMember(dest => dest.TransactionTypeText, opt => opt.MapFrom(src => GetTransactionTypeText(src.TransactionType)));

        CreateMap<CreateStockTransactionDto, StockTransaction>();
    }

    private static string GetTransactionTypeText(TransactionType type)
    {
        return type switch
        {
            TransactionType.StockIn => "Stok Girişi",
            TransactionType.StockOut => "Stok Çıkışı",
            TransactionType.Transfer => "Depo Transferi",
            TransactionType.Count => "Sayım Farkı",
            TransactionType.Waste => "Fire",
            _ => type.ToString()
        };
    }
}
