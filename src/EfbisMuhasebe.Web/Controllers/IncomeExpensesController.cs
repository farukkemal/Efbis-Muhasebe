using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class IncomeExpensesController : Controller
{
    private readonly IIncomeExpenseService _service;
    private readonly ICustomerService _customerService;

    public IncomeExpensesController(IIncomeExpenseService service, ICustomerService customerService)
    {
        _service = service;
        _customerService = customerService;
    }

    public IActionResult Index() => View();

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetRecords")]
    public async Task<IActionResult> GetAll([FromQuery] IncomeExpenseFilterDto filter)
    {
        var result = await _service.GetPagedAsync(filter);
        return Json(result);
    }

    [HttpGet("api/[controller]/dashboard")]
    [HttpGet("[controller]/GetDashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string? period)
    {
        var result = await _service.GetDashboardAsync();
        return Json(result);
    }

    [HttpGet("api/[controller]/monthly")]
    [HttpGet("[controller]/GetMonthly")]
    public async Task<IActionResult> GetMonthly([FromQuery] int year)
    {
        if (year <= 0) year = DateTime.UtcNow.Year;
        var result = await _service.GetMonthlySummaryAsync(year);
        return Json(result);
    }

    [HttpGet("api/[controller]/customers")]
    [HttpGet("[controller]/GetCustomers")]
    public async Task<IActionResult> GetCustomers()
    {
        var result = await _customerService.GetPagedAsync(new CustomerFilterDto { PageSize = 500, Status = Domain.Enums.CustomerStatus.Active });
        return Json(result.Items);
    }

    [HttpPost("api/[controller]")]
    [HttpPost("[controller]/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateIncomeExpenseDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        var (success, message, id) = await _service.CreateAsync(dto);
        return Json(new { success, message, id });
    }

    [HttpPost("api/[controller]/delete/{id}")]
    [HttpPost("[controller]/Delete/{id}")]
    [HttpDelete("api/[controller]/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAsync(id);
        return Json(new { success, message });
    }
}
