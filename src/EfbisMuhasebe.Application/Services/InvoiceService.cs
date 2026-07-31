using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public InvoiceService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<InvoiceDto> Items, int TotalCount)> GetPagedAsync(InvoiceFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Invoices.GetPagedAsync(
            filter.Page, filter.PageSize, filter.SearchTerm, filter.InvoiceType, filter.Status, filter.CustomerId, filter.StartDate, filter.EndDate, filter.SortBy, filter.Ascending);
        
        return (_mapper.Map<IEnumerable<InvoiceDto>>(items), totalCount);
    }

    public async Task<InvoiceDetailDto?> GetByIdAsync(int id)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdWithItemsAsync(id);
        return invoice == null ? null : _mapper.Map<InvoiceDetailDto>(invoice);
    }

    public async Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            string number;
            if (!string.IsNullOrWhiteSpace(dto.InvoiceNumber))
            {
                number = dto.InvoiceNumber.Trim();
                var existing = await _unitOfWork.Invoices.GetByInvoiceNumberAsync(number);
                if (existing != null)
                {
                    throw new Exception($"'{number}' numaralı fatura veritabanında zaten kayıtlı. Lütfen benzersiz bir fatura numarası giriniz.");
                }
            }
            else
            {
                number = await _unitOfWork.Invoices.GetNextInvoiceNumberAsync(dto.InvoiceType);
            }

            var invoice = new Invoice
            {
                InvoiceNumber = number,
                InvoiceType = dto.InvoiceType,
                CustomerId = dto.CustomerId,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Description = dto.Description,
                Scenario = string.IsNullOrWhiteSpace(dto.Scenario) ? "TICARI" : dto.Scenario,
                EFaturaUuid = Guid.NewGuid().ToString().ToUpper(),
                WithholdingRate = dto.WithholdingRate,
                Status = InvoiceStatus.Draft
            };

            foreach (var itemDto in dto.Items)
            {
                var discountAmount = itemDto.UnitPrice * itemDto.Quantity * (itemDto.DiscountRate / 100m);
                var vatAmount = (itemDto.UnitPrice * itemDto.Quantity - discountAmount) * (itemDto.VatRate / 100m);
                var lineTotal = itemDto.UnitPrice * itemDto.Quantity - discountAmount + vatAmount;

                invoice.Items.Add(new InvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitPrice = itemDto.UnitPrice,
                    VatRate = itemDto.VatRate,
                    DiscountRate = itemDto.DiscountRate,
                    DiscountAmount = discountAmount,
                    VatAmount = vatAmount,
                    LineTotal = lineTotal
                });

                invoice.SubTotal += itemDto.UnitPrice * itemDto.Quantity;
                invoice.DiscountTotal += discountAmount;
                invoice.VatTotal += vatAmount;
            }

            if (dto.WithholdingRate > 0)
            {
                invoice.WithholdingTotal = invoice.VatTotal * (dto.WithholdingRate / 10m);
            }

            invoice.GrandTotal = invoice.SubTotal - invoice.DiscountTotal + invoice.VatTotal - invoice.WithholdingTotal;

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            return _mapper.Map<InvoiceDto>(invoice);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> UpdateStatusAsync(int id, UpdateInvoiceStatusDto dto)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdWithItemsAsync(id);
        if (invoice == null) return false;

        if (invoice.Status == dto.Status) return true;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // State transitions
            if (dto.Status == InvoiceStatus.Approved && invoice.Status == InvoiceStatus.Draft)
            {
                await ApplyInventoryAndBalanceChanges(invoice, false);
            }
            else if (dto.Status == InvoiceStatus.Cancelled && invoice.Status == InvoiceStatus.Approved)
            {
                await ApplyInventoryAndBalanceChanges(invoice, true);
            }
            // other transitions like to Paid don't affect stock/balance directly in this simple model
            
            invoice.Status = dto.Status;
            _unitOfWork.Invoices.Update(invoice);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            return true;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private async Task ApplyInventoryAndBalanceChanges(Invoice invoice, bool isReversal)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(invoice.CustomerId);
        if (customer != null)
        {
            var amount = invoice.GrandTotal * (isReversal ? -1 : 1);
            if (invoice.InvoiceType == InvoiceType.Sales)
            {
                customer.Balance += amount;
            }
            else
            {
                customer.Balance -= amount; // for purchase, we owe them so balance decreases (or represents vendor balance)
            }
            _unitOfWork.Customers.Update(customer);
        }

        foreach (var item in invoice.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
            if (product != null)
            {
                var qty = item.Quantity * (isReversal ? -1 : 1);
                if (invoice.InvoiceType == InvoiceType.Sales)
                {
                    product.CurrentStock -= qty;
                }
                else
                {
                    product.CurrentStock += qty;
                }
                _unitOfWork.Products.Update(product);
            }
        }
    }

    public async Task<InvoiceDashboardDto> GetDashboardAsync()
    {
        var stats = await _unitOfWork.Invoices.GetDashboardStatsAsync();
        return new InvoiceDashboardDto(stats.TotalSalesCount, stats.TotalPurchaseCount, stats.TotalSalesAmount, stats.TotalPurchaseAmount, stats.DraftCount, stats.OverdueCount);
    }
}
