using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Kategoriler modülü controller'ı
/// </summary>
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllWithDetailsAsync();
        return Json(categories);
    }

    [HttpGet]
    public async Task<IActionResult> GetById(int id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null) return NotFound(new { success = false, message = "Kategori bulunamadı." });
        return Json(category);
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Create([FromBody] CategoryDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message) = await _categoryService.CreateAsync(dto);
        return Json(new { success, message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Update([FromBody] CategoryDto dto)
    {
        if (dto is null) return BadRequest(new { success = false, message = "Geçersiz veri." });
        var (success, message) = await _categoryService.UpdateAsync(dto);
        return Json(new { success, message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _categoryService.DeleteAsync(id);
        return Json(new { success, message });
    }
}
