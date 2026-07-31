using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string Message, UserDto? User)> ValidateUserAsync(string email, string password);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto?> GetUserByIdAsync(int id);
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);

    // Admin User Management & Stats
    Task<(IEnumerable<UserDto> Items, int TotalCount)> GetUsersPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, UserRole? role = null, bool? isActive = null);
    Task<(bool Success, string Message, UserDto? User)> CreateUserAsync(CreateUserDto dto);
    Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserDto dto);
    Task<(bool Success, string Message)> ToggleUserStatusAsync(int userId);
    Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword);
    Task<(bool Success, string Message)> DeleteUserAsync(int userId);
    Task<AdminDashboardStatsDto> GetAdminStatsAsync();
}
