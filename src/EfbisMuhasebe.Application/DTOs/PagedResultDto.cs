namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Sayfalama sonuç wrapper'ı.
/// Tüm listeli veriler için kullanılabilir generic yapı.
/// </summary>
public class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
