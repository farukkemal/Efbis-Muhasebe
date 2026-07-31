namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Hızlı Satış / POS Kasa Ödeme DTO
/// </summary>
public class PosCheckoutDto
{
    public int? CustomerId { get; set; }
    public int CashAccountId { get; set; }
    public string PaymentType { get; set; } = "Nakit"; // Nakit, KrediKarti, YemekKarti, Parcali
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public string? Note { get; set; }
    public List<PosCartItemDto> Items { get; set; } = new();
}

public class PosCartItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int VatRate { get; set; } = 20;
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}

public class PosReceiptDto
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public string CustomerTitle { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal VatTotal { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public List<PosCartItemDto> Items { get; set; } = new();
}

public class PosZReportDto
{
    public DateTime Date { get; set; } = DateTime.Today;
    public int TotalSalesCount { get; set; }
    public decimal TotalCashSales { get; set; }
    public decimal TotalCreditCardSales { get; set; }
    public decimal TotalMealCardSales { get; set; }
    public decimal GrandTotalSales { get; set; }
}
