using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public AuthService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(bool Success, string Message, UserDto? User)> ValidateUserAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (false, "E-posta ve şifre zorunludur.", null);

        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        if (user == null)
            return (false, "E-posta adresi veya şifre hatalı.", null);

        if (!user.IsActive)
            return (false, "Kullanıcı hesabınız pasif durumdadır. Yöneticiniz ile iletişime geçiniz.", null);

        if (!VerifyPasswordHash(password, user.PasswordHash))
            return (false, "E-posta adresi veya şifre hatalı.", null);

        user.LastLoginDate = DateTime.Now;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        var dto = _mapper.Map<UserDto>(user);
        return (true, "Giriş başarılı.", dto);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _unitOfWork.Users.GetByEmailAsync(email);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        if (!VerifyPasswordHash(currentPassword, user.PasswordHash))
            return (false, "Mevcut şifreniz hatalı.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Yeni şifre en az 6 karakter olmalıdır.");

        user.PasswordHash = HashPassword(newPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Şifreniz başarıyla güncellendi.");
    }

    // ─── Admin User Management ──────────────────────────────────────────────────
    public async Task<(IEnumerable<UserDto> Items, int TotalCount)> GetUsersPagedAsync(
        int pageNumber, int pageSize, string? searchTerm = null, UserRole? role = null, bool? isActive = null)
    {
        var (items, totalCount) = await _unitOfWork.Users.GetPagedAsync(pageNumber, pageSize, searchTerm, role, isActive);
        var dtos = _mapper.Map<IEnumerable<UserDto>>(items);
        return (dtos, totalCount);
    }

    public async Task<(bool Success, string Message, UserDto? User)> CreateUserAsync(CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.FullName))
            return (false, "E-posta, Ad Soyad ve Şifre zorunludur.", null);

        if (!await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email))
            return (false, "Bu e-posta adresi ile kayıtlı başka bir kullanıcı bulunmaktadır.", null);

        var user = new User
        {
            Email = dto.Email.Trim(),
            PasswordHash = HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Role = dto.Role,
            Title = dto.Title?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            IsActive = dto.IsActive,
            AllowedModules = string.IsNullOrWhiteSpace(dto.AllowedModules) ? "Products,SalesProducts,Pos,Categories,StockTransactions,Warehouses,Invoices,CashAccounts,IncomeExpenses,Employees,SalaryPayments,Shifts,Reports,Customers,Users" : dto.AllowedModules,
            CreatedDate = DateTime.Now
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var resultDto = _mapper.Map<UserDto>(user);
        return (true, "Yeni kullanıcı başarıyla oluşturuldu.", resultDto);
    }

    public async Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(dto.Id);
        if (user == null)
            return (false, "Güncellenecek kullanıcı bulunamadı.");

        if (!await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email, dto.Id))
            return (false, "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");

        user.Email = dto.Email.Trim();
        user.FullName = dto.FullName.Trim();
        user.Role = dto.Role;
        user.Title = dto.Title?.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.IsActive = dto.IsActive;
        if (!string.IsNullOrWhiteSpace(dto.AllowedModules))
        {
            user.AllowedModules = dto.AllowedModules;
        }

        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Kullanıcı bilgileri başarıyla güncellendi.");
    }

    public async Task<(bool Success, string Message)> ToggleUserStatusAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        user.IsActive = !user.IsActive;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        string statusText = user.IsActive ? "aktif" : "pasif";
        return (true, $"Kullanıcı hesabı {statusText} duruma getirildi.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            return (false, "Şifre en az 6 karakter olmalıdır.");

        user.PasswordHash = HashPassword(newPassword);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"{user.FullName} isimli kullanıcının şifresi sıfırlandı.");
    }

    public async Task<(bool Success, string Message)> DeleteUserAsync(int userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Kullanıcı bulunamadı.");

        _unitOfWork.Users.Remove(user);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Kullanıcı hesabı silindi.");
    }

    public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
    {
        var (users, totalUsers) = await _unitOfWork.Users.GetPagedAsync(1, 1000);
        var userList = users.ToList();

        var (products, totalProducts) = await _unitOfWork.Products.GetPagedAsync(1, 1);
        var (customers, totalCustomers) = await _unitOfWork.Customers.GetPagedAsync(1, 1);
        var (invoices, totalInvoices) = await _unitOfWork.Invoices.GetPagedAsync(1, 1);
        var (whs, totalWhs) = await _unitOfWork.Warehouses.GetPagedAsync(1, 1);
        var (emps, totalEmps) = await _unitOfWork.Employees.GetPagedAsync(1, 1);

        return new AdminDashboardStatsDto
        {
            TotalUsers = totalUsers,
            ActiveUsers = userList.Count(u => u.IsActive),
            AdminUsers = userList.Count(u => u.Role == UserRole.Admin),
            TotalProducts = totalProducts,
            TotalCustomers = totalCustomers,
            TotalInvoices = totalInvoices,
            TotalWarehouses = totalWhs,
            TotalEmployees = totalEmps,
            SystemStatus = "Sağlıklı (Healthy 🟢)",
            DbProvider = "EF Core SQL Server / LocalDB"
        };
    }

    // ─── Password Hashing Helpers ──────────────────────────────────────────────
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "EfbisMuhasebeSalt2026"));
        return Convert.ToBase64String(hashedBytes);
    }

    public static bool VerifyPasswordHash(string password, string storedHash)
    {
        var computedHash = HashPassword(password);
        return computedHash == storedHash;
    }
}
