using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

/// <summary>
/// Rapor servisi arayüzü — tüm modüllerden veri çekerek analiz ve özet raporlar sunar.
/// </summary>
public interface IReportService
{
    Task<ReportDashboardDto> GetDashboardReportAsync();
    Task<StockValueReportDto> GetStockValueReportAsync();
    Task<List<CustomerBalanceReportItemDto>> GetCustomerBalanceReportAsync();
    Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int top = 10);
    Task<List<CriticalStockReportItemDto>> GetCriticalStockReportAsync();
    Task<StockDistributionReportDto> GetStockDistributionReportAsync();

    // Muhasebesel Analiz & Vergi Takip Raporları
    Task<VatReportDto> GetVatReportAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<IncomeStatementReportDto> GetIncomeStatementReportAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<CustomerLedgerStatementDto?> GetCustomerLedgerStatementAsync(int customerId);
    Task<List<TaxCalendarItemDto>> GetTaxCalendarAsync();
    Task<List<VatPaymentTrackingDto>> GetVatPaymentTrackingAsync(int year);
    Task<TrialBalanceReportDto> GetTrialBalanceReportAsync();
    Task<BalanceSheetReportDto> GetBalanceSheetReportAsync();
}
