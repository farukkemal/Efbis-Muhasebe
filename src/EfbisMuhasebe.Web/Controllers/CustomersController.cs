using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Cari Hesaplar (Müşteri & Tedarikçi Yönetimi) Controller'ı
/// </summary>
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    // ─── Index View ───────────────────────────────────────────────────────────

    public IActionResult Index([FromQuery] string? type = null)
    {
        ViewBag.DefaultType = type; // e.g. "customer" or "supplier"
        return View();
    }

    // ─── AJAX Endpoints ───────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] CustomerFilterDto filter)
    {
        var result = await _customerService.GetPagedAsync(filter);
        return Json(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _customerService.GetDashboardStatsAsync();
        return Json(stats);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer is null) return NotFound(new { success = false, message = "Cari hesap bulunamadı." });
        return Json(customer);
    }

    [HttpGet]
    public async Task<IActionResult> GetForEdit(int id)
    {
        var customer = await _customerService.GetForEditAsync(id);
        if (customer is null) return NotFound(new { success = false, message = "Cari hesap bulunamadı." });
        return Json(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, customerId) = await _customerService.CreateAsync(dto);
        return Json(new { success, message, customerId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateCustomerDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message) = await _customerService.UpdateAsync(dto);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _customerService.DeleteAsync(id);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var (success, message) = await _customerService.ToggleStatusAsync(id);
        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkStatus([FromBody] BulkStatusUpdateDto dto)
    {
        if (dto is null || dto.ProductIds is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message, count) = await _customerService.BulkUpdateStatusAsync(dto.ProductIds, (Domain.Enums.CustomerStatus)dto.Status);
        return Json(new { success, message, count });
    }
}
