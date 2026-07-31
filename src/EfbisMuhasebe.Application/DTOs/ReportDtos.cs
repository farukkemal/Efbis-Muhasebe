namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Genel Dashboard Rapor DTO
/// </summary>
public class ReportDashboardDto
{
    // Ürün
    public int TotalProducts { get; set; }
    public int ActiveProducts { get; set; }
    public int SalesProducts { get; set; }
    public int CriticalStockCount { get; set; }
    public int OutOfStockCount { get; set; }

    // Kategori
    public int TotalCategories { get; set; }

    // Cari
    public int TotalCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public decimal TotalReceivables { get; set; }  // Toplam Alacak
    public decimal TotalPayables { get; set; }     // Toplam Borç

    // Stok Değeri
    public decimal TotalStockValue { get; set; }    // Toplam stok × alış fiyatı
    public decimal TotalSaleValue { get; set; }     // Toplam stok × satış fiyatı
    public decimal PotentialProfit { get; set; }    // Potansiyel Kâr
}

/// <summary>
/// Stok Değer Raporu DTO
/// </summary>
public class StockValueReportDto
{
    public decimal TotalPurchaseValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal PotentialProfit { get; set; }
    public int TotalItems { get; set; }
    public List<StockValueItemDto> Items { get; set; } = new();
}

public class StockValueItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal CurrentStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal StockPurchaseValue { get; set; }  // CurrentStock × PurchasePrice
    public decimal StockSaleValue { get; set; }      // CurrentStock × SalePrice
    public decimal Profit { get; set; }              // Fark
}

/// <summary>
/// Cari Bakiye Raporu
/// </summary>
public class CustomerBalanceReportItemDto
{
    public int CustomerId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string CustomerType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string BalanceStatus { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? City { get; set; }
}

/// <summary>
/// En Çok Satılan Ürünler (Fatura kalemlerinden)
/// </summary>
public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public int InvoiceCount { get; set; }
}

/// <summary>
/// Kritik Stok Raporu
/// </summary>
public class CriticalStockReportItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal Deficit { get; set; }  // MinimumStock - CurrentStock
    public string StockStatus { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class StockDistributionReportDto
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Values { get; set; } = new();
    public List<string> Colors { get; set; } = new();
}

/// <summary>
/// KDV Özet & Beyanname Ön Analiz DTO (Hesaplanan 391 KDV, İndirilecek 191 KDV)
/// </summary>
public class VatReportDto
{
    public decimal TotalSalesNet { get; set; }        // 600 Net Satış Matrahı
    public decimal CalculatedVat { get; set; }        // 391 Hesaplanan KDV
    public decimal TotalPurchaseNet { get; set; }     // 153/770 Net Alış Matrahı
    public decimal DeductibleVat { get; set; }        // 191 İndirilecek KDV
    public decimal NetVatPayable { get; set; }        // Ödenecek KDV (pozitif ise)
    public decimal NetVatCarriedForward { get; set; } // Devreden KDV (negatif ise)
    public List<VatRateBreakdownDto> RateBreakdown { get; set; } = new();
}

public class VatRateBreakdownDto
{
    public int VatRate { get; set; }              // 1, 10, 20
    public decimal SalesTaxableBase { get; set; }   // Satış Matrahı
    public decimal SalesVatAmount { get; set; }     // Satış KDV
    public decimal PurchaseTaxableBase { get; set; }// Alış Matrahı
    public decimal PurchaseVatAmount { get; set; }  // Alış KDV
}

/// <summary>
/// Gelir Tablosu DTO (Tek Düzen Hesap Planı 6'lı Hesap Grubu)
/// </summary>
public class IncomeStatementReportDto
{
    public decimal GrossSales { get; set; }          // 600 Brüt Satışlar
    public decimal SalesDiscounts { get; set; }       // 610 Satış İndirimleri
    public decimal NetSales { get; set; }             // 601 Net Satışlar
    public decimal CostOfGoodsSold { get; set; }      // 621 Satılan Malın Maliyeti (SMM)
    public decimal GrossProfit { get; set; }          // Brüt Satış Kârı
    public decimal OperatingExpenses { get; set; }    // 770 Genel Yönetim & Faaliyet Giderleri
    public decimal NetOperatingProfit { get; set; }   // Net Faaliyet Kârı / Zararı
}

/// <summary>
/// Cari Hesap Ekstresi & Borç/Alacak Yürüyen Bakiye DTO
/// </summary>
public class CustomerLedgerStatementDto
{
    public int CustomerId { get; set; }
    public string CustomerTitle { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }    // Toplam Borç
    public decimal TotalCredit { get; set; }   // Toplam Alacak
    public decimal FinalBalance { get; set; }  // Net Bakiye
    public List<CustomerLedgerItemDto> Transactions { get; set; } = new();
}

public class CustomerLedgerItemDto
{
    public DateTime Date { get; set; }
    public string DocumentNo { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }         // Borç
    public decimal Credit { get; set; }        // Alacak
    public decimal RunningBalance { get; set; }// Yürüyen Bakiye
}

/// <summary>
/// Vergi & SGK Ödeme Takvimi DTO
/// </summary>
public class TaxCalendarItemDto
{
    public string TaxType { get; set; } = string.Empty; // KDV1, Muhtasar, GeciciVergi, SGK, DamgaVergisi
    public string Period { get; set; } = string.Empty;  // Örn: Temmuz 2026
    public DateTime DueDate { get; set; }
    public decimal EstimatedAmount { get; set; }
    public string Status { get; set; } = "Bekliyor"; // Bekliyor, Ödendi, Gecikmede
    public int DaysRemaining { get; set; }
}

/// <summary>
/// KDV Ödeme & Aylık Beyanname Takip DTO
/// </summary>
public class VatPaymentTrackingDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal CalculatedVat { get; set; }   // 391
    public decimal DeductibleVat { get; set; }   // 191
    public decimal NetVatAmount { get; set; }   // Fark
    public bool IsPayable { get; set; }         // Pozitif ise ödenecek, negatif ise devreden
    public DateTime DueDate { get; set; }       // Ayın 28'i
    public string Status { get; set; } = "Ödeme Bekliyor"; // Ödendi, Ödeme Bekliyor, Devreden KDV
}

/// <summary>
/// Tek Düzen Hesap Planı (TDHP) Genel Geçici Mizan DTO
/// </summary>
public class TrialBalanceReportDto
{
    public decimal TotalDebit { get; set; }        // Toplam Borç Tutarı
    public decimal TotalCredit { get; set; }       // Toplam Alacak Tutarı
    public decimal TotalDebitBalance { get; set; } // Toplam Borç Bakiye
    public decimal TotalCreditBalance { get; set; }// Toplam Alacak Bakiye
    public bool IsBalanced => Math.Abs(TotalDebit - TotalCredit) < 0.01m; // Mizan Denkliği
    public List<TrialBalanceItemDto> AccountRows { get; set; } = new();
}

public class TrialBalanceItemDto
{
    public string AccountCode { get; set; } = string.Empty; // 100, 102, 120, 153, 191, 320, 391, 600, 621, 770
    public string AccountName { get; set; } = string.Empty;
    public decimal Debit { get; set; }         // Borç Hareketi
    public decimal Credit { get; set; }        // Alacak Hareketi
    public decimal DebitBalance { get; set; }  // Borç Bakiyesi
    public decimal CreditBalance { get; set; } // Alacak Bakiyesi
}

/// <summary>
/// Resmi Özet Bilanço DTO (Aktif / Pasif Dengesi)
/// </summary>
public class BalanceSheetReportDto
{
    // AKTİF (VARLIKLAR)
    public decimal LiquidAssets { get; set; }        // 100/102 Kasa ve Bankalar
    public decimal TradeReceivables { get; set; }     // 120 Müşteri Alacakları
    public decimal Inventories { get; set; }         // 153 Stok Değeri
    public decimal VatCarriedForward { get; set; }    // 191 Devreden KDV
    public decimal TotalCurrentAssets => LiquidAssets + TradeReceivables + Inventories + VatCarriedForward; // Dönen Varlıklar Toplamı

    // PASİF (KAYNAKLAR)
    public decimal TradePayables { get; set; }        // 320 Tedarikçi Borçları
    public decimal VatPayable { get; set; }           // 391 Ödenecek KDV
    public decimal PersonnelPayables { get; set; }    // 335 Personel Maaş Borçları
    public decimal TotalShortTermLiabilities => TradePayables + VatPayable + PersonnelPayables; // Kısa Vadeli Yabancı Kaynaklar

    public decimal EquityCapital { get; set; }        // 500 Sermaye / Öz Kaynak
    public decimal NetPeriodProfit { get; set; }      // 590 Net Dönem Kârı / Zararı
    public decimal TotalEquity => EquityCapital + NetPeriodProfit;

    public decimal TotalPassives => TotalShortTermLiabilities + TotalEquity;
    public bool IsBalanced => Math.Abs(TotalCurrentAssets - TotalPassives) < 0.01m;
}
