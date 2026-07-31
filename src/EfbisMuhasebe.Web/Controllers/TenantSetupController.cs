using System;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Web.Controllers;

public class TenantSetupController : Controller
{
    private readonly AppDbContext _context;

    public TenantSetupController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> SeedBeltur()
    {
        try
        {
            await EfbisMuhasebe.Infrastructure.Data.Seed.DbSeeder.SeedBelturFloryaDataAsync(_context);
            return Json(new { success = true, message = "BELTUR Florya Sosyal Tesisleri (farukbeltur@beltur.com) hesabı ve tüm 5'er adetlik özel demo verileri veritabanına yüklendi!" });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = "SEED FAIL: " + msg });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _context.Tenants.Where(t => !t.IsDeleted).ToListAsync();
        return Json(tenants);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> DebugTenants()
    {
        var tenants = await _context.Tenants.IgnoreQueryFilters().ToListAsync();
        var users = await _context.Users.IgnoreQueryFilters().Select(u => new { u.Id, u.Email, u.TenantId, u.Role }).ToListAsync();
        var products = await _context.Products.IgnoreQueryFilters().Select(p => new { p.Id, p.ProductName, p.TenantId }).ToListAsync();
        var invoices = await _context.Invoices.IgnoreQueryFilters().Select(i => new { i.Id, i.InvoiceNumber, i.TenantId, i.GrandTotal }).ToListAsync();
        var custs = await _context.Customers.IgnoreQueryFilters().Select(c => new { c.Id, c.Title, c.TenantId }).ToListAsync();

        return Json(new {
            tenants,
            users,
            products,
            invoices,
            custs
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateNewTenant([FromBody] CreateTenantRequest req)
    {
        try
        {
            if (req == null || string.IsNullOrWhiteSpace(req.CompanyName) || string.IsNullOrWhiteSpace(req.AdminEmail) || string.IsNullOrWhiteSpace(req.AdminPassword))
            {
                return Json(new { success = false, message = "Lütfen Şirket Adı, Yetkili E-Posta ve Şifre alanlarını doldurunuz." });
            }

            var existingUser = await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == req.AdminEmail && !u.IsDeleted);
            if (existingUser)
            {
                return Json(new { success = false, message = "Bu e-posta adresi ile zaten kayıtlı bir kullanıcı bulunuyor." });
            }

            // 1. Yeni Tenant Tanımla
            var maxId = await _context.Tenants.IgnoreQueryFilters().AnyAsync() ? await _context.Tenants.IgnoreQueryFilters().MaxAsync(t => t.Id) : 0;
            int newTenantId = maxId + 1;

            var tenant = new Tenant
            {
                TenantCode = $"TEN-{newTenantId:D3}",
                CompanyName = req.CompanyName.Trim(),
                TradeTitle = req.TradeTitle ?? req.CompanyName.Trim(),
                TaxNumber = req.TaxNumber ?? "1234567890",
                TaxOffice = req.TaxOffice ?? "Ticaret Vergi Dairesi",
                Sector = req.Sector ?? "Genel Ticaret & Perakende",
                Phone = req.Phone ?? "0500 000 0000",
                Email = req.AdminEmail.Trim(),
                City = req.City ?? "İstanbul",
                Address = req.Address ?? "Merkez Adres",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();

            // 2. Müşteri Şirket Admin Hesabı Aç (TenantId = newTenantId)
            var adminUser = new User
            {
                TenantId = tenant.Id,
                Email = req.AdminEmail.Trim(),
                PasswordHash = Application.Services.AuthService.HashPassword(req.AdminPassword.Trim()),
                FullName = string.IsNullOrWhiteSpace(req.AdminFullName) ? (req.CompanyName + " Yöneticisi") : req.AdminFullName.Trim(),
                Role = UserRole.Admin,
                Title = req.CompanyName + " Genel Müdürü (Tenant Admin)",
                PhoneNumber = req.Phone ?? "0500 000 0000",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = $"{req.CompanyName} için yeni müşteri veritabanı alanı ve Tenant Admin üyeliği başarıyla oluşturuldu!",
                tenantId = tenant.Id,
                adminEmail = req.AdminEmail,
                adminPassword = req.AdminPassword
            });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = "Kurulum sırasında hata oluştu: " + msg });
        }
    }
}

public class CreateTenantRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string? TradeTitle { get; set; }
    public string? Sector { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
}
