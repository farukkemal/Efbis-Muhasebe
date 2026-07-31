using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class StockTransactionService : IStockTransactionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StockTransactionService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<StockTransactionDto> Items, int TotalCount)> GetPagedAsync(StockTransactionFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.StockTransactions.GetPagedAsync(
            filter.Page,
            filter.PageSize,
            filter.SearchTerm,
            filter.TransactionType,
            filter.ProductId,
            filter.WarehouseId,
            filter.StartDate,
            filter.EndDate,
            filter.SortBy,
            filter.Ascending
        );

        return (_mapper.Map<IEnumerable<StockTransactionDto>>(items), totalCount);
    }

    public async Task<StockTransactionDto?> GetByIdAsync(int id)
    {
        var item = await _unitOfWork.StockTransactions.GetByIdAsync(id);
        return item == null ? null : _mapper.Map<StockTransactionDto>(item);
    }

    public async Task<StockTransactionDto> CreateAsync(CreateStockTransactionDto dto)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
        if (product == null)
            throw new Exception("Ürün bulunamadı.");

        // KESİNLİKLE FATURA ZORUNLULUĞU KURALI (Stok Girişi için sisteme kayıtlı fatura şarttır)
        if (dto.TransactionType == TransactionType.StockIn)
        {
            if (string.IsNullOrWhiteSpace(dto.ReferenceNo))
            {
                throw new Exception("Sisteme stok girişi yapabilmek için ürünün geldiğine dair fatura girilmiş olması zorunludur! Lütfen geçerli bir fatura numarası seçiniz.");
            }

            var refNo = dto.ReferenceNo.Trim();
            var invoiceExists = await _unitOfWork.Invoices.GetByInvoiceNumberAsync(refNo);
            if (invoiceExists == null)
            {
                throw new Exception($"'{refNo}' numaralı fatura sisteme kayıtlı değildir! Faturası sisteme işlenmemiş ürünün stok girişi yapılamaz. Lütfen önce 'Faturalar' sayfasından faturayı sisteme giriniz.");
            }
        }

        // Check stock availability for outputs
        if ((dto.TransactionType == TransactionType.StockOut || dto.TransactionType == TransactionType.Waste) && product.CurrentStock < dto.Quantity)
        {
            throw new Exception("Yetersiz stok miktarı.");
        }

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            var transaction = _mapper.Map<StockTransaction>(dto);
            transaction.TotalAmount = dto.Quantity * dto.UnitPrice;
            transaction.TransactionCode = await GenerateTransactionCodeAsync(dto.TransactionType);

            // Update Product Stock
            if (dto.TransactionType == TransactionType.StockIn || dto.TransactionType == TransactionType.Count)
            {
                product.CurrentStock += dto.Quantity;
            }
            else if (dto.TransactionType == TransactionType.StockOut || dto.TransactionType == TransactionType.Waste)
            {
                product.CurrentStock -= dto.Quantity;
            }
            
            _unitOfWork.Products.Update(product);
            await _unitOfWork.StockTransactions.AddAsync(transaction);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var savedTransaction = await _unitOfWork.StockTransactions.GetByIdAsync(transaction.Id);
            return _mapper.Map<StockTransactionDto>(savedTransaction);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<StockTransactionDashboardDto> GetDashboardAsync()
    {
        var stats = await _unitOfWork.StockTransactions.GetDashboardStatsAsync();
        return new StockTransactionDashboardDto
        {
            TotalIn = stats.TotalIn,
            TotalOut = stats.TotalOut,
            TotalTransfer = stats.TotalTransfer,
            TotalWaste = stats.TotalWaste,
            TodayTransactions = stats.TodayTransactions,
            MonthlyTransactions = stats.MonthlyTransactions
        };
    }

    private async Task<string> GenerateTransactionCodeAsync(TransactionType type)
    {
        var prefix = type switch
        {
            TransactionType.StockIn => "GRS",
            TransactionType.StockOut => "CKS",
            TransactionType.Transfer => "TRF",
            TransactionType.Count => "SYM",
            TransactionType.Waste => "FRE",
            _ => "STK"
        };
        
        var randomSuffix = new Random().Next(1000, 9999);
        var timestamp = DateTime.UtcNow.ToString("yyMMdd");
        return $"{prefix}-{timestamp}-{randomSuffix}";
    }
}
