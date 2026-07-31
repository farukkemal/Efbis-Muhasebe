using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Dashboard ana sayfa controller'ı.
/// </summary>
public class HomeController : Controller
{
    private readonly IReportService _reportService;
    private readonly IProductService _productService;
    private readonly ICashAccountService _cashAccountService;

    public HomeController(IReportService reportService, IProductService productService, ICashAccountService cashAccountService)
    {
        _reportService = reportService;
        _productService = productService;
        _cashAccountService = cashAccountService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var dashboard = await _reportService.GetDashboardReportAsync();
            ViewBag.Dashboard = dashboard;
        }
        catch
        {
            ViewBag.Dashboard = null;
        }

        try
        {
            var cashAccounts = await _cashAccountService.GetActiveAccountsAsync();
            ViewBag.CashAccounts = cashAccounts;
        }
        catch
        {
            ViewBag.CashAccounts = new List<CashAccountDto>();
        }

        try
        {
            var topSelling = await _reportService.GetTopSellingProductsAsync(5);
            ViewBag.TopSellingProducts = topSelling;
        }
        catch
        {
            ViewBag.TopSellingProducts = new List<TopSellingProductDto>();
        }

        try
        {
            var recentResult = await _productService.GetPagedProductsAsync(new ProductFilterDto
            {
                PageNumber = 1,
                PageSize = 5,
                SortBy = "CreatedDate",
                Ascending = false
            });
            ViewBag.RecentProducts = recentResult.Items;
        }
        catch
        {
            ViewBag.RecentProducts = new List<ProductDto>();
        }

        return View();
    }

    public IActionResult Privacy() => View();

    [HttpPost("api/System/ClearAllData")]
    public async Task<IActionResult> ClearAllData([FromServices] AppDbContext context)
    {
        try
        {
            await context.Database.ExecuteSqlRawAsync(@"
                DELETE FROM InvoiceItems;
                DELETE FROM Invoices;
                DELETE FROM StockTransactions;
                DELETE FROM CashTransactions;
                DELETE FROM IncomeExpenses;
                DELETE FROM Products;
                DELETE FROM Customers;
                DELETE FROM Warehouses;
                DELETE FROM CashAccounts;
                DELETE FROM Categories;
            ");
            return Ok(new { success = true, message = "Tüm örnek veriler veritabanından başarıyla temizlendi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
