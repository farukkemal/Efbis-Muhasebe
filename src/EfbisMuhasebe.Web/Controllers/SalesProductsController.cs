using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Satışta Olan Ürünler controller'ı.
/// Bu controller yalnızca satış politikalarını yönetir.
/// Ürün oluşturma, silme ve stok değiştirme işlemleri burada yoktur.
/// </summary>
public class SalesProductsController : Controller
{
    private readonly ISalesProductService _salesService;
    private readonly ICategoryService _categoryService;

    public SalesProductsController(ISalesProductService salesService, ICategoryService categoryService)
    {
        _salesService = salesService;
        _categoryService = categoryService;
    }

    // ─── Ana Sayfa ────────────────────────────────────────────────────────────

    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _categoryService.GetAllAsync();
        return View();
    }

    // ─── MVC API (AJAX) ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] SaleProductFilterDto filter)
    {
        var result = await _salesService.GetPagedAsync(filter);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _salesService.GetDashboardStatsAsync();
        return Json(stats);
    }

    [HttpGet]
    public async Task<IActionResult> GetDetail(int id)
    {
        var product = await _salesService.GetByIdAsync(id);
        if (product is null) return NotFound(new { message = "Ürün bulunamadı." });
        return Json(product);
    }

    // ─── Satış Durumu Toggle ──────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSaleStatus([FromBody] UpdateSaleStatusDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message) = await _salesService.UpdateSaleStatusAsync(dto);
        return Json(new { success, message });
    }

    // ─── Satış Fiyatı Güncelle ────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSalePrice([FromBody] UpdateSalePriceDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message) = await _salesService.UpdateSalePriceAsync(dto);
        return Json(new { success, message });
    }

    // ─── Toplu İşlemler ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkSaleStatus([FromBody] BulkSaleUpdateDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, count) = await _salesService.BulkUpdateSaleStatusAsync(dto);
        return Json(new { success, message, count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkStatus([FromBody] BulkStatusUpdateDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, count) = await _salesService.BulkUpdateStatusAsync(dto);
        return Json(new { success, message, count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkPrice([FromBody] BulkPriceUpdateDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, count) = await _salesService.BulkUpdatePriceAsync(dto);
        return Json(new { success, message, count });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkCategory([FromBody] BulkCategoryUpdateDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, count) = await _salesService.BulkUpdateCategoryAsync(dto);
        return Json(new { success, message, count });
    }
}
