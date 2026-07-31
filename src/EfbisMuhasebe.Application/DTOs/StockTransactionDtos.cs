using System;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class StockTransactionDto
{
    public int Id { get; set; }
    public string TransactionCode { get; set; } = string.Empty;
    public TransactionType TransactionType { get; set; }
    public string TransactionTypeText { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerTitle { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; }
    public string FormattedDate { get; set; } = string.Empty;
    public string? ReferenceNo { get; set; }
}

public class CreateStockTransactionDto
{
    public TransactionType TransactionType { get; set; }
    public int ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public int? CustomerId { get; set; }
    public string? Description { get; set; }
    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? ReferenceNo { get; set; }
}

public class StockTransactionFilterDto
{
    public string? SearchTerm { get; set; }
    public TransactionType? TransactionType { get; set; }
    public int? ProductId { get; set; }
    public int? WarehouseId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    private int _page = 1;
    public int Page { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageNumber { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageSize { get; set; } = 15;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = false;
}

public class StockTransactionDashboardDto
{
    public int TotalIn { get; set; }
    public int TotalOut { get; set; }
    public int TotalTransfer { get; set; }
    public int TotalWaste { get; set; }
    public int TodayTransactions { get; set; }
    public int MonthlyTransactions { get; set; }
}
