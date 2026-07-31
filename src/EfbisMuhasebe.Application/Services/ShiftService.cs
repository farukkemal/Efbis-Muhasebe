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

public class ShiftService : IShiftService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ShiftService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<ShiftDto> Items, int TotalCount)> GetPagedAsync(ShiftFilterDto filter)
    {
        var (items, totalCount) = await _unitOfWork.Shifts.GetPagedAsync(
            filter.PageNumber,
            filter.PageSize,
            filter.SearchTerm,
            filter.ShiftType,
            filter.Status,
            filter.EmployeeId,
            filter.Department,
            filter.StartDate,
            filter.EndDate,
            filter.SortBy,
            filter.Ascending
        );

        return (_mapper.Map<IEnumerable<ShiftDto>>(items), totalCount);
    }

    public async Task<ShiftDto?> GetByIdAsync(int id)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<ShiftDto>(entity);
    }

    public async Task<ShiftDto> CreateAsync(CreateShiftDto dto)
    {
        if (await _unitOfWork.Shifts.ExistsAsync(dto.EmployeeId, dto.ShiftDate, dto.ShiftType))
        {
            throw new Exception("Bu personel için aynı tarih ve tipte bir vardiya zaten mevcut.");
        }

        var entity = _mapper.Map<Shift>(dto);

        if (string.IsNullOrEmpty(dto.StartTime) || string.IsNullOrEmpty(dto.EndTime))
        {
            var times = GetDefaultTimesForType(dto.ShiftType);
            entity.StartTime = times.Start;
            entity.EndTime = times.End;
        }

        entity.ShiftCode = GenerateShiftCode(entity.ShiftDate);
        entity.Status = ShiftStatus.Planned;

        await _unitOfWork.Shifts.AddAsync(entity);
        await _unitOfWork.SaveChangesAsync();

        return _mapper.Map<ShiftDto>(entity);
    }

    public async Task UpdateAsync(int id, UpdateShiftDto dto)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (entity == null) throw new Exception("Vardiya bulunamadı.");

        _mapper.Map(dto, entity);
        
        _unitOfWork.Shifts.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (entity == null) throw new Exception("Vardiya bulunamadı.");

        _unitOfWork.Shifts.Remove(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CheckInAsync(int id)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (entity == null) throw new Exception("Vardiya bulunamadı.");

        if (entity.Status != ShiftStatus.Planned)
            throw new Exception("Sadece planlanmış vardiyalar için giriş yapılabilir.");

        entity.ActualStartTime = DateTime.Now.TimeOfDay;
        entity.Status = ShiftStatus.Active;

        _unitOfWork.Shifts.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task CheckOutAsync(int id)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (entity == null) throw new Exception("Vardiya bulunamadı.");

        if (entity.Status != ShiftStatus.Active)
            throw new Exception("Sadece aktif vardiyalar için çıkış yapılabilir.");

        entity.ActualEndTime = DateTime.Now.TimeOfDay;
        entity.Status = ShiftStatus.Completed;

        if (entity.ActualHours.HasValue && entity.PlannedHours > 0)
        {
            entity.OvertimeHours = (decimal)Math.Max(0, entity.ActualHours.Value - entity.PlannedHours);
        }

        _unitOfWork.Shifts.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task MarkAbsentAsync(int id)
    {
        var entity = await _unitOfWork.Shifts.GetByIdAsync(id);
        if (entity == null) throw new Exception("Vardiya bulunamadı.");

        entity.Status = ShiftStatus.Absent;

        _unitOfWork.Shifts.Update(entity);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<ShiftDashboardDto> GetDashboardAsync(DateTime? date = null)
    {
        var stats = await _unitOfWork.Shifts.GetDashboardStatsAsync(date);
        
        return new ShiftDashboardDto
        {
            TodayShifts = stats.TodayShifts,
            ActiveNow = stats.ActiveNow,
            CompletedToday = stats.CompletedToday,
            AbsentToday = stats.AbsentToday,
            TotalOvertimeHours = stats.TotalOvertimeHours,
            PlannedThisWeek = stats.PlannedThisWeek
        };
    }

    public async Task<int> GenerateWeeklyScheduleAsync(DateTime startDate)
    {
        var monday = startDate.Date.AddDays(-(int)startDate.Date.DayOfWeek + (int)DayOfWeek.Monday);
        if (startDate.Date.DayOfWeek == DayOfWeek.Sunday) monday = monday.AddDays(-7);

        var employees = (await _unitOfWork.Employees.GetAllAsync())
            .Where(e => e.Status == EmployeeStatus.Active)
            .ToList();

        int createdCount = 0;
        int seq = 1;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            for (int dayOffset = 0; dayOffset < 7; dayOffset++)
            {
                var shiftDate = monday.AddDays(dayOffset);

                foreach (var emp in employees)
                {
                    ShiftType shiftType;
                    TimeSpan start, end;

                    switch (emp.Department)
                    {
                        case EmployeeDepartment.Warehouse:
                            shiftType = ShiftType.Morning;
                            start = new TimeSpan(8, 0, 0);
                            end = new TimeSpan(17, 0, 0);
                            break;
                        case EmployeeDepartment.Cashier:
                            if (dayOffset % 2 == 0)
                            {
                                shiftType = ShiftType.Morning;
                                start = new TimeSpan(9, 0, 0);
                                end = new TimeSpan(17, 0, 0);
                            }
                            else
                            {
                                shiftType = ShiftType.Afternoon;
                                start = new TimeSpan(13, 0, 0);
                                end = new TimeSpan(21, 0, 0);
                            }
                            break;
                        case EmployeeDepartment.Sales:
                            shiftType = ShiftType.FullDay;
                            start = new TimeSpan(10, 0, 0);
                            end = new TimeSpan(22, 0, 0);
                            break;
                        case EmployeeDepartment.CustomerService:
                            shiftType = ShiftType.Afternoon;
                            start = new TimeSpan(12, 0, 0);
                            end = new TimeSpan(21, 0, 0);
                            break;
                        default:
                            shiftType = ShiftType.Morning;
                            start = new TimeSpan(9, 0, 0);
                            end = new TimeSpan(18, 0, 0);
                            break;
                    }

                    if (await _unitOfWork.Shifts.ExistsAsync(emp.Id, shiftDate, shiftType))
                        continue;

                    var shift = new Shift
                    {
                        ShiftCode = $"VRD-{shiftDate:yyyy-MM-dd}-{seq:D3}",
                        EmployeeId = emp.Id,
                        ShiftDate = shiftDate,
                        ShiftType = shiftType,
                        StartTime = start,
                        EndTime = end,
                        Status = ShiftStatus.Planned
                    };

                    await _unitOfWork.Shifts.AddAsync(shift);
                    createdCount++;
                    seq++;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return createdCount;
    }

    private string GenerateShiftCode(DateTime date)
    {
        var seq = new Random().Next(1, 999).ToString("D3");
        return $"VRD-{date:yyyy-MM-dd}-{seq}";
    }

    private (TimeSpan Start, TimeSpan End) GetDefaultTimesForType(ShiftType type)
    {
        return type switch
        {
            ShiftType.Morning => (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0)),
            ShiftType.Afternoon => (new TimeSpan(13, 0, 0), new TimeSpan(21, 0, 0)),
            ShiftType.Evening => (new TimeSpan(17, 0, 0), new TimeSpan(1, 0, 0)),
            ShiftType.FullDay => (new TimeSpan(9, 0, 0), new TimeSpan(21, 0, 0)),
            ShiftType.HalfDay => (new TimeSpan(9, 0, 0), new TimeSpan(13, 0, 0)),
            _ => (new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0))
        };
    }
}
