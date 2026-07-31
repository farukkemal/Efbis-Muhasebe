using EfbisMuhasebe.Domain.Interfaces;
using EfbisMuhasebe.Infrastructure.Data;
using EfbisMuhasebe.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace EfbisMuhasebe.Infrastructure.UnitOfWork;

/// <summary>
/// Unit of Work implementasyonu.
/// Tüm repository'leri tek çatı altında yönetir ve transaction desteği sağlar.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProductRepository? _products;
    private ICategoryRepository? _categories;
    private ICustomerRepository? _customers;
    private IWarehouseRepository? _warehouses;
    private IStockTransactionRepository? _stockTransactions;
    private IInvoiceRepository? _invoices;
    private ICashAccountRepository? _cashAccounts;
    private IIncomeExpenseRepository? _incomeExpenses;
    private IEmployeeRepository? _employees;
    private ISalaryPaymentRepository? _salaryPayments;
    private IShiftRepository? _shifts;
    private IUserRepository? _users;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IProductRepository Products
        => _products ??= new ProductRepository(_context);

    public ICategoryRepository Categories
        => _categories ??= new CategoryRepository(_context);

    public ICustomerRepository Customers
        => _customers ??= new CustomerRepository(_context);

    public IWarehouseRepository Warehouses
        => _warehouses ??= new WarehouseRepository(_context);

    public IStockTransactionRepository StockTransactions
        => _stockTransactions ??= new StockTransactionRepository(_context);

    public IInvoiceRepository Invoices
        => _invoices ??= new InvoiceRepository(_context);

    public ICashAccountRepository CashAccounts
        => _cashAccounts ??= new CashAccountRepository(_context);

    public IIncomeExpenseRepository IncomeExpenses
        => _incomeExpenses ??= new IncomeExpenseRepository(_context);

    public IEmployeeRepository Employees
        => _employees ??= new EmployeeRepository(_context);

    public ISalaryPaymentRepository SalaryPayments
        => _salaryPayments ??= new SalaryPaymentRepository(_context);

    public IShiftRepository Shifts
        => _shifts ??= new ShiftRepository(_context);

    public IUserRepository Users
        => _users ??= new UserRepository(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync()
        => _transaction = await _context.Database.BeginTransactionAsync();

    public async Task CommitTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
