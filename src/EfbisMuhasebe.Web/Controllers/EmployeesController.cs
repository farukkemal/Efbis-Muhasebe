using System;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class EmployeesController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly IWarehouseService _warehouseService;

    public EmployeesController(IEmployeeService employeeService, IWarehouseService warehouseService)
    {
        _employeeService = employeeService;
        _warehouseService = warehouseService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetEmployees")]
    public async Task<IActionResult> GetAll([FromQuery] EmployeeFilterDto filter)
    {
        filter ??= new EmployeeFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageNumber <= 0) filter.PageNumber = filter.Page;
        if (filter.PageSize <= 0) filter.PageSize = 15;

        var result = await _employeeService.GetPagedAsync(filter);
        return Json(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("api/[controller]/dashboard")]
    [HttpGet("[controller]/GetDashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var stats = await _employeeService.GetDashboardAsync();
        return Json(stats);
    }

    [HttpGet("api/[controller]/{id}")]
    [HttpGet("[controller]/GetDetail/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _employeeService.GetByIdAsync(id);
        if (result == null)
            return Json(new { success = false, message = "Personel bulunamadı." });

        return Json(result);
    }

    [HttpPost("api/[controller]")]
    [HttpPost("[controller]/Create")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto dto)
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
            var result = await _employeeService.CreateAsync(dto);
            return Json(new { success = true, message = "Personel kaydı başarıyla oluşturuldu.", id = result.Id });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }

    [HttpPost("[controller]/Update/{id}")]
    [HttpPut("api/[controller]/{id}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz veri gönderildi." });

        dto.Id = id;
        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, message = errors });
        }

        try
        {
            await _employeeService.UpdateAsync(id, dto);
            return Json(new { success = true, message = "Personel bilgileri güncellendi." });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }

    [HttpPost("[controller]/Delete/{id}")]
    [HttpDelete("api/[controller]/{id}")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _employeeService.DeleteAsync(id);
            return Json(new { success = true, message = "Personel kaydı silindi." });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }

    [HttpGet("api/[controller]/warehouses")]
    [HttpGet("[controller]/GetWarehouses")]
    public async Task<IActionResult> GetWarehouses()
    {
        var warehouses = await _warehouseService.GetAllActiveAsync();
        return Json(warehouses);
    }
}
