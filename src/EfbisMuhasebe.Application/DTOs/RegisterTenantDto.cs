namespace EfbisMuhasebe.Application.DTOs;

public class RegisterTenantDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string AdminFullName { get; set; } = string.Empty;
    public string AdminEmail { get; set; } = string.Empty;
    public string AdminPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? Sector { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
}
