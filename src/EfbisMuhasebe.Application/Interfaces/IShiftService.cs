using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;

namespace EfbisMuhasebe.Application.Interfaces;

public interface IShiftService
{
    Task<(IEnumerable<ShiftDto> Items, int TotalCount)> GetPagedAsync(ShiftFilterDto filter);
    Task<ShiftDto?> GetByIdAsync(int id);
    Task<ShiftDto> CreateAsync(CreateShiftDto dto);
    Task UpdateAsync(int id, UpdateShiftDto dto);
    Task DeleteAsync(int id);
    Task CheckInAsync(int id);
    Task CheckOutAsync(int id);
    Task MarkAbsentAsync(int id);
    Task<ShiftDashboardDto> GetDashboardAsync(DateTime? date = null);
    Task<int> GenerateWeeklyScheduleAsync(DateTime startDate);
}
