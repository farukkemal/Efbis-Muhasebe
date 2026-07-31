using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class CashAccountsController : Controller
{
    private readonly ICashAccountService _service;
    private readonly ICustomerService _customerService;

    public CashAccountsController(ICashAccountService service, ICustomerService customerService)
    {
        _service = service;
        _customerService = customerService;
    }

    public IActionResult Index() => View();

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetAccounts")]
    public async Task<IActionResult> GetAll([FromQuery] CashAccountFilterDto filter)
    {
        var result = await _service.GetAccountsPagedAsync(filter);
        return Json(result);
    }

    [HttpGet("api/[controller]/active")]
    [HttpGet("[controller]/GetActive")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _service.GetActiveAccountsAsync();
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
    public async Task<IActionResult> Create([FromBody] CreateCashAccountDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        var (success, message, id) = await _service.CreateAccountAsync(dto);
        return Json(new { success, message, id });
    }

    [HttpPost("api/[controller]/update")]
    [HttpPost("[controller]/Update/{id}")]
    [HttpPut("api/[controller]/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCashAccountDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        dto.Id = id;
        var (success, message) = await _service.UpdateAccountAsync(dto);
        return Json(new { success, message });
    }

    [HttpPost("api/[controller]/delete/{id}")]
    [HttpPost("[controller]/Delete/{id}")]
    [HttpDelete("api/[controller]/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteAccountAsync(id);
        return Json(new { success, message });
    }

    [HttpGet("api/[controller]/transactions")]
    [HttpGet("[controller]/GetTransactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] CashTransactionFilterDto filter)
    {
        var result = await _service.GetTransactionsPagedAsync(filter);
        return Json(result);
    }

    [HttpPost("api/[controller]/transactions")]
    [HttpPost("[controller]/CreateTransaction")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTransaction([FromBody] CreateCashTransactionDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        var (success, message, id) = await _service.CreateTransactionAsync(dto);
        return Json(new { success, message, id });
    }

    [HttpGet("api/[controller]/dashboard")]
    [HttpGet("[controller]/GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _service.GetDashboardAsync();
        return Json(result);
    }
}
