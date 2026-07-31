using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Ürünler modülü controller'ı.
/// Hem JSON (AJAX) hem de View döndüren action'lar içerir.
/// </summary>
public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    public ProductsController(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService;
        _categoryService = categoryService;
    }

    // ─── Liste Sayfası ────────────────────────────────────────────────────────

    /// <summary>Ana ürün listesi sayfası</summary>
    public async Task<IActionResult> Index()
    {
        ViewBag.Categories = await _categoryService.GetAllAsync();
        return View();
    }

    /// <summary>Sayfalanmış ürün listesi (AJAX)</summary>
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter)
    {
        var result = await _productService.GetPagedProductsAsync(filter);
        return Json(result);
    }

    // ─── Ürün Detayı ─────────────────────────────────────────────────────────

    /// <summary>Ürün detayı (Modal için JSON)</summary>
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = "Ürün bulunamadı." });
        return Json(product);
    }

    // ─── Ürün Oluşturma ───────────────────────────────────────────────────────

    /// <summary>Yeni ürün oluşturma (AJAX POST)</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        if (dto is null)
            return Json(new { success = false, message = "Geçersiz ürün verisi gönderildi." });

        if (!ModelState.IsValid)
        {
            var errors = string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Json(new { success = false, message = string.IsNullOrEmpty(errors) ? "Form verilerinde hata mevcut." : errors });
        }

        var (success, message, productId) = await _productService.CreateAsync(dto);

        return Json(new { success, message, productId });
    }

    // ─── Ürün Düzenleme ───────────────────────────────────────────────────────

    /// <summary>Düzenlenecek ürün verilerini getir (AJAX)</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _productService.GetForEditAsync(id);
        if (product is null)
            return NotFound(new { message = "Ürün bulunamadı." });
        return Json(product);
    }

    /// <summary>Ürün güncelle (AJAX PUT)</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([FromBody] UpdateProductDto dto)
    {
        if (dto is null)
            return BadRequest(new { success = false, message = "Geçersiz veri." });

        var (success, message) = await _productService.UpdateAsync(dto);
        return Json(new { success, message });
    }

    // ─── Ürün Silme ───────────────────────────────────────────────────────────

    /// <summary>Ürünü soft-delete ile sil (AJAX)</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _productService.DeleteAsync(id);
        return Json(new { success, message });
    }

    // ─── Durum Değiştir ───────────────────────────────────────────────────────

    /// <summary>Aktif/Pasif durumu değiştir (AJAX)</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var (success, message) = await _productService.ToggleStatusAsync(id);
        return Json(new { success, message });
    }

    // ─── Yardımcı ─────────────────────────────────────────────────────────────

    /// <summary>Kategorileri dropdown için getir (AJAX)</summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _categoryService.GetAllAsync();
        return Json(categories);
    }

    /// <summary>Ürün kodu benzersizlik kontrolü (AJAX)</summary>
    [HttpGet]
    public async Task<IActionResult> CheckProductCode(string code, int? excludeId = null)
    {
        // Doğrudan repository'ye erişim yerine service üzerinden
        var filter = new ProductFilterDto { SearchTerm = code, PageSize = 1 };
        var result = await _productService.GetPagedProductsAsync(filter);
        var exists = result.Items.Any(p => p.ProductCode == code && (!excludeId.HasValue || p.Id != excludeId));
        return Json(new { isUnique = !exists });
    }

    /// <summary>Enum listelerini form için getir</summary>
    [HttpGet]
    public IActionResult GetEnums()
    {
        return Json(new
        {
            productTypes = Enum.GetValues<ProductType>().Select(e => new
            {
                value = (int)e,
                text = e switch
                {
                    ProductType.StockedProduct => "Stoklu Ürün",
                    ProductType.Service => "Hizmet",
                    ProductType.RawMaterial => "Hammadde",
                    ProductType.FinishedProduct => "Mamul",
                    ProductType.SemiFinished => "Yarı Mamul",
                    _ => e.ToString()
                }
            }),
            units = Enum.GetValues<Unit>().Select(e => new
            {
                value = (int)e,
                text = e switch
                {
                    Unit.Piece => "Adet",
                    Unit.Kg => "Kg",
                    Unit.Liter => "Litre",
                    Unit.Package => "Paket",
                    Unit.Box => "Koli",
                    Unit.Meter => "Metre",
                    Unit.Pair => "Çift",
                    _ => e.ToString()
                }
            }),
            vatRates = Enum.GetValues<VatRate>().Select(e => new
            {
                value = (int)e,
                text = $"%{(int)e}"
            }),
            discountTypes = Enum.GetValues<DiscountType>().Select(e => new
            {
                value = (int)e,
                text = e switch
                {
                    DiscountType.None => "İskonto Yok",
                    DiscountType.Percentage => "Yüzde (%)",
                    DiscountType.Amount => "Tutar (₺)",
                    _ => e.ToString()
                }
            }),
            specialTaxTypes = Enum.GetValues<SpecialTaxType>().Select(e => new
            {
                value = (int)e,
                text = e switch
                {
                    SpecialTaxType.None => "Yok",
                    SpecialTaxType.Proportional => "Oransal",
                    SpecialTaxType.Amount => "Tutar",
                    _ => e.ToString()
                }
            })
        });
    }
}
