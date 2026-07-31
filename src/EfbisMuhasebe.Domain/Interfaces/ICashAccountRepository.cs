using EfbisMuhasebe.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace EfbisMuhasebe.Domain.Interfaces;

public record CashDashboardStats(
    decimal TotalCashBalance,
    decimal TotalBankBalance,
    decimal TotalPosBalance,
    int CashAccountCount,
    int BankAccountCount,
    int PosAccountCount,
    decimal TodayCollections,
    decimal TodayPayments,
    decimal MonthlyCollections,
    decimal MonthlyPayments
);

public interface ICashAccountRepository : IRepository<CashAccount>
{
    Task<(IEnumerable<CashAccount> Items, int TotalCount)> GetAccountsPagedAsync(
        int pageNumber, int pageSize, string? searchTerm, int? type, int? status, string? sortBy, bool ascending);
    Task<CashAccount?> GetByCodeAsync(string code);
    Task<bool> IsCodeUniqueAsync(string code, int? id = null);
    Task<IEnumerable<CashAccount>> GetAllActiveAsync();

    Task<(IEnumerable<CashTransaction> Items, int TotalCount)> GetTransactionsPagedAsync(
        int pageNumber, int pageSize, int? cashAccountId, int? transactionType, int? customerId, 
        DateTime? startDate, DateTime? endDate, string? searchTerm, string? sortBy, bool ascending);

    Task AddTransactionAsync(CashTransaction transaction);
        
    Task<CashDashboardStats> GetDashboardStatsAsync();
}
