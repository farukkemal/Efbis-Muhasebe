using System.Security.Claims;
using EfbisMuhasebe.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace EfbisMuhasebe.Infrastructure.Data;

/// <summary>
/// Ana EF Core DbContext.
/// Multi-Tenant (TenantId) ve Soft Delete (IsDeleted) için global query filter uygulanmıştır.
/// </summary>
public class AppDbContext : DbContext
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int CurrentTenantId
    {
        get
        {
            var user = _httpContextAccessor?.HttpContext?.User;
            var role = user?.FindFirstValue(ClaimTypes.Role);

            // 1. Normal kullanıcılar (SuperAdmin dışındakiler) için KESİNLİKLE kendi TenantId claim'i geçerlidir
            if (user != null && role != "SuperAdmin")
            {
                var tenantClaim = user.FindFirstValue("TenantId");
                if (int.TryParse(tenantClaim, out int tid) && tid > 0) return tid;
            }

            // 2. SuperAdmin için session'da seçilen aktif tenant varsa onu dön
            var session = _httpContextAccessor?.HttpContext?.Session;
            if (session != null)
            {
                var activeTenant = session.GetInt32("ActiveTenantId");
                if (activeTenant.HasValue) return activeTenant.Value;
            }

            // 3. SuperAdmin varsayılan olarak 0 (Ortak Görünüm)
            if (role == "SuperAdmin")
            {
                return 0;
            }

            return 0;
        }
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<CashAccount> CashAccounts => Set<CashAccount>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<IncomeExpense> IncomeExpenses => Set<IncomeExpense>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<SalaryPayment> SalaryPayments => Set<SalaryPayment>();
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Fluent API configuration dosyalarını otomatik uygula
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Global soft-delete + tenant isolation filter
        modelBuilder.Entity<Tenant>().HasQueryFilter(t => !t.IsDeleted);

        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted && (CurrentTenantId == 0 || p.TenantId == CurrentTenantId));
        modelBuilder.Entity<Category>().HasQueryFilter(c => !c.IsDeleted && (CurrentTenantId == 0 || c.TenantId == CurrentTenantId));
        modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted && (CurrentTenantId == 0 || c.TenantId == CurrentTenantId));
        modelBuilder.Entity<Warehouse>().HasQueryFilter(w => !w.IsDeleted && (CurrentTenantId == 0 || w.TenantId == CurrentTenantId));
        modelBuilder.Entity<StockTransaction>().HasQueryFilter(s => !s.IsDeleted && (CurrentTenantId == 0 || s.TenantId == CurrentTenantId));
        modelBuilder.Entity<Invoice>().HasQueryFilter(i => !i.IsDeleted && (CurrentTenantId == 0 || i.TenantId == CurrentTenantId));
        modelBuilder.Entity<InvoiceItem>().HasQueryFilter(ii => !ii.IsDeleted && (CurrentTenantId == 0 || ii.TenantId == CurrentTenantId));
        modelBuilder.Entity<CashAccount>().HasQueryFilter(ca => !ca.IsDeleted && (CurrentTenantId == 0 || ca.TenantId == CurrentTenantId));
        modelBuilder.Entity<CashTransaction>().HasQueryFilter(ct => !ct.IsDeleted && (CurrentTenantId == 0 || ct.TenantId == CurrentTenantId));
        modelBuilder.Entity<IncomeExpense>().HasQueryFilter(ie => !ie.IsDeleted && (CurrentTenantId == 0 || ie.TenantId == CurrentTenantId));
        modelBuilder.Entity<Employee>().HasQueryFilter(e => !e.IsDeleted && (CurrentTenantId == 0 || e.TenantId == CurrentTenantId));
        modelBuilder.Entity<SalaryPayment>().HasQueryFilter(sp => !sp.IsDeleted && (CurrentTenantId == 0 || sp.TenantId == CurrentTenantId));
        modelBuilder.Entity<Shift>().HasQueryFilter(s => !s.IsDeleted && (CurrentTenantId == 0 || s.TenantId == CurrentTenantId));
        modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
    }

    /// <summary>
    /// SaveChanges öncesinde TenantId ve audit alanlarını otomatik günceller.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries();
        int activeTenantId = CurrentTenantId;

        foreach (var entry in entries)
        {
            if (entry.Entity is Domain.Common.BaseEntity baseEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (baseEntity.TenantId == 0 && activeTenantId > 0 && !(baseEntity is Tenant) && !(baseEntity is User u && u.Role == Domain.Enums.UserRole.SuperAdmin))
                    {
                        baseEntity.TenantId = activeTenantId;
                    }
                    baseEntity.CreatedDate = DateTime.UtcNow;
                }
                else if (entry.State == EntityState.Modified)
                {
                    baseEntity.UpdatedDate = DateTime.UtcNow;
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
