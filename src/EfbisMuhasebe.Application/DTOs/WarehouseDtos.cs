using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

public class WarehouseDto
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public WarehouseStatus Status { get; set; }
    public string StatusText => Status == WarehouseStatus.Active ? "Aktif" : "Pasif";
}

public class CreateWarehouseDto
{
    public string WarehouseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;
}

public class UpdateWarehouseDto
{
    public int Id { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public WarehouseStatus Status { get; set; } = WarehouseStatus.Active;
}

public class WarehouseFilterDto
{
    public string? SearchTerm { get; set; }
    public WarehouseStatus? Status { get; set; }
    private int _page = 1;
    public int Page { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageNumber { get => _page; set => _page = value > 0 ? value : 1; }
    public int PageSize { get; set; } = 15;
    public string? SortBy { get; set; }
    public bool Ascending { get; set; } = true;
}
