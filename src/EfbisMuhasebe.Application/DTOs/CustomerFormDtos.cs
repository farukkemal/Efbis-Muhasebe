using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Application.DTOs;

/// <summary>Yeni cari kart oluşturma DTO</summary>
public class CreateCustomerDto
{
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorizedPerson { get; set; }
    public CustomerType CustomerType { get; set; } = CustomerType.Customer;

    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gsm { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }

    public decimal InitialBalance { get; set; } = 0; // İlk açılış bakiyesi
    public decimal RiskLimit { get; set; } = 0;
    public string? Notes { get; set; }
}

/// <summary>Cari kart güncelleme DTO</summary>
public class UpdateCustomerDto
{
    public int Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? AuthorizedPerson { get; set; }
    public CustomerType CustomerType { get; set; }

    public string? TaxOffice { get; set; }
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Gsm { get; set; }

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }

    public decimal RiskLimit { get; set; }
    public string? Notes { get; set; }
}
