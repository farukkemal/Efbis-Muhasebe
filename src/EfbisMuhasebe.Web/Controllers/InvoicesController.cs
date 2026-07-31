using System;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICustomerService _customerService;
    private readonly IProductService _productService;

    public InvoicesController(IInvoiceService invoiceService, ICustomerService customerService, IProductService productService)
    {
        _invoiceService = invoiceService;
        _customerService = customerService;
        _productService = productService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetInvoices")]
    public async Task<IActionResult> GetAll([FromQuery] InvoiceFilterDto filter)
    {
        var (items, totalCount) = await _invoiceService.GetPagedAsync(filter);
        return Json(new { items, totalCount });
    }

    [HttpGet("api/[controller]/dashboard")]
    [HttpGet("[controller]/GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _invoiceService.GetDashboardAsync();
        return Json(stats);
    }

    [HttpGet("api/[controller]/{id}")]
    [HttpGet("[controller]/GetDetail/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var invoice = await _invoiceService.GetByIdAsync(id);
        if (invoice == null) return Json(new { success = false, message = "Fatura bulunamadı." });
        return Json(invoice);
    }

    [HttpPost("api/[controller]")]
    [HttpPost("[controller]/Create")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
    {
        if (dto == null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, message = errors });
        }

        try
        {
            var result = await _invoiceService.CreateAsync(dto);
            return Json(new { success = true, message = "Fatura başarıyla oluşturuldu.", id = result.Id });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }

    [HttpPost("[controller]/UpdateStatus/{id}")]
    [HttpPut("api/[controller]/{id}/status")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateInvoiceStatusDto dto)
    {
        if (dto == null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        try
        {
            var success = await _invoiceService.UpdateStatusAsync(id, dto);
            if (!success) return Json(new { success = false, message = "Fatura bulunamadı." });
            return Json(new { success = true, message = "Fatura durumu başarıyla güncellendi." });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }

    [HttpGet("api/[controller]/customers")]
    [HttpGet("[controller]/GetCustomers")]
    public async Task<IActionResult> GetCustomers()
    {
        var filter = new CustomerFilterDto { PageSize = 1000 };
        var result = await _customerService.GetPagedAsync(filter);
        return Json(result.Items);
    }

    [HttpGet("api/[controller]/products")]
    [HttpGet("[controller]/GetProducts")]
    public async Task<IActionResult> GetProducts()
    {
        var filter = new ProductFilterDto { PageSize = 1000 };
        var result = await _productService.GetPagedProductsAsync(filter);
        return Json(result.Items);
    }
}
