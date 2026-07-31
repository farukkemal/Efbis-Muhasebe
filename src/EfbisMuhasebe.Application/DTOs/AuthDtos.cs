using System;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = false;
}

public class UserDto
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleText => Role switch
    {
        UserRole.SuperAdmin => "👑 Super Admin (Sistem Sahibi)",
        UserRole.Admin => "👔 Tenant Admin (Şirket Yöneticisi)",
        UserRole.StoreManager => "Mağaza / Şube Müdürü",
        UserRole.Accountant => "Kıdemli Muhasebeci",
        UserRole.Staff => "🧑‍💼 Tenant User (Şirket Personeli)",
        _ => "Kullanıcı"
    };
    public string RoleBadgeClass => Role switch
    {
        UserRole.SuperAdmin => "bg-purple text-white",
        UserRole.Admin => "bg-danger",
        UserRole.StoreManager => "bg-primary",
        UserRole.Accountant => "bg-success",
        _ => "bg-secondary"
    };
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string FormattedLastLogin => LastLoginDate.HasValue ? LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm") : "Henüz Giriş Yapmadı";
    public string AllowedModules { get; set; } = "Products,SalesProducts,Pos,Categories,StockTransactions,Warehouses,Invoices,CashAccounts,IncomeExpenses,Employees,SalaryPayments,Shifts,Reports,Customers,Users";
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FullName)) return "US";
            var parts = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0][..1].ToUpper();
            return (parts[0][..1] + parts[^1][..1]).ToUpper();
        }
    }
}

public class CreateUserDto
{
    public int TenantId { get; set; } = 1;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public string? AllowedModules { get; set; }
}

public class UpdateUserDto
{
    public int Id { get; set; }
    public int TenantId { get; set; } = 1;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public string? AllowedModules { get; set; }
}

public class ResetUserPasswordDto
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminDashboardStatsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int AdminUsers { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int TotalInvoices { get; set; }
    public int TotalWarehouses { get; set; }
    public int TotalEmployees { get; set; }
    public string SystemStatus { get; set; } = "Sağlıklı (Healthy 🟢)";
    public string DbProvider { get; set; } = "EF Core SQL Server / LocalDB";
}

public class AuditLogDto
{
    public string Timestamp { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Level { get; set; } = "INFO";
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string IpAddress { get; set; } = "127.0.0.1";
}
