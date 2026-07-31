using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public EmployeeService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetPagedAsync(EmployeeFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Employees.GetPagedAsync(
            filter.Page,
            filter.PageSize,
            filter.SearchTerm,
            filter.Department,
            filter.Status,
            filter.WarehouseId,
            filter.SortBy,
            filter.Ascending
        );

        return (_mapper.Map<IEnumerable<EmployeeDto>>(items), totalCount);
    }

    public async Task<EmployeeDto?> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.Employees.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<EmployeeDto>(entity);
    }

    public async Task<EmployeeDto> CreateAsync(CreateEmployeeDto dto)
    {
        var isUnique = await _unitOfWork.Employees.IsCodeUniqueAsync(dto.EmployeeCode);
        if (!isUnique)
            throw new Exception("Personel kodu zaten kullanımda.");

        var entity = _mapper.Map<Employee>(dto);
        await _unitOfWork.Employees.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<EmployeeDto>(entity);
    }

    public async Task UpdateAsync(int id, UpdateEmployeeDto dto)
    {
        var entity = await _unitOfWork.Employees.GetByIdAsync(id);
        if (entity == null)
            throw new Exception("Personel bulunamadı.");

        var isUnique = await _unitOfWork.Employees.IsCodeUniqueAsync(dto.EmployeeCode, id);
        if (!isUnique)
            throw new Exception("Personel kodu zaten kullanımda.");

        _mapper.Map(dto, entity);
        _unitOfWork.Employees.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _unitOfWork.Employees.GetByIdAsync(id);
        if (entity == null)
            throw new Exception("Personel bulunamadı.");

        _unitOfWork.Employees.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<EmployeeDashboardDto> GetDashboardAsync()
    {
        var stats = await _unitOfWork.Employees.GetDashboardStatsAsync();
        return new EmployeeDashboardDto
        {
            TotalEmployees = stats.TotalEmployees,
            ActiveEmployees = stats.ActiveEmployees,
            OnLeaveEmployees = stats.OnLeaveEmployees,
            TotalMonthlySalary = stats.TotalMonthlySalary,
            WarehouseStaffCount = stats.WarehouseStaffCount,
            CashierStaffCount = stats.CashierStaffCount,
            SalesStaffCount = stats.SalesStaffCount,
            ConsultantStaffCount = stats.ConsultantStaffCount
        };
    }
}
