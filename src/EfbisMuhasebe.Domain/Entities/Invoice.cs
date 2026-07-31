using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public int CustomerId { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Description { get; set; }

    public string Scenario { get; set; } = "TICARI"; // TICARI, TEMEL, EARSIV
    public string? EFaturaUuid { get; set; }
    public string? WithholdingCode { get; set; }
    public decimal WithholdingRate { get; set; } = 0; // 0, 2 (2/10), 5 (5/10), 9 (9/10)
    public decimal WithholdingTotal { get; set; } = 0;

    // Navigation
    public Customer? Customer { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
