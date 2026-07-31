using EfbisMuhasebe.Domain.Common;

namespace EfbisMuhasebe.Domain.Entities;

/// <summary>
/// Ürün kategorisi entity'si.
/// İleride alt kategoriler eklenebilmesi için ParentId alanı hazır bırakılmıştır.
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentId { get; set; }

    // Navigation properties
    public Category? Parent { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
