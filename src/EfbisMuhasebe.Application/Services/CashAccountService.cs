using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.Services;

public class CashAccountService : ICashAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CashAccountService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<CashAccountDto>> GetAccountsPagedAsync(CashAccountFilterDto filter)
    {
        var result = await _unitOfWork.CashAccounts.GetAccountsPagedAsync(
            filter.PageNumber, filter.PageSize, filter.SearchTerm, filter.Type, filter.Status, filter.SortBy, filter.Ascending);
            
        var dtos = _mapper.Map<List<CashAccountDto>>(result.Items);
        return new PagedResultDto<CashAccountDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<CashAccountDto?> GetAccountByIdAsync(int id)
    {
        var account = await _unitOfWork.CashAccounts.GetByIdAsync(id);
        return account == null ? null : _mapper.Map<CashAccountDto>(account);
    }

    public async Task<IEnumerable<CashAccountDto>> GetActiveAccountsAsync()
    {
        var accounts = await _unitOfWork.CashAccounts.GetAllActiveAsync();
        return _mapper.Map<IEnumerable<CashAccountDto>>(accounts);
    }

    public async Task<(bool Success, string Message, int? Id)> CreateAccountAsync(CreateCashAccountDto dto)
    {
        if (!await _unitOfWork.CashAccounts.IsCodeUniqueAsync(dto.AccountCode))
            return (false, "Hesap kodu zaten kullanılıyor.", null);

        var account = _mapper.Map<CashAccount>(dto);
        account.Balance = dto.InitialBalance;
        
        await _unitOfWork.CashAccounts.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        
        return (true, "Hesap başarıyla oluşturuldu.", account.Id);
    }

    public async Task<(bool Success, string Message)> UpdateAccountAsync(UpdateCashAccountDto dto)
    {
        var account = await _unitOfWork.CashAccounts.GetByIdAsync(dto.Id);
        if (account == null) return (false, "Hesap bulunamadı.");

        if (!await _unitOfWork.CashAccounts.IsCodeUniqueAsync(dto.AccountCode, dto.Id))
            return (false, "Hesap kodu zaten kullanılıyor.");

        _mapper.Map(dto, account);
        _unitOfWork.CashAccounts.Update(account);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Hesap başarıyla güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAccountAsync(int id)
    {
        var account = await _unitOfWork.CashAccounts.GetByIdAsync(id);
        if (account == null) return (false, "Hesap bulunamadı.");

        if (account.Balance != 0)
            return (false, "Bakiyesi olan hesap silinemez.");

        account.IsDeleted = true;
        _unitOfWork.CashAccounts.Update(account);
        await _unitOfWork.SaveChangesAsync();

        return (true, "Hesap başarıyla silindi.");
    }

    public async Task<PagedResultDto<CashTransactionDto>> GetTransactionsPagedAsync(CashTransactionFilterDto filter)
    {
        var result = await _unitOfWork.CashAccounts.GetTransactionsPagedAsync(
            filter.PageNumber, filter.PageSize, filter.CashAccountId, filter.TransactionType, filter.CustomerId,
            filter.StartDate, filter.EndDate, filter.SearchTerm, filter.SortBy, filter.Ascending);

        var dtos = _mapper.Map<List<CashTransactionDto>>(result.Items);
        return new PagedResultDto<CashTransactionDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<(bool Success, string Message, int? Id)> CreateTransactionAsync(CreateCashTransactionDto dto)
    {
        var account = await _unitOfWork.CashAccounts.GetByIdAsync(dto.CashAccountId);
        if (account == null) return (false, "Kasa/Banka hesabı bulunamadı.", null);

        var tx = _mapper.Map<CashTransaction>(dto);
        tx.TransactionCode = $"ISL-{DateTime.Now:yyyyMMddHHmmssfff}";

        // İş mantığı
        if (dto.TransactionType == CashTransactionType.Collection || dto.TransactionType == CashTransactionType.BankTransferIn || dto.TransactionType == CashTransactionType.EFT)
        {
            account.Balance += dto.Amount;
            // Müşteri bakiyesi güncelle
            if (dto.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId.Value);
                if (customer != null)
                {
                    customer.Balance -= dto.Amount;
                    _unitOfWork.Customers.Update(customer);
                }
            }

            // Gelir & Gider kaydı oluştur (Gelir)
            await _unitOfWork.IncomeExpenses.AddAsync(new IncomeExpense
            {
                TransactionCode = $"GLR-{DateTime.Now:yyyyMMddHHmmss}",
                Type = IncomeExpenseType.Income,
                CategoryName = "Cari Tahsilat",
                Amount = dto.Amount,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "Cari Hesaptan Tahsilat" : dto.Description,
                TransactionDate = dto.TransactionDate,
                CashAccountId = dto.CashAccountId,
                CustomerId = dto.CustomerId
            });
        }
        else if (dto.TransactionType == CashTransactionType.Payment || dto.TransactionType == CashTransactionType.BankTransferOut)
        {
            account.Balance -= dto.Amount;
            if (dto.CustomerId.HasValue)
            {
                var customer = await _unitOfWork.Customers.GetByIdAsync(dto.CustomerId.Value);
                if (customer != null)
                {
                    customer.Balance += dto.Amount;
                    _unitOfWork.Customers.Update(customer);
                }
            }

            // Gelir & Gider kaydı oluştur (Gider)
            await _unitOfWork.IncomeExpenses.AddAsync(new IncomeExpense
            {
                TransactionCode = $"GDR-{DateTime.Now:yyyyMMddHHmmss}",
                Type = IncomeExpenseType.Expense,
                CategoryName = "Cari Ödeme / Tediye",
                Amount = dto.Amount,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? "Cari Hesaba Ödeme / Tediye" : dto.Description,
                TransactionDate = dto.TransactionDate,
                CashAccountId = dto.CashAccountId,
                CustomerId = dto.CustomerId
            });
        }
        else if (dto.TransactionType == CashTransactionType.Transfer && dto.TargetAccountId.HasValue)
        {
            var targetAccount = await _unitOfWork.CashAccounts.GetByIdAsync(dto.TargetAccountId.Value);
            if (targetAccount == null) return (false, "Hedef hesap bulunamadı.", null);
            
            account.Balance -= dto.Amount;
            targetAccount.Balance += dto.Amount;
            
            var tx2 = new CashTransaction
            {
                CashAccountId = dto.TargetAccountId.Value,
                TransactionType = CashTransactionType.Transfer,
                Amount = dto.Amount,
                TransactionCode = tx.TransactionCode + "-IN",
                Description = "Virman Giriş",
                TransactionDate = dto.TransactionDate
            };
            await _unitOfWork.CashAccounts.AddTransactionAsync(tx2);
            _unitOfWork.CashAccounts.Update(targetAccount);
        }

        await _unitOfWork.CashAccounts.AddTransactionAsync(tx);
        _unitOfWork.CashAccounts.Update(account);
        
        await _unitOfWork.SaveChangesAsync();
        return (true, "İşlem başarıyla kaydedildi.", tx.Id);
    }

    public async Task<CashDashboardDto> GetDashboardAsync()
    {
        var stats = await _unitOfWork.CashAccounts.GetDashboardStatsAsync();
        return new CashDashboardDto
        {
            TotalCashBalance = stats.TotalCashBalance,
            TotalBankBalance = stats.TotalBankBalance,
            TotalPosBalance = stats.TotalPosBalance,
            CashAccountCount = stats.CashAccountCount,
            BankAccountCount = stats.BankAccountCount,
            PosAccountCount = stats.PosAccountCount,
            TodayCollections = stats.TodayCollections,
            TodayPayments = stats.TodayPayments,
            MonthlyCollections = stats.MonthlyCollections,
            MonthlyPayments = stats.MonthlyPayments
        };
    }
}
