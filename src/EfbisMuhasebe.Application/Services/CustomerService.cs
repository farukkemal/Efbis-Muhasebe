using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CustomerService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResultDto<CustomerDto>> GetPagedAsync(CustomerFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Customers.GetPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.CustomerType,
            filter.Status,
            filter.BalanceStatus,
            filter.City,
            filter.SortBy,
            filter.Ascending);

        return new PagedResultDto<CustomerDto>
        {
            Items = _mapper.Map<IEnumerable<CustomerDto>>(items),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    public async Task<CustomerDashboardDto> GetDashboardStatsAsync()
    {
        var stats = await _unitOfWork.Customers.GetDashboardStatsAsync();
        return new CustomerDashboardDto
        {
            TotalCustomers = stats.TotalCustomers,
            CustomersOnly = stats.CustomersOnly,
            SuppliersOnly = stats.SuppliersOnly,
            BothCount = stats.BothCount,
            PassiveCount = stats.PassiveCount,
            TotalReceivables = stats.TotalReceivables,
            TotalPayables = stats.TotalPayables,
            NetBalance = stats.NetBalance
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        return customer is null ? null : _mapper.Map<CustomerDto>(customer);
    }

    public async Task<UpdateCustomerDto?> GetForEditAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        return customer is null ? null : _mapper.Map<UpdateCustomerDto>(customer);
    }

    public async Task<(bool Success, string Message, int? CustomerId)> CreateAsync(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            return (false, "Cari kodu zorunludur.", null);

        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, "Firma unvanı zorunludur.", null);

        var isCodeUnique = await _unitOfWork.Customers.IsCodeUniqueAsync(dto.CustomerCode);
        if (!isCodeUnique)
            return (false, "Bu cari kodu zaten kullanılmaktadır.", null);

        var customer = _mapper.Map<Customer>(dto);
        customer.CustomerCode = dto.CustomerCode.Trim().ToUpper();
        customer.Title = dto.Title.Trim();

        await _unitOfWork.Customers.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{customer.Title}' cari kartı oluşturuldu.", customer.Id);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(dto.Id);
        if (customer is null) return (false, "Cari hesap bulunamadı.");

        if (string.IsNullOrWhiteSpace(dto.CustomerCode))
            return (false, "Cari kodu zorunludur.");

        if (string.IsNullOrWhiteSpace(dto.Title))
            return (false, "Firma unvanı zorunludur.");

        var isCodeUnique = await _unitOfWork.Customers.IsCodeUniqueAsync(dto.CustomerCode, dto.Id);
        if (!isCodeUnique)
            return (false, "Bu cari kodu başka bir hesapta kullanılmaktadır.");

        _mapper.Map(dto, customer);
        customer.CustomerCode = dto.CustomerCode.Trim().ToUpper();
        customer.Title = dto.Title.Trim();
        customer.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{customer.Title}' cari kartı güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer is null) return (false, "Cari hesap bulunamadı.");

        // Bakiye sıfır değilse uyarı ver
        if (customer.Balance != 0)
            return (false, $"'{customer.Title}' hesabında {customer.Balance:N2} ₺ bakiye bulunmaktadır. Bakiyesi sıfırlanmamış cari hesap silinemez.");

        customer.IsDeleted = true;
        customer.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync();

        return (true, $"'{customer.Title}' cari hesabı silindi.");
    }

    public async Task<(bool Success, string Message)> ToggleStatusAsync(int id)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(id);
        if (customer is null) return (false, "Cari hesap bulunamadı.");

        customer.Status = customer.Status == CustomerStatus.Active ? CustomerStatus.Passive : CustomerStatus.Active;
        customer.UpdatedDate = DateTime.UtcNow;

        _unitOfWork.Customers.Update(customer);
        await _unitOfWork.SaveChangesAsync();

        var statusText = customer.Status == CustomerStatus.Active ? "aktifleştirildi" : "pasife alındı";
        return (true, $"'{customer.Title}' cari hesabı {statusText}.");
    }

    public async Task<(bool Success, string Message, int AffectedCount)> BulkUpdateStatusAsync(IEnumerable<int> ids, CustomerStatus status)
    {
        if (ids is null || !ids.Any()) return (false, "En az bir kayıt seçilmelidir.", 0);

        var count = await _unitOfWork.Customers.BulkUpdateStatusAsync(ids, status);
        var statusText = status == CustomerStatus.Active ? "aktifleştirildi" : "pasife alındı";
        return (true, $"{count} cari hesap {statusText}.", count);
    }
}
