using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>
/// Cari Hesap DTO — Liste ve detay görünümü için
/// </summary>
public class CustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorizedPerson { get; set; }
    public CustomerType CustomerType { get; set; }
    public string CustomerTypeDisplay { get; set; } = string.Empty;

    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gsm { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }

    public decimal Balance { get; set; }
    public BalanceStatus BalanceStatus { get; set; }
    public string BalanceStatusDisplay { get; set; } = string.Empty;
    public decimal RiskLimit { get; set; }

    public CustomerStatus Status { get; set; }
    public string StatusDisplay { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
