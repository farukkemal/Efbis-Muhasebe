using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Raporlar Controller — Stok değer, cari bakiye, en çok satılan ürün, kritik stok raporları.
/// </summary>
public class ReportsController : Controller
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public IActionResult Index() => View();

    [HttpGet("api/[controller]/dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var result = await _reportService.GetDashboardReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/stock-value")]
    public async Task<IActionResult> GetStockValueReport()
    {
        try
        {
            var result = await _reportService.GetStockValueReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/customer-balance")]
    public async Task<IActionResult> GetCustomerBalanceReport()
    {
        try
        {
            var result = await _reportService.GetCustomerBalanceReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/top-selling")]
    public async Task<IActionResult> GetTopSellingProducts([FromQuery] int top = 10)
    {
        try
        {
            var result = await _reportService.GetTopSellingProductsAsync(top);
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/critical-stock")]
    public async Task<IActionResult> GetCriticalStockReport()
    {
        try
        {
            var result = await _reportService.GetCriticalStockReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/stock-distribution")]
    public async Task<IActionResult> GetStockDistribution()
    {
        try
        {
            var result = await _reportService.GetStockDistributionReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/vat")]
    public async Task<IActionResult> GetVatReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var result = await _reportService.GetVatReportAsync(startDate, endDate);
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/income-statement")]
    public async Task<IActionResult> GetIncomeStatementReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        try
        {
            var result = await _reportService.GetIncomeStatementReportAsync(startDate, endDate);
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/customer-ledger/{id}")]
    public async Task<IActionResult> GetCustomerLedgerStatement(int id)
    {
        try
        {
            var result = await _reportService.GetCustomerLedgerStatementAsync(id);
            if (result == null) return NotFound(new { Message = "Cari hesap bulunamadı." });
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/tax-calendar")]
    public async Task<IActionResult> GetTaxCalendar()
    {
        try
        {
            var result = await _reportService.GetTaxCalendarAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/vat-tracking")]
    public async Task<IActionResult> GetVatTracking([FromQuery] int? year)
    {
        try
        {
            var y = year ?? DateTime.Now.Year;
            var result = await _reportService.GetVatPaymentTrackingAsync(y);
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/trial-balance")]
    public async Task<IActionResult> GetTrialBalance()
    {
        try
        {
            var result = await _reportService.GetTrialBalanceReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/balance-sheet")]
    public async Task<IActionResult> GetBalanceSheet()
    {
        try
        {
            var result = await _reportService.GetBalanceSheetReportAsync();
            return Json(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
