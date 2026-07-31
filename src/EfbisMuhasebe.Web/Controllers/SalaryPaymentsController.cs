using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class SalaryPaymentsController : Controller
{
    private readonly ISalaryPaymentService _salaryPaymentService;
    private readonly IEmployeeService _employeeService;
    private readonly ICashAccountService _cashAccountService;

    public SalaryPaymentsController(
        ISalaryPaymentService salaryPaymentService,
        IEmployeeService employeeService,
        ICashAccountService cashAccountService)
    {
        _salaryPaymentService = salaryPaymentService;
        _employeeService = employeeService;
        _cashAccountService = cashAccountService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] SalaryPaymentFilterDto filter)
    {
        try
        {
            var (items, totalCount) = await _salaryPaymentService.GetPagedAsync(filter);
            return Json(new { items = items, totalCount = totalCount });
        }
        catch (Exception ex)
        {
            return Json(new { items = new List<SalaryPaymentDto>(), totalCount = 0, error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard(int? year, int? month)
    {
        try
        {
            var stats = await _salaryPaymentService.GetDashboardAsync(year, month);
            return Json(stats);
        }
        catch
        {
            return Json(new SalaryDashboardDto());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDetail(int id)
    {
        try
        {
            var payment = await _salaryPaymentService.GetByIdAsync(id);
            if (payment == null) return NotFound();
            return Json(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        try
        {
            var (items, _) = await _employeeService.GetPagedAsync(new EmployeeFilterDto { PageSize = 100, Status = Domain.Enums.EmployeeStatus.Active });
            return Json(items);
        }
        catch
        {
            return Json(new List<EmployeeDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCashAccounts()
    {
        try
        {
            var accounts = await _cashAccountService.GetActiveAccountsAsync();
            return Json(accounts);
        }
        catch
        {
            return Json(new List<CashAccountDto>());
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateSalaryPaymentDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _salaryPaymentService.CreateAsync(dto);
        return Json(new { success = true, data = result });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GeneratePayroll([FromBody] GeneratePayrollDto dto)
    {
        var count = await _salaryPaymentService.GenerateMonthlyPayrollAsync(dto.Year, dto.Month);
        return Json(new { success = true, generatedCount = count });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        var result = await _salaryPaymentService.MarkAsPaidAsync(id);
        return Json(new { success = result });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkPay([FromBody] GeneratePayrollDto dto)
    {
        var count = await _salaryPaymentService.BulkPayAsync(dto.Year, dto.Month);
        return Json(new { success = true, paidCount = count });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _salaryPaymentService.CancelAsync(id);
        return Json(new { success = result });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateSalaryPaymentDto dto)
    {
        if (dto == null || dto.Id <= 0)
            return BadRequest(new { success = false, message = "Geçersiz veri." });

        var result = await _salaryPaymentService.UpdateAsync(dto.Id, dto);
        return Json(new { success = result, message = result ? "Maaş bordrosu başarıyla güncellendi." : "Bordro kaydı bulunamadı." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _salaryPaymentService.DeleteAsync(id);
        return Json(new { success = result, message = result ? "Bordro kaydı silindi." : "Kayıt bulunamadı." });
    }
}
