using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Repositories;

public class CashAccountRepository : GenericRepository<CashAccount>, ICashAccountRepository
{
    public CashAccountRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<(IEnumerable<CashAccount> Items, int TotalCount)> GetAccountsPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, int? type, int? status, string? sortBy, bool ascending)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(x => x.AccountName.Contains(searchTerm) || x.AccountCode.Contains(searchTerm));

        if (type.HasValue)
            query = query.Where(x => x.AccountType == (CashAccountType)type.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == (CashAccountStatus)status.Value);

        var totalCount = await query.CountAsync();

        query = sortBy switch
        {
            "AccountName" => ascending ? query.OrderBy(x => x.AccountName) : query.OrderByDescending(x => x.AccountName),
            "Balance" => ascending ? query.OrderBy(x => x.Balance) : query.OrderByDescending(x => x.Balance),
            _ => query.OrderByDescending(x => x.Id)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<CashAccount?> GetByCodeAsync(string code)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.AccountCode == code);
    }

    public async Task<bool> IsCodeUniqueAsync(string code, int? id = null)
    {
        var query = _dbSet.Where(x => x.AccountCode == code);
        if (id.HasValue) query = query.Where(x => x.Id != id.Value);
        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<CashAccount>> GetAllActiveAsync()
    {
        return await _dbSet.Where(x => x.Status == CashAccountStatus.Active).ToListAsync();
    }

    public async Task<(IEnumerable<CashTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
        int pageNumber, int pageSize, int? cashAccountId, int? transactionType, int? customerId, 
        DateTime? startDate, DateTime? endDate, string? searchTerm, string? sortBy, bool ascending)
    {
        var query = _context.Set<CashTransaction>().Include(x => x.CashAccount).Include(x => x.Customer).AsQueryable();

        if (cashAccountId.HasValue) query = query.Where(x => x.CashAccountId == cashAccountId.Value);
        if (transactionType.HasValue) query = query.Where(x => x.TransactionType == (CashTransactionType)transactionType.Value);
        if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
        if (startDate.HasValue) query = query.Where(x => x.TransactionDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(x => x.TransactionDate <= endDate.Value);
        if (!string.IsNullOrWhiteSpace(searchTerm)) query = query.Where(x => x.TransactionCode.Contains(searchTerm) || (x.Description != null && x.Description.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        query = query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id);
        
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, totalCount);
    }

    public async Task<CashDashboardStats> GetDashboardStatsAsync()
    {
        var today = DateTime.UtcNow.Date;
        var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
        
        var accounts = await _dbSet.ToListAsync();
        var totalCash = accounts.Where(x => x.AccountType == CashAccountType.Kasa).Sum(x => x.Balance);
        var totalBank = accounts.Where(x => x.AccountType == CashAccountType.Banka).Sum(x => x.Balance);
        var totalPos = accounts.Where(x => x.AccountType == CashAccountType.POS || x.AccountType == CashAccountType.KrediKarti).Sum(x => x.Balance);
        
        var cashCount = accounts.Count(x => x.AccountType == CashAccountType.Kasa && x.Status == CashAccountStatus.Active);
        var bankCount = accounts.Count(x => x.AccountType == CashAccountType.Banka && x.Status == CashAccountStatus.Active);
        var posCount = accounts.Count(x => (x.AccountType == CashAccountType.POS || x.AccountType == CashAccountType.KrediKarti) && x.Status == CashAccountStatus.Active);
        
        var transactions = await _context.Set<CashTransaction>().Where(x => x.TransactionDate >= firstDayOfMonth).ToListAsync();
        
        var monthColl = transactions.Where(x => x.TransactionType == CashTransactionType.Collection || x.TransactionType == CashTransactionType.BankTransferIn).Sum(x => x.Amount);
        var monthPay = transactions.Where(x => x.TransactionType == CashTransactionType.Payment || x.TransactionType == CashTransactionType.BankTransferOut).Sum(x => x.Amount);
        
        var todayTx = transactions.Where(x => x.TransactionDate >= today).ToList();
        var todayColl = todayTx.Where(x => x.TransactionType == CashTransactionType.Collection || x.TransactionType == CashTransactionType.BankTransferIn).Sum(x => x.Amount);
        var todayPay = todayTx.Where(x => x.TransactionType == CashTransactionType.Payment || x.TransactionType == CashTransactionType.BankTransferOut).Sum(x => x.Amount);
        
        return new CashDashboardStats(totalCash, totalBank, totalPos, cashCount, bankCount, posCount, todayColl, todayPay, monthColl, monthPay);
    }

    public async Task AddTransactionAsync(CashTransaction transaction)
    {
        await _context.Set<CashTransaction>().AddAsync(transaction);
    }
}
