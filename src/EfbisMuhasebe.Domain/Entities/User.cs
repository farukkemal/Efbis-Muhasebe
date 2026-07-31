using EfbisMuhasebe.Domain.Common;
using EfbisMuhasebe.Domain.Enums;

namespace EfbisMuhasebe.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Staff;
    public string? Title { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginDate { get; set; }
    public string AllowedModules { get; set; } = "Products,SalesProducts,Pos,Categories,StockTransactions,Warehouses,Invoices,CashAccounts,IncomeExpenses,Employees,SalaryPayments,Shifts,Reports,Customers,Users";
}
