using System;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

public class WarehousesController : Controller
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("api/[controller]")]
    [HttpGet("[controller]/GetWarehouses")]
    public async Task<IActionResult> GetAll([FromQuery] WarehouseFilterDto filter)
    {
        filter ??= new WarehouseFilterDto();
        if (filter.Page <= 0) filter.Page = 1;
        if (filter.PageNumber <= 0) filter.PageNumber = filter.Page;
        if (filter.PageSize <= 0) filter.PageSize = 15;

        var result = await _warehouseService.GetPagedAsync(filter);
        return Json(new { items = result.Items, totalCount = result.TotalCount });
    }

    [HttpGet("api/[controller]/active")]
    [HttpGet("[controller]/GetActive")]
    public async Task<IActionResult> GetActive()
    {
        var result = await _warehouseService.GetAllActiveAsync();
        return Json(result);
    }

    [HttpGet("api/[controller]/{id}")]
    [HttpGet("[controller]/GetDetail/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _warehouseService.GetByIdAsync(id);
        if (result == null)
            return Json(new { success = false, message = "Depo bulunamadı." });
            
        return Json(result);
    }

    [HttpPost("api/[controller]")]
    [HttpPost("[controller]/Create")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateWarehouseDto dto)
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
            var result = await _warehouseService.CreateAsync(dto);
            return Json(new { success = true, message = "Depo başarıyla oluşturuldu.", id = result.Id });
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
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWarehouseDto dto)
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
            await _warehouseService.UpdateAsync(id, dto);
            return Json(new { success = true, message = "Depo başarıyla güncellendi." });
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
            await _warehouseService.DeleteAsync(id);
            return Json(new { success = true, message = "Depo başarıyla silindi." });
        }
        catch (Exception ex)
        {
            var innerMsg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = innerMsg });
        }
    }
}
