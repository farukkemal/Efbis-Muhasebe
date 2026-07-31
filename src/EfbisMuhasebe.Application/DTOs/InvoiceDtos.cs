using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public string InvoiceTypeText { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerTitle { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public InvoiceStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string Scenario { get; set; } = "TICARI";
    public string? EFaturaUuid { get; set; }
    public decimal WithholdingRate { get; set; }
    public decimal WithholdingTotal { get; set; }
}

public class InvoiceDetailDto : InvoiceDto
{
    public string? Description { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class CreateInvoiceDto
{
    public string? InvoiceNumber { get; set; }
    public InvoiceType InvoiceType { get; set; }
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public DateTime? DueDate { get; set; }
    public string? Description { get; set; }
    public string Scenario { get; set; } = "TICARI";
    public decimal WithholdingRate { get; set; } = 0;
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

public class CreateInvoiceItemDto
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; }
    public decimal DiscountRate { get; set; }
}

public class UpdateInvoiceStatusDto
{
    public InvoiceStatus Status { get; set; }
}

public class InvoiceFilterDto
{
    public string? SearchTerm { get; set; }
    public InvoiceType? InvoiceType { get; set; }
    public InvoiceStatus? Status { get; set; }
    public int? CustomerId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}

public record InvoiceDashboardDto(int TotalSalesCount, int TotalPurchaseCount, decimal TotalSalesAmount, decimal TotalPurchaseAmount, int DraftCount, int OverdueCount);
