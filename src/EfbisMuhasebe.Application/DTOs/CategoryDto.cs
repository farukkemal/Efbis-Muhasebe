namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Kategori DTO — ürün sayısı ve üst kategori bilgileri dahil</summary>
public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
