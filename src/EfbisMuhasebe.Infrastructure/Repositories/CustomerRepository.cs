using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        CustomerType? customerType = null,
        CustomerStatus? status = null,
        BalanceStatus? balanceStatus = null,
        string? city = null,
        string? sortBy = null,
        bool ascending = true)
    {
        var query = _dbSet.AsQueryable();

        // Arama (Kod, Unvan, Yetkili, Telefon, Vergi No, Şehir)
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                c.CustomerCode.ToLower().Contains(term) ||
                c.Title.ToLower().Contains(term) ||
                (c.AuthorizedPerson != null && c.AuthorizedPerson.ToLower().Contains(term)) ||
                (c.Phone != null && c.Phone.Contains(term)) ||
                (c.TaxNumber != null && c.TaxNumber.Contains(term)) ||
                (c.City != null && c.City.ToLower().Contains(term)));
        }

        // Cari türü filtresi
        if (customerType.HasValue)
            query = query.Where(c => c.CustomerType == customerType.Value);

        // Durum filtresi
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);

        // Şehir filtresi
        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(c => c.City != null && c.City.ToLower() == city.ToLower());

        // Bakiye durumu filtresi
        if (balanceStatus.HasValue)
        {
            query = balanceStatus.Value switch
            {
                BalanceStatus.Debit  => query.Where(c => c.Balance > 0),
                BalanceStatus.Credit => query.Where(c => c.Balance < 0),
                BalanceStatus.Zero   => query.Where(c => c.Balance == 0),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();

        // Sıralama
        query = (sortBy?.ToLower()) switch
        {
            "title"        => ascending ? query.OrderBy(c => c.Title)        : query.OrderByDescending(c => c.Title),
            "customercode" => ascending ? query.OrderBy(c => c.CustomerCode) : query.OrderByDescending(c => c.CustomerCode),
            "balance"      => ascending ? query.OrderBy(c => c.Balance)      : query.OrderByDescending(c => c.Balance),
            "city"         => ascending ? query.OrderBy(c => c.City)         : query.OrderByDescending(c => c.City),
            "createddate"  => ascending ? query.OrderBy(c => c.CreatedDate)    : query.OrderByDescending(c => c.CreatedDate),
            _              => query.OrderBy(c => c.Title)
        };

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Customer?> GetByCodeAsync(string customerCode)
        => await _dbSet.FirstOrDefaultAsync(c => c.CustomerCode == customerCode);

    public async Task<bool> IsCodeUniqueAsync(string customerCode, int? excludeId = null)
    {
        var query = _dbSet.Where(c => c.CustomerCode == customerCode);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return !await query.AnyAsync();
    }

    public async Task<CustomerDashboardStats> GetDashboardStatsAsync()
    {
        var all = await _dbSet.ToListAsync();

        var receivables = all.Where(c => c.Balance > 0).Sum(c => c.Balance);
        var payables = Math.Abs(all.Where(c => c.Balance < 0).Sum(c => c.Balance));

        return new CustomerDashboardStats(
            TotalCustomers:   all.Count,
            CustomersOnly:    all.Count(c => c.CustomerType == CustomerType.Customer),
            SuppliersOnly:    all.Count(c => c.CustomerType == CustomerType.Supplier),
            BothCount:        all.Count(c => c.CustomerType == CustomerType.Both),
            PassiveCount:     all.Count(c => c.Status == CustomerStatus.Passive),
            TotalReceivables: receivables,
            TotalPayables:    payables,
            NetBalance:       receivables - payables
        );
    }

    public async Task<int> BulkUpdateStatusAsync(IEnumerable<int> ids, CustomerStatus status)
    {
        var idList = ids.ToList();
        var customers = await _dbSet.Where(c => idList.Contains(c.Id)).ToListAsync();

        foreach (var c in customers)
        {
            c.Status = status;
            c.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return customers.Count;
    }
}
