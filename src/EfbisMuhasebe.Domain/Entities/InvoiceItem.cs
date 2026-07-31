using EfbisMuhasebe.Domain.Common;

namespace EfbisMuhasebe.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; } // percentage 0,1,8,10,20
    public decimal VatAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    // Navigation
    public Invoice? Invoice { get; set; }
    public Product? Product { get; set; }
}
