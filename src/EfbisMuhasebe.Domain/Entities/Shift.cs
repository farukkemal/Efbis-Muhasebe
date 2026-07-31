using System;
using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class Shift : BaseEntity
{
    public string ShiftCode { get; set; } = string.Empty;
    public int EmployeeId { get; set; }
    public DateTime ShiftDate { get; set; }
    public ShiftType ShiftType { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public TimeSpan? ActualStartTime { get; set; }
    public TimeSpan? ActualEndTime { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Planned;
    public decimal OvertimeHours { get; set; }
    public string? Notes { get; set; }
    
    public Employee? Employee { get; set; }
    
    public double PlannedHours => (EndTime - StartTime).TotalHours < 0 ? (EndTime - StartTime).TotalHours + 24 : (EndTime - StartTime).TotalHours;
    public double? ActualHours => ActualStartTime.HasValue && ActualEndTime.HasValue
        ? ((ActualEndTime.Value - ActualStartTime.Value).TotalHours < 0 ? (ActualEndTime.Value - ActualStartTime.Value).TotalHours + 24 : (ActualEndTime.Value - ActualStartTime.Value).TotalHours)
        : null;
}
