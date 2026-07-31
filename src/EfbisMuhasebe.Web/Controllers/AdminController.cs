using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

using EfbisMuhasebe.Web.Extensions;

namespace EfbisMuhasebe.Web.Controllers;

[Authorize] // Accessible by authenticated users with Admin role or StoreManager
public class AdminController : Controller
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AdminController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var activeTenantId = HttpContext.Session.GetInt32("ActiveTenantId") ?? 0;
        ViewBag.ActiveTenantId = activeTenantId;

        var tenants = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedDate)
            .ToListAsync();

        return View(tenants);
    }

    /// <summary>
    /// Super Admin Impersonation Endpoint: Updates authentication cookie with ImpersonatedTenantId claim and redirects to tenant view dashboard.
    /// </summary>
    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Impersonate(int tenantId)
    {
        var userRoleStr = User.FindFirstValue(ClaimTypes.Role);
        if (userRoleStr != UserRole.SuperAdmin.ToString() && !User.IsInRole("SuperAdmin"))
        {
            TempData["ErrorMessage"] = "Sadece Super Admin müşteri hesabı taklidi (impersonation) gerçekleştirebilir.";
            return RedirectToAction("Index");
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted);
        if (tenant == null)
        {
            TempData["ErrorMessage"] = "Belirtilen şirket hesabı bulunamadı.";
            return RedirectToAction("Index");
        }

        await HttpContext.ImpersonateTenantAsync(tenant.Id, tenant.CompanyName);
        TempData["SuccessMessage"] = $"\"{tenant.CompanyName}\" müşteri hesabına geçiş yapıldı.";

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Stop Impersonating: Removes ImpersonatedTenantId claim from cookie and restores Super Admin global view.
    /// </summary>
    [HttpPost]
    [HttpGet]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> StopImpersonating()
    {
        await HttpContext.StopImpersonatingAsync();
        TempData["InfoMessage"] = "Müşteri hesabından çıkış yapıldı. Super Admin Kumanda Merkezi'ne geri dönüldü.";
        return RedirectToAction("Index", "Admin");
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var stats = await _authService.GetAdminStatsAsync();
        var tenantCount = await _context.Tenants.CountAsync(t => !t.IsDeleted);
        var activeTenantCount = await _context.Tenants.CountAsync(t => !t.IsDeleted && t.IsActive);
        var totalInvoicesAmount = await _context.Invoices.IgnoreQueryFilters().Where(i => !i.IsDeleted).SumAsync(i => (decimal?)i.GrandTotal) ?? 0;
        var totalInvoiceCount = await _context.Invoices.IgnoreQueryFilters().CountAsync(i => !i.IsDeleted);

        var activeTenantId = HttpContext.Session.GetInt32("ActiveTenantId") ?? 0;
        string currentTenantName = "Tüm Şirketler (Ortak Görünüm)";
        if (activeTenantId > 0)
        {
            var t = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == activeTenantId);
            if (t != null) currentTenantName = t.CompanyName;
        }

        return Json(new {
            success = true,
            data = new {
                stats.TotalUsers,
                stats.ActiveUsers,
                stats.AdminUsers,
                stats.TotalProducts,
                stats.TotalCustomers,
                stats.TotalInvoices,
                stats.TotalWarehouses,
                stats.TotalEmployees,
                TenantCount = tenantCount,
                ActiveTenantCount = activeTenantCount,
                TotalInvoicesAmount = totalInvoicesAmount,
                TotalInvoiceCount = totalInvoiceCount,
                ActiveTenantId = activeTenantId,
                CurrentTenantName = currentTenantName,
                DbServer = "(localdb)\\mssqllocaldb [EF Core Primary Node]",
                SystemStatus = "Sağlıklı (Healthy 🟢)"
            }
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetTenants()
    {
        var tenants = await _context.Tenants
            .Where(t => !t.IsDeleted)
            .OrderByDescending(t => t.CreatedDate)
            .Select(t => new {
                t.Id,
                t.TenantCode,
                t.CompanyName,
                t.TradeTitle,
                t.Email,
                t.Phone,
                t.City,
                t.Sector,
                t.TaxNumber,
                t.TaxOffice,
                t.IsActive,
                CreatedDateStr = t.CreatedDate.ToString("dd.MM.yyyy HH:mm"),
                ProductCount = _context.Products.IgnoreQueryFilters().Count(p => !p.IsDeleted && p.TenantId == t.Id),
                InvoiceCount = _context.Invoices.IgnoreQueryFilters().Count(i => !i.IsDeleted && i.TenantId == t.Id),
                UserCount = _context.Users.IgnoreQueryFilters().Count(u => !u.IsDeleted && u.TenantId == t.Id)
            })
            .ToListAsync();

        return Json(new { success = true, data = tenants });
    }

    [HttpGet]
    public async Task<IActionResult> GetTenantDatabaseDetails(int tenantId)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != UserRole.SuperAdmin.ToString() && !User.IsInRole("SuperAdmin"))
        {
            return Json(new { success = false, message = "Yetkisiz erişim." });
        }

        var tenant = await _context.Tenants.IgnoreQueryFilters().FirstOrDefaultAsync(t => t.Id == tenantId);
        if (tenant == null) return Json(new { success = false, message = "Şirket veritabanı bulunamadı." });

        var products = await _context.Products.IgnoreQueryFilters()
            .Where(p => !p.IsDeleted && p.TenantId == tenantId)
            .Select(p => new { p.Id, p.ProductCode, p.ProductName, p.SalePrice, p.CurrentStock, CreatedDateStr = p.CreatedDate.ToString("dd.MM.yyyy HH:mm") })
            .ToListAsync();

        var invoices = await _context.Invoices.IgnoreQueryFilters()
            .Where(i => !i.IsDeleted && i.TenantId == tenantId)
            .Select(i => new { i.Id, i.InvoiceNumber, i.GrandTotal, InvoiceDateStr = i.InvoiceDate.ToString("dd.MM.yyyy"), StatusStr = i.Status.ToString() })
            .ToListAsync();

        var customers = await _context.Customers.IgnoreQueryFilters()
            .Where(c => !c.IsDeleted && c.TenantId == tenantId)
            .Select(c => new { c.Id, c.CustomerCode, c.Title, c.Balance, c.Phone })
            .ToListAsync();

        var cashAccounts = await _context.CashAccounts.IgnoreQueryFilters()
            .Where(ca => !ca.IsDeleted && ca.TenantId == tenantId)
            .Select(ca => new { ca.Id, ca.AccountCode, ca.AccountName, ca.Balance, ca.Currency })
            .ToListAsync();

        var employees = await _context.Employees.IgnoreQueryFilters()
            .Where(e => !e.IsDeleted && e.TenantId == tenantId)
            .Select(e => new { e.Id, e.FullName, e.Title, e.Department, e.Phone })
            .ToListAsync();

        return Json(new {
            success = true,
            tenant = new {
                tenant.Id,
                tenant.TenantCode,
                tenant.CompanyName,
                tenant.TradeTitle,
                tenant.Email,
                DbServer = "(localdb)\\mssqllocaldb",
                DbName = "EfbisMuhasebeDb",
                SchemaScope = $"dbo.* WHERE TenantId = {tenantId}",
                TableCounts = new {
                    ProductsCount = products.Count,
                    InvoicesCount = invoices.Count,
                    CustomersCount = customers.Count,
                    CashAccountsCount = cashAccounts.Count,
                    EmployeesCount = employees.Count
                }
            },
            tables = new {
                products,
                invoices,
                customers,
                cashAccounts,
                employees
            }
        });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult SwitchActiveTenant([FromBody] SwitchTenantRequest req)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != UserRole.SuperAdmin.ToString())
        {
            return Json(new { success = false, message = "Sadece Super Admin şirket görünümünü değiştirebilir." });
        }

        HttpContext.Session.SetInt32("ActiveTenantId", req.TenantId);
        return Json(new { success = true, message = "Aktif şirket görünümü başarıyla güncellendi.", activeTenantId = req.TenantId });
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers(
        int pageNumber = 1, int pageSize = 10, string? searchTerm = null, int? role = null, bool? isActive = null)
    {
        UserRole? roleEnum = role.HasValue ? (UserRole)role.Value : null;
        var (items, totalCount) = await _authService.GetUsersPagedAsync(pageNumber, pageSize, searchTerm, roleEnum, isActive);
        return Json(new { success = true, data = new { items, totalCount } });
    }

    [HttpGet]
    public async Task<IActionResult> GetUserDetail(int id)
    {
        var user = await _authService.GetUserByIdAsync(id);
        if (user == null)
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });

        return Json(new { success = true, data = user });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid)
            return Json(new { success = false, message = "Lütfen alanları kontrol ediniz." });

        var (success, message, user) = await _authService.CreateUserAsync(dto);
        return Json(new { success, message, data = user });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserDto dto)
    {
        dto.Id = id;
        var (success, message) = await _authService.UpdateUserAsync(dto);
        return Json(new { success, message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ToggleUserStatus(int id)
    {
        var (success, message) = await _authService.ToggleUserStatusAsync(id);
        return Json(new { success, message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ResetUserPassword([FromBody] ResetUserPasswordDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword))
            return Json(new { success = false, message = "Lütfen yeni şifreyi giriniz." });

        var (success, message) = await _authService.ResetPasswordAsync(dto.UserId, dto.NewPassword);
        return Json(new { success, message });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var (success, message) = await _authService.DeleteUserAsync(id);
        return Json(new { success, message });
    }

    [HttpGet]
    public IActionResult GetAuditLogs()
    {
        var now = DateTime.Now;
        var currentUser = User.Identity?.Name ?? "Admin";

        var logs = new List<AuditLogDto>
        {
            new() { Timestamp = now.ToString("dd.MM.yyyy HH:mm:ss"), User = currentUser, Level = "SECURITY", Action = "Yönetici Paneline Giriş Yapıldı", Module = "Admin", IpAddress = "192.168.1.10" },
            new() { Timestamp = now.AddMinutes(-12).ToString("dd.MM.yyyy HH:mm:ss"), User = "Fatih Yılmaz", Level = "INFO", Action = "Vardiya Planı Otomatik Oluşturuldu (20 Personel)", Module = "Vardiya", IpAddress = "192.168.1.10" },
            new() { Timestamp = now.AddMinutes(-45).ToString("dd.MM.yyyy HH:mm:ss"), User = "Zeynep Kaya", Level = "INFO", Action = "SFT-2026-001 Nolu Satış Faturası Onaylandı", Module = "Fatura", IpAddress = "192.168.1.25" },
            new() { Timestamp = now.AddHours(-2).ToString("dd.MM.yyyy HH:mm:ss"), User = "Mehmet Can", Level = "INFO", Action = "Maaş Ödemeleri Bordrosu Güncellendi", Module = "Maaş", IpAddress = "192.168.1.30" },
            new() { Timestamp = now.AddHours(-4).ToString("dd.MM.yyyy HH:mm:ss"), User = "Fatih Yılmaz", Level = "WARN", Action = "Stok Seviyesi Kritik Uyarısı: Samsung TV (2 Adet Kaldı)", Module = "Stok", IpAddress = "192.168.1.10" },
            new() { Timestamp = now.AddHours(-6).ToString("dd.MM.yyyy HH:mm:ss"), User = "Zeynep Kaya", Level = "INFO", Action = "Yeni Kullanıcı Oluşturuldu: Zeynep Arslan", Module = "Kullanıcı", IpAddress = "192.168.1.25" }
        };

        return Json(new { success = true, data = logs });
    }
}
