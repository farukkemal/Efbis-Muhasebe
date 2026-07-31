using System;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class ShiftsController : Controller
{
    private readonly IShiftService _shiftService;
    private readonly IEmployeeService _employeeService;

    public ShiftsController(IShiftService shiftService, IEmployeeService employeeService)
    {
        _shiftService = shiftService;
        _employeeService = employeeService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetShifts([FromQuery] ShiftFilterDto filter)
    {
        try
        {
            var (items, totalCount) = await _shiftService.GetPagedAsync(filter);
            return Ok(new { success = true, data = new { items, totalCount } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var data = await _shiftService.GetDashboardAsync(DateTime.Now);
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetDetail(int id)
    {
        try
        {
            var data = await _shiftService.GetByIdAsync(id);
            if (data == null) return NotFound(new { success = false, message = "Vardiya bulunamadı." });
            return Ok(new { success = true, data });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateShiftDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Geçersiz veriler mevcut." });
            }

            var result = await _shiftService.CreateAsync(dto);
            return Ok(new { success = true, message = "Vardiya başarıyla oluşturuldu.", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateShiftDto dto)
    {
        try
        {
            if (id != dto.Id) return BadRequest(new { success = false, message = "ID eşleşmiyor." });
            
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Geçersiz veriler mevcut." });
            }

            await _shiftService.UpdateAsync(id, dto);
            return Ok(new { success = true, message = "Vardiya güncellendi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _shiftService.DeleteAsync(id);
            return Ok(new { success = true, message = "Vardiya silindi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            await _shiftService.CheckInAsync(id);
            return Ok(new { success = true, message = "Giriş işlemi başarılı." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CheckOut(int id)
    {
        try
        {
            await _shiftService.CheckOutAsync(id);
            return Ok(new { success = true, message = "Çıkış işlemi başarılı." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> MarkAbsent(int id)
    {
        try
        {
            await _shiftService.MarkAbsentAsync(id);
            return Ok(new { success = true, message = "Devamsız olarak işaretlendi." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        try
        {
            var filter = new EmployeeFilterDto { PageSize = 1000, Status = Domain.Enums.EmployeeStatus.Active };
            var result = await _employeeService.GetPagedAsync(filter);
            return Ok(new { success = true, data = result.Items });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> GenerateWeeklySchedule([FromBody] GenerateScheduleRequest? req)
    {
        try
        {
            var targetDate = req?.TargetDate ?? DateTime.Today;
            var count = await _shiftService.GenerateWeeklyScheduleAsync(targetDate);
            return Ok(new { success = true, message = $"{count} adet haftalık vardiya kaydı otomatik oluşturuldu.", count });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}

public class GenerateScheduleRequest
{
    public DateTime TargetDate { get; set; }
}
