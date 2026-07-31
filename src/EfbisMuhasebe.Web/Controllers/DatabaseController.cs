using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Infrastructure.Data;
using EfbisMuhasebe.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Web.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class DatabaseController : Controller
{
    private readonly AppDbContext _context;

    public DatabaseController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> GetServerStats()
    {
        try
        {
            var provider = _context.Database.ProviderName ?? "SqlServer";
            var canConnect = await _context.Database.CanConnectAsync();

            var userCount = await _context.Users.CountAsync();
            var productCount = await _context.Products.CountAsync();
            var categoryCount = await _context.Categories.CountAsync();
            var customerCount = await _context.Customers.CountAsync();
            var warehouseCount = await _context.Warehouses.CountAsync();
            var invoiceCount = await _context.Invoices.CountAsync();
            var invoiceItemCount = await _context.InvoiceItems.CountAsync();
            var stockTxCount = await _context.StockTransactions.CountAsync();
            var cashAccountCount = await _context.CashAccounts.CountAsync();
            var cashTxCount = await _context.CashTransactions.CountAsync();
            var incomeExpenseCount = await _context.IncomeExpenses.CountAsync();
            var employeeCount = await _context.Employees.CountAsync();
            var salaryPaymentCount = await _context.SalaryPayments.CountAsync();
            var shiftCount = await _context.Shifts.CountAsync();

            var totalRecords = userCount + productCount + categoryCount + customerCount + warehouseCount +
                               invoiceCount + invoiceItemCount + stockTxCount + cashAccountCount + cashTxCount +
                               incomeExpenseCount + employeeCount + salaryPaymentCount + shiftCount;

            return Json(new
            {
                success = true,
                data = new
                {
                    providerName = provider,
                    databaseName = "EfbisMuhasebeDb",
                    status = canConnect ? "Online 🟢" : "Offline 🔴",
                    tableCount = 14,
                    totalRecords = totalRecords,
                    connectionString = "Server=(localdb)\\mssqllocaldb; Database=EfbisMuhasebeDb; Trusted_Connection=True",
                    queryFilterPolicy = "Global Soft-Delete Filter Active (!IsDeleted)"
                }
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTableSummary()
    {
        try
        {
            var tables = new List<object>
            {
                new { name = "Users", displayName = "Kullanıcılar (Users)", recordCount = await _context.Users.CountAsync(), columnCount = 10, category = "Güvenlik & Yetki" },
                new { name = "Products", displayName = "Ürünler (Products)", recordCount = await _context.Products.CountAsync(), columnCount = 17, category = "Stok & Katalog" },
                new { name = "Categories", displayName = "Kategoriler (Categories)", recordCount = await _context.Categories.CountAsync(), columnCount = 7, category = "Stok & Katalog" },
                new { name = "Warehouses", displayName = "Depolar (Warehouses)", recordCount = await _context.Warehouses.CountAsync(), columnCount = 10, category = "Stok & Depo" },
                new { name = "StockTransactions", displayName = "Stok Hareketleri (StockTransactions)", recordCount = await _context.StockTransactions.CountAsync(), columnCount = 11, category = "Stok & Depo" },
                new { name = "Customers", displayName = "Müşteri & Tedarikçiler (Customers)", recordCount = await _context.Customers.CountAsync(), columnCount = 14, category = "Cari Hesablar" },
                new { name = "Invoices", displayName = "Faturalar (Invoices)", recordCount = await _context.Invoices.CountAsync(), columnCount = 11, category = "Satış & Fatura" },
                new { name = "InvoiceItems", displayName = "Fatura Kalemleri (InvoiceItems)", recordCount = await _context.InvoiceItems.CountAsync(), columnCount = 10, category = "Satış & Fatura" },
                new { name = "CashAccounts", displayName = "Kasa & Banka Hesapları (CashAccounts)", recordCount = await _context.CashAccounts.CountAsync(), columnCount = 10, category = "Finans & Kasa" },
                new { name = "CashTransactions", displayName = "Kasa Hareketleri (CashTransactions)", recordCount = await _context.CashTransactions.CountAsync(), columnCount = 9, category = "Finans & Kasa" },
                new { name = "IncomeExpenses", displayName = "Gelir & Gider Kayıtları (IncomeExpenses)", recordCount = await _context.IncomeExpenses.CountAsync(), columnCount = 8, category = "Finans & Kasa" },
                new { name = "Employees", displayName = "Personeller (Employees)", recordCount = await _context.Employees.CountAsync(), columnCount = 14, category = "İnsan Kaynakları" },
                new { name = "SalaryPayments", displayName = "Maaş Ödemeleri (SalaryPayments)", recordCount = await _context.SalaryPayments.CountAsync(), columnCount = 12, category = "İnsan Kaynakları" },
                new { name = "Shifts", displayName = "Vardiyalar (Shifts)", recordCount = await _context.Shifts.CountAsync(), columnCount = 13, category = "İnsan Kaynakları" }
            };

            return Json(new { success = true, data = tables });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTableData(string tableName, int page = 1, int pageSize = 15)
    {
        try
        {
            page = page > 0 ? page : 1;
            pageSize = pageSize > 0 ? pageSize : 15;

            object data = tableName switch
            {
                "Users" => await GetPagedData(_context.Users.Select(u => new { u.Id, u.Email, u.FullName, u.Title, Role = u.Role.ToString(), u.PhoneNumber, u.IsActive, u.LastLoginDate, u.CreatedDate }), page, pageSize),
                "Products" => await GetPagedData(_context.Products.Include(p => p.Category).Select(p => new { p.Id, p.ProductCode, p.ProductName, Category = p.Category != null ? p.Category.Name : null, ProductType = p.ProductType.ToString(), p.PurchasePrice, p.SalePrice, p.CurrentStock, p.MinimumStock, Status = p.Status.ToString() }), page, pageSize),
                "Categories" => await GetPagedData(_context.Categories.Include(c => c.Parent).Select(c => new { c.Id, c.Name, Parent = c.Parent != null ? c.Parent.Name : null, c.Description, ProductCount = c.Products.Count, c.CreatedDate }), page, pageSize),
                "Customers" => await GetPagedData(_context.Customers.Select(c => new { c.Id, c.CustomerCode, c.Title, Type = c.CustomerType.ToString(), c.Balance, c.Phone, c.Email, c.City, Status = c.Status.ToString() }), page, pageSize),
                "Warehouses" => await GetPagedData(_context.Warehouses.Select(w => new { w.Id, w.WarehouseCode, w.Name, w.City, w.Phone, w.IsDefault, Status = w.Status.ToString() }), page, pageSize),
                "Invoices" => await GetPagedData(_context.Invoices.Include(i => i.Customer).Select(i => new { i.Id, i.InvoiceNumber, Type = i.InvoiceType.ToString(), Customer = i.Customer != null ? i.Customer.Title : null, i.InvoiceDate, i.GrandTotal, Status = i.Status.ToString() }), page, pageSize),
                "CashAccounts" => await GetPagedData(_context.CashAccounts.Select(a => new { a.Id, a.AccountCode, a.AccountName, Type = a.AccountType.ToString(), a.BankName, a.Balance, a.Currency, Status = a.Status.ToString() }), page, pageSize),
                "IncomeExpenses" => await GetPagedData(_context.IncomeExpenses.Select(ie => new { ie.Id, ie.TransactionCode, Type = ie.Type.ToString(), ie.CategoryName, ie.Amount, ie.TransactionDate, ie.Description }), page, pageSize),
                "Employees" => await GetPagedData(_context.Employees.Select(e => new { e.Id, e.EmployeeCode, e.FirstName, e.LastName, Department = e.Department.ToString(), e.Title, e.Salary, Status = e.Status.ToString() }), page, pageSize),
                "SalaryPayments" => await GetPagedData(_context.SalaryPayments.Include(sp => sp.Employee).Select(sp => new { sp.Id, sp.PaymentCode, Employee = sp.Employee != null ? sp.Employee.FullName : null, sp.Year, sp.Month, sp.GrossSalary, sp.NetSalary, sp.TotalPayment, Status = sp.Status.ToString() }), page, pageSize),
                "Shifts" => await GetPagedData(_context.Shifts.Include(s => s.Employee).Select(s => new { s.Id, s.ShiftCode, Employee = s.Employee != null ? s.Employee.FullName : null, s.ShiftDate, Type = s.ShiftType.ToString(), PlannedTime = s.StartTime + " - " + s.EndTime, Status = s.Status.ToString() }), page, pageSize),
                _ => await GetPagedData(_context.StockTransactions.Include(st => st.Product).Select(st => new { st.Id, st.TransactionCode, Type = st.TransactionType.ToString(), Product = st.Product != null ? st.Product.ProductName : null, st.Quantity, st.UnitPrice, st.TotalAmount, st.TransactionDate }), page, pageSize)
            };

            return Json(new { success = true, data });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    private async Task<object> GetPagedData<T>(IQueryable<T> query, int page, int pageSize)
    {
        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new { items, totalCount, page, pageSize, totalPages = (int)Math.Ceiling((double)totalCount / pageSize) };
    }

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> RunSeed()
    {
        try
        {
            await DbSeeder.SeedAsync(_context);
            return Json(new { success = true, message = "Veritabanı seed işlemi başarıyla tamamlandı. Eksik veriler eklendi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}
