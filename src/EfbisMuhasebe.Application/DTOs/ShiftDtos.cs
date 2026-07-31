using System;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class ShiftDto
{
    public int Id { get; set; }
    public string ShiftCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentText { get; set; } = string.Empty;
    public DateTime ShiftDate { get; set; }
    public string FormattedDate => ShiftDate.ToString("dd.MM.yyyy");
    public ShiftType ShiftType { get; set; }
    public string ShiftTypeText { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string FormattedStartTime => StartTime.ToString(@"hh\:mm");
    public string FormattedEndTime => EndTime.ToString(@"hh\:mm");
    public TimeSpan? ActualStartTime { get; set; }
    public TimeSpan? ActualEndTime { get; set; }
    public string? FormattedActualStart => ActualStartTime?.ToString(@"hh\:mm");
    public string? FormattedActualEnd => ActualEndTime?.ToString(@"hh\:mm");
    public ShiftStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public decimal OvertimeHours { get; set; }
    public string? Notes { get; set; }
    public double PlannedHours { get; set; }
    public double? ActualHours { get; set; }
}

public class CreateShiftDto
{
    public int EmployeeId { get; set; }
    public DateTime ShiftDate { get; set; }
    public ShiftType ShiftType { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class UpdateShiftDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime ShiftDate { get; set; }
    public ShiftType ShiftType { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? ActualStartTime { get; set; }
    public string? ActualEndTime { get; set; }
    public ShiftStatus Status { get; set; }
    public decimal OvertimeHours { get; set; }
    public string? Notes { get; set; }
}

public class ShiftFilterDto
{
    public string? SearchTerm { get; set; }
    public ShiftType? ShiftType { get; set; }
    public ShiftStatus? Status { get; set; }
    public int? EmployeeId { get; set; }
    public EmployeeDepartment? Department { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    
    private int _page = 1;
    public int Page 
    { 
        get => _page; 
        set => _page = value; 
    }
    public int PageNumber 
    { 
        get => _page; 
        set => _page = value; 
    }
    
    public int PageSize { get; set; } = 10;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}

public class ShiftDashboardDto
{
    public int TodayShifts { get; set; }
    public int ActiveNow { get; set; }
    public int CompletedToday { get; set; }
    public int AbsentToday { get; set; }
    public decimal TotalOvertimeHours { get; set; }
    public int PlannedThisWeek { get; set; }
}
