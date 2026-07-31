using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface ICashAccountService
{
    Task<PagedResultDto<CashAccountDto>> GetAccountsPagedAsync(CashAccountFilterDto filter);
    Task<CashAccountDto?> GetAccountByIdAsync(int id);
    Task<IEnumerable<CashAccountDto>> GetActiveAccountsAsync();
    Task<(bool Success, string Message, int? Id)> CreateAccountAsync(CreateCashAccountDto dto);
    Task<(bool Success, string Message)> UpdateAccountAsync(UpdateCashAccountDto dto);
    Task<(bool Success, string Message)> DeleteAccountAsync(int id);

    Task<PagedResultDto<CashTransactionDto>> GetTransactionsPagedAsync(CashTransactionFilterDto filter);
    Task<(bool Success, string Message, int? Id)> CreateTransactionAsync(CreateCashTransactionDto dto);
    
    Task<CashDashboardDto> GetDashboardAsync();
}
