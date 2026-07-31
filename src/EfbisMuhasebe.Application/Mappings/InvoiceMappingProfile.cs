using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Mappings;

public class InvoiceMappingProfile : Profile
{
    public InvoiceMappingProfile()
    {
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.CustomerTitle, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Title : string.Empty))
            .ForMember(dest => dest.InvoiceTypeText, opt => opt.MapFrom(src => GetInvoiceTypeText(src.InvoiceType)))
            .ForMember(dest => dest.FormattedDate, opt => opt.MapFrom(src => src.InvoiceDate.ToString("dd.MM.yyyy")))
            .ForMember(dest => dest.StatusText, opt => opt.MapFrom(src => GetStatusText(src.Status)))
            .ForMember(dest => dest.StatusBadgeClass, opt => opt.MapFrom(src => GetStatusBadgeClass(src.Status)))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<Invoice, InvoiceDetailDto>()
            .IncludeBase<Invoice, InvoiceDto>();

        CreateMap<InvoiceItem, InvoiceItemDto>()
            .ForMember(dest => dest.ProductCode, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductCode : string.Empty))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : string.Empty));
    }

    private string GetInvoiceTypeText(InvoiceType type) => type switch
    {
        InvoiceType.Sales => "Satış Faturası",
        InvoiceType.Purchase => "Alış Faturası",
        _ => type.ToString()
    };

    private string GetStatusText(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Taslak",
        InvoiceStatus.Approved => "Onaylı",
        InvoiceStatus.Cancelled => "İptal",
        InvoiceStatus.Paid => "Ödendi",
        _ => status.ToString()
    };

    private string GetStatusBadgeClass(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "bg-secondary",
        InvoiceStatus.Approved => "bg-success",
        InvoiceStatus.Cancelled => "bg-danger",
        InvoiceStatus.Paid => "bg-primary",
        _ => "bg-light"
    };
}
