using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class Warehouse : BaseEntity
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; } = false;
    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;
}
