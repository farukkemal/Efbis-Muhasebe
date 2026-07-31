using System;
using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class StockTransaction : BaseEntity
{
    public string TransactionCode { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public int? CustomerId { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? ReferenceNo { get; set; }

    // Navigation
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
    public Customer? Customer { get; set; }
}
