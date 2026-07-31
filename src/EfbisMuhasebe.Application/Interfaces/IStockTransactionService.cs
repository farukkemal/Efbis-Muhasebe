using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IStockTransactionService
{
    Task<(IEnumerable<StockTransactionDto> Items, int TotalCount)> GetPagedAsync(StockTransactionFilterDto filter);
    Task<StockTransactionDto?> GetByIdAsync(int id);
    Task<StockTransactionDto> CreateAsync(CreateStockTransactionDto dto);
    Task<StockTransactionDashboardDto> GetDashboardAsync();
}
