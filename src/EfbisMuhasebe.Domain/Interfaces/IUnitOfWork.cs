namespace EfbisMuhasebe.Domain.Interfaces;

/// <summary>
/// Unit of Work arayüzü.
/// Tek transaction içinde birden fazla repository işlemini koordine eder.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IProductRepository Products { get; }
    ICategoryRepository Categories { get; }
    ICustomerRepository Customers { get; }
    IWarehouseRepository Warehouses { get; }
    IStockTransactionRepository StockTransactions { get; }
    IInvoiceRepository Invoices { get; }
    ICashAccountRepository CashAccounts { get; }
    IIncomeExpenseRepository IncomeExpenses { get; }
    IEmployeeRepository Employees { get; }
    ISalaryPaymentRepository SalaryPayments { get; }
    IShiftRepository Shifts { get; }
    IUserRepository Users { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
