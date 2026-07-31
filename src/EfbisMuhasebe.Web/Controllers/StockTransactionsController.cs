using System;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class StockTransactionsController : Controller
{
    private readonly IStockTransactionService _stockTransactionService;

    public StockTransactionsController(IStockTransactionService stockTransactionService)
    {
        _stockTransactionService = stockTransactionService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetTransactions")]
    public async Task<IActionResult> GetAll([FromQuery] StockTransactionFilterDto filter)
    {
        filter ??= new StockTransactionFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageNumber <= 0) filter.PageNumber = filter.Page;
        if (filter.PageSize <= 0) filter.PageSize = 15;

        var result = await _stockTransactionService.GetPagedAsync(filter);
        return Json(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("api/[controller]/dashboard")]
    [HttpGet("[controller]/GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _stockTransactionService.GetDashboardAsync();
        return Json(stats);
    }

    [HttpGet("api/[controller]/{id}")]
    [HttpGet("[controller]/GetDetail/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _stockTransactionService.GetByIdAsync(id);
        if (item == null) return Json(new { success = false, message = "Kayıt bulunamadı." });
        return Json(item);
    }

    [HttpPost("api/[controller]")]
    [HttpPost("[controller]/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateStockTransactionDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, message = errors });
        }

        try
        {
            var result = await _stockTransactionService.CreateAsync(dto);
            return Json(new { success = true, message = "Stok hareketi başarıyla kaydedildi.", id = result.Id });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
