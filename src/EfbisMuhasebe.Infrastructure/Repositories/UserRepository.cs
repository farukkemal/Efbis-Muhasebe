using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalizedEmail = email.Trim().ToLower();
        return await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail);
    }

    public async Task<bool> IsEmailUniqueAsync(string email, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var normalizedEmail = email.Trim().ToLower();

        var query = _dbSet.IgnoreQueryFilters().Where(u => u.Email.ToLower() == normalizedEmail);
        if (excludeId.HasValue)
            query = query.Where(u => u.Id != excludeId.Value);

        return !await query.AnyAsync();
    }

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize,
        string? searchTerm = null,
        UserRole? role = null,
        bool? isActive = null)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchTerm) ||
                u.FullName.ToLower().Contains(searchTerm) ||
                (u.Title != null && u.Title.ToLower().Contains(searchTerm))
            );
        }

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(u => u.CreatedDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
