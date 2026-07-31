using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Web.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly AppDbContext _context;

    public AccountController(IAuthService authService, AppDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Register([FromBody] RegisterTenantDto dto)
    {
        try
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.CompanyName) || string.IsNullOrWhiteSpace(dto.AdminEmail) || string.IsNullOrWhiteSpace(dto.AdminPassword))
            {
                return Json(new { success = false, message = "Lütfen Şirket Adı, Yönetici E-Posta ve Şifre alanlarını eksiksiz doldurunuz." });
            }

            if (dto.AdminPassword != dto.ConfirmPassword)
            {
                return Json(new { success = false, message = "Girdiğiniz şifreler birbiriyle uyuşmuyor." });
            }

            var existingUser = await _context.Users.AnyAsync(u => u.Email == dto.AdminEmail.Trim() && !u.IsDeleted);
            if (existingUser)
            {
                return Json(new { success = false, message = "Bu e-posta adresi ile zaten kayıtlı bir kullanıcı var." });
            }

            // 1. Yeni Şirket (Tenant) Oluştur
            var maxId = await _context.Tenants.IgnoreQueryFilters().AnyAsync() ? await _context.Tenants.IgnoreQueryFilters().MaxAsync(t => t.Id) : 0;
            int newTenantId = maxId + 1;

            var tenant = new Tenant
            {
                TenantCode = $"TEN-{newTenantId:D3}",
                CompanyName = dto.CompanyName.Trim(),
                TradeTitle = dto.CompanyName.Trim(),
                TaxNumber = string.IsNullOrWhiteSpace(dto.TaxNumber) ? "1234567890" : dto.TaxNumber.Trim(),
                TaxOffice = string.IsNullOrWhiteSpace(dto.TaxOffice) ? "Vergi Dairesi" : dto.TaxOffice.Trim(),
                Sector = string.IsNullOrWhiteSpace(dto.Sector) ? "Genel Ticaret" : dto.Sector.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? "0500 000 0000" : dto.Phone.Trim(),
                Email = dto.AdminEmail.Trim(),
                City = string.IsNullOrWhiteSpace(dto.City) ? "İstanbul" : dto.City.Trim(),
                Address = "Merkez Adres",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Tenants.AddAsync(tenant);
            await _context.SaveChangesAsync();

            // 2. Müşteri Şirket Yöneticisi Hesabı Aç (Tenant Admin)
            var adminUser = new User
            {
                TenantId = tenant.Id,
                Email = dto.AdminEmail.Trim(),
                PasswordHash = Application.Services.AuthService.HashPassword(dto.AdminPassword.Trim()),
                FullName = string.IsNullOrWhiteSpace(dto.AdminFullName) ? (dto.CompanyName + " Yöneticisi") : dto.AdminFullName.Trim(),
                Role = UserRole.Admin,
                Title = dto.CompanyName + " Yöneticisi (Tenant Admin)",
                PhoneNumber = string.IsNullOrWhiteSpace(dto.Phone) ? "0500 000 0000" : dto.Phone.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Users.AddAsync(adminUser);
            await _context.SaveChangesAsync();

            // 3. Yeni Şirkete Özel Temiz Başlangıç Yapılandırması (Boş Veritabanı + İlk Varsayılan Kasa/Depo/Kategori)
            var defaultWarehouse = new Warehouse
            {
                TenantId = tenant.Id,
                WarehouseCode = "DEP-001",
                Name = "Ana Depo",
                IsDefault = true,
                Status = WarehouseStatus.Active,
                CreatedDate = DateTime.UtcNow
            };

            var defaultCategory = new Category
            {
                TenantId = tenant.Id,
                Name = "Genel Ürünler",
                Description = "İlk Varsayılan Ürün Kategorisi",
                CreatedDate = DateTime.UtcNow
            };

            var defaultCash = new CashAccount
            {
                TenantId = tenant.Id,
                AccountCode = "KSA-001",
                AccountName = "Merkez Kasa",
                AccountType = CashAccountType.Kasa,
                Balance = 0,
                Status = CashAccountStatus.Active,
                CreatedDate = DateTime.UtcNow
            };

            var defaultBank = new CashAccount
            {
                TenantId = tenant.Id,
                AccountCode = "BNK-001",
                AccountName = "Şirket Banka Hesabı",
                AccountType = CashAccountType.Banka,
                BankName = "T.C. Ziraat Bankası",
                Balance = 0,
                Status = CashAccountStatus.Active,
                CreatedDate = DateTime.UtcNow
            };

            await _context.Warehouses.AddAsync(defaultWarehouse);
            await _context.Categories.AddAsync(defaultCategory);
            await _context.CashAccounts.AddRangeAsync(defaultCash, defaultBank);
            await _context.SaveChangesAsync();

            // 4. Otomatik Oturum Aç ve Doğrudan Paneline Yönlendir!
            var initials = string.Join("", adminUser.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToUpper();
            if (string.IsNullOrWhiteSpace(initials)) initials = "AD";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, adminUser.Id.ToString()),
                new(ClaimTypes.Email, adminUser.Email),
                new(ClaimTypes.Name, adminUser.FullName),
                new(ClaimTypes.Role, adminUser.Role.ToString()),
                new("TenantId", adminUser.TenantId.ToString()),
                new("CompanyName", tenant.CompanyName),
                new("Title", adminUser.Title ?? "Firma Yöneticisi"),
                new("RoleText", "Firma Admin"),
                new("RoleBadgeClass", "bg-primary"),
                new("Initials", initials)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });

            HttpContext.Session.SetInt32("ActiveTenantId", tenant.Id);

            return Json(new
            {
                success = true,
                message = $"Tebrikler! {tenant.CompanyName} şirket hesabınız başarıyla açıldı! Yönlendiriliyorsunuz...",
                redirectUrl = Url.Action("Index", "Home")
            });
        }
        catch (Exception ex)
        {
            var msg = ex.InnerException?.Message ?? ex.Message;
            return Json(new { success = false, message = "Kayıt işlemi sırasında hata oluştu: " + msg });
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            return RedirectToLocal(returnUrl);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginDto dto, [FromQuery] string? returnUrl = null)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return Json(new { success = false, message = "Lütfen e-posta adresi ve şifrenizi giriniz." });
        }

        var (success, message, user) = await _authService.ValidateUserAsync(dto.Email, dto.Password);
        if (!success || user == null)
        {
            return Json(new { success = false, message });
        }

        var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        if (dbUser != null)
        {
            dbUser.LastLoginDate = DateTime.Now;
            _context.Users.Update(dbUser);
            await _context.SaveChangesAsync();
        }

        var tenantObj = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == user.TenantId);
        var companyName = tenantObj?.CompanyName ?? user.FullName;

        var lastLoginStr = dbUser?.LastLoginDate.HasValue == true
            ? dbUser.LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm")
            : DateTime.Now.ToString("dd.MM.yyyy HH:mm");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new("CompanyName", companyName),
            new("Title", user.Title ?? user.RoleText),
            new("RoleText", user.RoleText),
            new("RoleBadgeClass", user.RoleBadgeClass),
            new("Initials", user.Initials),
            new("LastLogin", lastLoginStr),
            new("AllowedModules", string.IsNullOrWhiteSpace(dbUser?.AllowedModules) ? "Products,SalesProducts,Pos,Categories,StockTransactions,Warehouses,Invoices,CashAccounts,IncomeExpenses,Employees,SalaryPayments,Shifts,Reports,Customers,Users" : dbUser.AllowedModules)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = dto.RememberMe,
            ExpiresUtc = dto.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Active Tenant Session
        HttpContext.Session.SetInt32("ActiveTenantId", user.TenantId);

        var redirectUrl = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) 
            ? returnUrl 
            : (user.Role == UserRole.SuperAdmin ? Url.Action("Index", "Admin") : Url.Action("Index", "Home"));

        return Json(new { success = true, message = "Giriş başarılı! Yönlendiriliyorsunuz...", redirectUrl });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult SwitchTenant([FromBody] SwitchTenantRequest req)
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role != UserRole.SuperAdmin.ToString())
        {
            return Json(new { success = false, message = "Sadece Super Admin şirketler arası aktif görünümü değiştirebilir." });
        }

        HttpContext.Session.SetInt32("ActiveTenantId", req.TenantId);
        return Json(new { success = true, message = $"Aktif şirket görünümü değiştirildi." });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Json(new { success = true, redirectUrl = Url.Action("Login", "Account") });
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.CurrentPassword) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            return Json(new { success = false, message = "Lütfen tüm şifre alanlarını doldurunuz." });
        }

        if (dto.NewPassword != dto.ConfirmPassword)
        {
            return Json(new { success = false, message = "Yeni şifreler uyuşmuyor." });
        }

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Json(new { success = false, message = "Kullanıcı oturumu doğrulanamadı." });
        }

        var (success, message) = await _authService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword);
        return Json(new { success, message });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return RedirectToAction("Login");

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return RedirectToAction("Login");

        return View(user);
    }

    [HttpPost]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Json(new { success = false, message = "Oturum geçersiz." });

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);
        if (user == null) return Json(new { success = false, message = "Kullanıcı bulunamadı." });

        if (!string.IsNullOrWhiteSpace(req.FullName)) user.FullName = req.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(req.PhoneNumber)) user.PhoneNumber = req.PhoneNumber.Trim();
        if (!string.IsNullOrWhiteSpace(req.Title)) user.Title = req.Title.Trim();

        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = "Profil bilgileriniz başarıyla güncellendi!" });
    }

    [HttpGet]
    [Authorize]
    public IActionResult Users()
    {
        return View();
    }

    [HttpGet("api/[controller]/team-users")]
    [Authorize]
    public async Task<IActionResult> GetTeamUsers()
    {
        var tenantIdClaim = User.FindFirstValue("TenantId");
        int.TryParse(tenantIdClaim, out int tenantId);

        var users = await _context.Users
            .Where(u => u.TenantId == tenantId && !u.IsDeleted)
            .OrderByDescending(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                Role = u.Role.ToString(),
                RoleText = u.Role == UserRole.Admin ? "Şirket Yöneticisi" : (u.Role == UserRole.Accountant ? "Muhasebeci" : "Personel"),
                u.Title,
                u.PhoneNumber,
                u.IsActive,
                AllowedModules = string.IsNullOrWhiteSpace(u.AllowedModules) ? "Products,SalesProducts,Pos,Categories,StockTransactions,Warehouses,Invoices,CashAccounts,IncomeExpenses,Employees,SalaryPayments,Shifts,Reports,Customers,Users" : u.AllowedModules,
                FormattedLastLogin = u.LastLoginDate.HasValue ? u.LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm") : "Giriş Yok"
            })
            .ToListAsync();

        return Json(users);
    }

    [HttpPost("api/[controller]/create-team-user")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateTeamUser([FromBody] CreateUserDto dto)
    {
        var tenantIdClaim = User.FindFirstValue("TenantId");
        int.TryParse(tenantIdClaim, out int tenantId);
        dto.TenantId = tenantId;

        var (success, message, user) = await _authService.CreateUserAsync(dto);
        return Json(new { success, message, user });
    }

    [HttpPost("api/[controller]/update-team-user-permissions")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateTeamUserPermissions([FromBody] UpdateUserPermissionsRequest req)
    {
        var tenantIdClaim = User.FindFirstValue("TenantId");
        int.TryParse(tenantIdClaim, out int tenantId);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && u.TenantId == tenantId && !u.IsDeleted);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        user.AllowedModules = req.AllowedModules ?? "";
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return Json(new { success = true, message = $"{user.FullName} kullanıcısının erişim izinleri başarıyla güncellendi!" });
    }

    [HttpPost("api/[controller]/toggle-team-user-status/{id}")]
    [Authorize]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> ToggleTeamUserStatus(int id)
    {
        var (success, message) = await _authService.ToggleUserStatusAsync(id);
        return Json(new { success, message });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}

public class SwitchTenantRequest
{
    public int TenantId { get; set; }
}

public class UpdateProfileRequest
{
    public string? FullName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Title { get; set; }
}

public class UpdateUserPermissionsRequest
{
    public int UserId { get; set; }
    public string? AllowedModules { get; set; }
}
