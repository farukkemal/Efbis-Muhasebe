using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EfbisMuhasebe.Infrastructure.Data.Seed;

/// <summary>
/// Veritabanı ilk kurulum ve multi-tenant test verileri seeder sınıfı.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // 1. Somee.com üzerindeki önceden oluşturulmuş veritabanına erişim kontrolü
        if (!await context.Database.CanConnectAsync())
        {
            throw new InvalidOperationException(
                "[EFBIS ERROR] Veritabanına bağlanılamadı! Lütfen Somee.com panelinde veritabanının oluşturulduğundan " +
                "ve Render.com üzerindeki 'ConnectionStrings__DefaultConnection' çevre değişkeninin (Server, Database, User Id, Password) " +
                "Somee.com veritabanı bilgileriyle BİREBİR EŞLEŞTİĞİNDEN emin olun.");
        }

        // 2. Pre-existing veritabanı üzerine EF Core Migration'larını uygula (master/CREATE DATABASE izni gerekmez)
        await context.Database.MigrateAsync();

        // 3. Veritabanında zaten veri varsa seed işlemini atla (mevcut verileri koru)
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // 1. SADECE SUPER ADMIN HESABI
        var superAdmin = new User
        {
            TenantId = 0,
            Email = "superadmin@efbismuhasebe.com",
            PasswordHash = Application.Services.AuthService.HashPassword("SuperAdmin123!"),
            FullName = "Platform Super Admin",
            Role = UserRole.SuperAdmin,
            Title = "Sistem Sahibi & Genel Platform Yöneticisi",
            PhoneNumber = "0500 999 0000",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await context.Users.AddAsync(superAdmin);
        await context.SaveChangesAsync();

        // 2. BELTUR Florya Sosyal Tesisleri & Plaj (Turizm & Gastronomi)
        await SeedBelturFloryaDataAsync(context);

        // 3. Mard Inn Otelcilik (Otel & Konaklama) - mertmardin@mardin.com
        await SeedMardInnHotelDataAsync(context);

        // 4. AK Polimer Sanayi (Kimya & Plastik Sanayi) - aycapolimer@polimer.com
        await SeedAkPolimerDataAsync(context);
    }

    #region 1. BELTUR FLORYA DATA SEEDER
    public static async Task SeedBelturFloryaDataAsync(AppDbContext context)
    {
        try
        {
            var existingUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "farukbeltur@beltur.com");
            Tenant tenant;
            User user;

            if (existingUser != null)
            {
                user = existingUser;
                tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == user.TenantId);
            }
            else
            {
                var maxId = await context.Tenants.IgnoreQueryFilters().AnyAsync() 
                    ? await context.Tenants.IgnoreQueryFilters().MaxAsync(t => t.Id) 
                    : 0;
                int newTenantId = maxId + 1;

                tenant = new Tenant
                {
                    TenantCode = $"TEN-{newTenantId:D3}",
                    CompanyName = "BELTUR Florya Sosyal Tesisleri & Plaj İşletmeciliği",
                    TradeTitle = "BELTUR Florya Sosyal Tesisleri ve Plaj İşletmeleri A.Ş.",
                    TaxNumber = "1472583690",
                    TaxOffice = "Bakırköy Vergi Dairesi",
                    Sector = "Turizm, Plaj & Gastronomi",
                    Phone = "0212 555 3400",
                    Email = "farukbeltur@beltur.com",
                    City = "İstanbul",
                    Address = "Florya Sahil Yolu No:45 Bakırköy / İstanbul",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Tenants.AddAsync(tenant);
                await context.SaveChangesAsync();

                user = new User
                {
                    TenantId = tenant.Id,
                    Email = "farukbeltur@beltur.com",
                    PasswordHash = Application.Services.AuthService.HashPassword("Beltur123!"),
                    FullName = "Faruk Yılmaz",
                    Role = UserRole.Admin,
                    Title = "BELTUR Florya Tesis Müdürü (Tenant Admin)",
                    PhoneNumber = "0532 555 3400",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }

            int tId = tenant.Id;

            // Warehouses
            var existingWhs = await context.Warehouses.IgnoreQueryFilters().Where(w => w.TenantId == tId).ToListAsync();
            if (!existingWhs.Any())
            {
                var warehouses = new List<Warehouse>
                {
                    new Warehouse { TenantId = tId, WarehouseCode = "BLT-DEP-001", Name = "Florya Plaj Deposu", Address = "Plaj Bölgesi Kabinler Arkası", City = "İstanbul", Phone = "0212 555 3401", IsDefault = true, Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "BLT-DEP-002", Name = "Florya Restoran & Kafe Deposu", Address = "Ana Bina Mutfak Yanı", City = "İstanbul", Phone = "0212 555 3402", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "BLT-DEP-003", Name = "Florya Giyim & Hediyelik Deposu", Address = "Mağaza İdari Katı", City = "İstanbul", Phone = "0212 555 3403", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "BLT-DEP-004", Name = "Florya Mutfak & Ham Madde Deposu", Address = "Soğuk Hava Depo Alanı", City = "İstanbul", Phone = "0212 555 3404", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "BLT-DEP-005", Name = "Florya Merkez Genel Depo", Address = "Tesis Arka Saha Deposu", City = "İstanbul", Phone = "0212 555 3405", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Warehouses.AddRangeAsync(warehouses);
                await context.SaveChangesAsync();
            }

            // Categories
            var existingCats = await context.Categories.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            Category catPlaj, catGiyim, catRestoran, catKafe;
            if (!existingCats.Any())
            {
                catPlaj = new Category { TenantId = tId, Name = "Plaj & Hizmetler", Description = "Plaj alanı şezlong ve kabin hizmetleri", CreatedDate = DateTime.UtcNow };
                catGiyim = new Category { TenantId = tId, Name = "Plaj Giyim & Aksesuar", Description = "Terlik, havlu ve plaj ekipmanları", CreatedDate = DateTime.UtcNow };
                catRestoran = new Category { TenantId = tId, Name = "Restoran & Izgara Menüler", Description = "Ana restoran ve sıcak yemekler", CreatedDate = DateTime.UtcNow };
                catKafe = new Category { TenantId = tId, Name = "Kafe & İçecekler", Description = "Soğuk kahve, tost ve meşrubatlar", CreatedDate = DateTime.UtcNow };

                await context.Categories.AddRangeAsync(new[] { catPlaj, catGiyim, catRestoran, catKafe });
                await context.SaveChangesAsync();
            }
            else
            {
                catPlaj = existingCats.FirstOrDefault(c => c.Name == "Plaj & Hizmetler") ?? existingCats.First();
                catGiyim = existingCats.FirstOrDefault(c => c.Name == "Plaj Giyim & Aksesuar") ?? existingCats.First();
                catRestoran = existingCats.FirstOrDefault(c => c.Name == "Restoran & Izgara Menüler") ?? existingCats.First();
                catKafe = existingCats.FirstOrDefault(c => c.Name == "Kafe & İçecekler") ?? existingCats.First();
            }

            // Products
            var existingProducts = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();
            if (!existingProducts.Any())
            {
                var products = new List<Product>
                {
                    new Product { TenantId = tId, CategoryId = catPlaj.Id, ProductCode = "PRD-BLT-001", ProductName = "Şezlong Kiralama Günlük (Plaj Hizmeti)", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 500, PurchasePrice = 50, SalePrice = 250, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catGiyim.Id, ProductCode = "PRD-BLT-002", ProductName = "Parmak Arası Plaj Terliği (BELTUR)", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 200, PurchasePrice = 60, SalePrice = 180, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catGiyim.Id, ProductCode = "PRD-BLT-003", ProductName = "BELTUR Baskılı Plaj Havlusu", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 150, PurchasePrice = 120, SalePrice = 350, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catRestoran.Id, ProductCode = "PRD-BLT-004", ProductName = "Kasap Köfte & Ayran Menü", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 300, PurchasePrice = 90, SalePrice = 280, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catKafe.Id, ProductCode = "PRD-BLT-005", ProductName = "Soğuk Latte & Kaşarlı Tost Menü", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 400, PurchasePrice = 45, SalePrice = 160, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // Customers & Suppliers
            var existingCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            if (!existingCusts.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer { TenantId = tId, CustomerCode = "CARI-BLT-001", Title = "Florya Gıda & Et Tedarik Ltd. Şti.", CustomerType = CustomerType.Supplier, TaxOffice = "Bakırköy V.D.", TaxNumber = "1112223334", Phone = "0212 444 1001", City = "İstanbul", Balance = -45000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CARI-BLT-002", Title = "İstanbul Tekstil & Plaj Giyim A.Ş.", CustomerType = CustomerType.Supplier, TaxOffice = "Zeytinburnu V.D.", TaxNumber = "2223334445", Phone = "0212 444 1002", City = "İstanbul", Balance = -18000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CARI-BLT-003", Title = "Marmara Meşrubat & Kahve San. A.Ş.", CustomerType = CustomerType.Supplier, TaxOffice = "Kadıköy V.D.", TaxNumber = "3334445556", Phone = "0216 444 1003", City = "İstanbul", Balance = -12500, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CARI-BLT-004", Title = "BELTUR Kurumsal Etkinlik & İBBA", CustomerType = CustomerType.Customer, TaxOffice = "Beşiktaş V.D.", TaxNumber = "4445556667", Phone = "0212 444 1004", City = "İstanbul", Balance = 75000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CARI-BLT-005", Title = "Florya Plaj Voleybol Turnuva Org.", CustomerType = CustomerType.Customer, TaxOffice = "Bakırköy V.D.", TaxNumber = "5556667778", Phone = "0212 444 1005", City = "İstanbul", Balance = 32000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // Cash Accounts
            var existingCash = await context.CashAccounts.IgnoreQueryFilters().Where(ca => ca.TenantId == tId).ToListAsync();
            if (!existingCash.Any())
            {
                var cashAccounts = new List<CashAccount>
                {
                    new CashAccount { TenantId = tId, AccountCode = "KSA-BLT-001", AccountName = "Florya Plaj Gişe Ana Kasa", AccountType = CashAccountType.Kasa, Balance = 38500, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Nakit Plaj Giriş Kasa", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "KSA-BLT-002", AccountName = "Restoran & Kafe POS Kasa", AccountType = CashAccountType.Kasa, Balance = 24800, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Günlük Yemek POS Kasa", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-BLT-001", AccountName = "Vakıfbank BELTUR Florya Ticari", AccountType = CashAccountType.Banka, BankName = "Vakıfbank", Iban = "TR12 0001 5001 2345 6789 0001 01", Balance = 485000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Ana Kurumsal Banka Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-BLT-002", AccountName = "Ziraat Bankası Etkinlik & Tahsilat", AccountType = CashAccountType.Banka, BankName = "Ziraat Bankası", Iban = "TR98 0001 0002 3456 7890 0002 02", Balance = 195000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Organizasyon Tahsilat Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-BLT-003", AccountName = "Halkbank Tedarikçi Ödeme Hesabı", AccountType = CashAccountType.Banka, BankName = "Halkbank", Iban = "TR55 0001 2003 4567 8901 0003 03", Balance = 112000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Fatura ve Tediye Havale Hesabı", CreatedDate = DateTime.UtcNow }
                };
                await context.CashAccounts.AddRangeAsync(cashAccounts);
                await context.SaveChangesAsync();
            }

            // Employees
            var existingEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
            if (!existingEmp.Any())
            {
                var employees = new List<Employee>
                {
                    new Employee { TenantId = tId, EmployeeCode = "PRS-BLT-001", FirstName = "Ahmet", LastName = "Kaya", TCKN = "11223344556", Department = EmployeeDepartment.Sales, Title = "Plaj & Tesis Sorumlusu", Salary = 42000, HireDate = DateTime.UtcNow.AddYears(-2), Phone = "0532 111 0101", Email = "ahmet.kaya@beltur.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-BLT-002", FirstName = "Zeynep", LastName = "Demir", TCKN = "22334455667", Department = EmployeeDepartment.Management, Title = "Kıdemli Tesis Muhasebecisi", Salary = 45000, HireDate = DateTime.UtcNow.AddYears(-3), Phone = "0532 111 0202", Email = "zeynep.demir@beltur.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-BLT-003", FirstName = "Mehmet", LastName = "Şahin", TCKN = "33445566778", Department = EmployeeDepartment.Warehouse, Title = "Head Chef / Mutfak Şefi", Salary = 50000, HireDate = DateTime.UtcNow.AddYears(-1), Phone = "0532 111 0303", Email = "mehmet.sahin@beltur.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-BLT-004", FirstName = "Elif", LastName = "Çelik", TCKN = "44556677889", Department = EmployeeDepartment.Sales, Title = "Mağaza & Hediyelik Satış Personeli", Salary = 32000, HireDate = DateTime.UtcNow.AddMonths(-8), Phone = "0532 111 0404", Email = "elif.celik@beltur.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-BLT-005", FirstName = "Caner", LastName = "Yıldız", TCKN = "55667788990", Department = EmployeeDepartment.Warehouse, Title = "Ana Depo ve Lojistik Görevlisi", Salary = 34000, HireDate = DateTime.UtcNow.AddMonths(-5), Phone = "0532 111 0505", Email = "caner.yildiz@beltur.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Employees.AddRangeAsync(employees);
                await context.SaveChangesAsync();
            }

            // Shifts
            var existingShifts = await context.Shifts.IgnoreQueryFilters().Where(s => s.TenantId == tId).ToListAsync();
            if (!existingShifts.Any())
            {
                var dbEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
                if (dbEmp.Count >= 5)
                {
                    var shifts = new List<Shift>
                    {
                        new Shift { TenantId = tId, ShiftCode = "VRD-BLT-001", EmployeeId = dbEmp[0].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 30, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Completed, Notes = "Gündüz Plaj ve Şezlong Düzenleme Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-BLT-002", EmployeeId = dbEmp[1].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(18, 0, 0), ShiftType = ShiftType.FullDay, Status = ShiftStatus.Active, Notes = "Ofis ve Gün Sonu Kasa Kapanış Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-BLT-003", EmployeeId = dbEmp[2].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(12, 0, 0), EndTime = new TimeSpan(20, 30, 0), ShiftType = ShiftType.Evening, Status = ShiftStatus.Active, Notes = "Akşam Restoran ve Yemek Servisi Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-BLT-004", EmployeeId = dbEmp[3].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(19, 0, 0), ShiftType = ShiftType.Afternoon, Status = ShiftStatus.Planned, Notes = "Yarın Mağaza Satış Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-BLT-005", EmployeeId = dbEmp[4].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(16, 0, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Planned, Notes = "Erken Depo Sevkiyat Kabul Vardiyası", CreatedDate = DateTime.UtcNow }
                    };
                    await context.Shifts.AddRangeAsync(shifts);
                    await context.SaveChangesAsync();
                }
            }

            // Income & Expenses
            var existingIE = await context.IncomeExpenses.IgnoreQueryFilters().Where(ie => ie.TenantId == tId).ToListAsync();
            if (!existingIE.Any())
            {
                var incomeExpenses = new List<IncomeExpense>
                {
                    new IncomeExpense { TenantId = tId, TransactionCode = "GLR-2026-001", Type = IncomeExpenseType.Income, CategoryName = "Plaj & Şezlong Kiralama Geliri", Amount = 125000, Description = "Hafta Sonu Toplu Plaj Giriş Gelirleri", TransactionDate = DateTime.UtcNow.AddDays(-2), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-2026-001", Type = IncomeExpenseType.Expense, CategoryName = "Kira Giderleri", Amount = 85000, Description = "İBB Tesis Kullanım Aylık Kira Bedeli", TransactionDate = DateTime.UtcNow.AddDays(-5), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-2026-002", Type = IncomeExpenseType.Expense, CategoryName = "Elektrik & Su Faturası", Amount = 34500, Description = "Tesis ve Plaj Aydınlatma / Duş Kullanım Bedeli", TransactionDate = DateTime.UtcNow.AddDays(-4), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GLR-2026-002", Type = IncomeExpenseType.Income, CategoryName = "Restoran & Kafe Satış Geliri", Amount = 68400, Description = "Haftalık Kafe ve Tost / Kahve Menü Satışları", TransactionDate = DateTime.UtcNow.AddDays(-1), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-2026-003", Type = IncomeExpenseType.Expense, CategoryName = "Temizlik & İlaçlama Gideri", Amount = 12000, Description = "Plaj Kumu Eleme ve Hijyen İlaçlama Hizmeti", TransactionDate = DateTime.UtcNow.AddDays(-3), CreatedDate = DateTime.UtcNow }
                };
                await context.IncomeExpenses.AddRangeAsync(incomeExpenses);
                await context.SaveChangesAsync();
            }

            // Invoices
            var existingInv = await context.Invoices.IgnoreQueryFilters().Where(i => i.TenantId == tId).ToListAsync();
            if (!existingInv.Any())
            {
                var dbCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
                var dbProds = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();

                if (dbCusts.Count >= 5 && dbProds.Count >= 5)
                {
                    var inv1 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "AFT-2026-101",
                        InvoiceType = InvoiceType.Purchase,
                        CustomerId = dbCusts[0].Id,
                        InvoiceDate = DateTime.UtcNow.AddDays(-10),
                        SubTotal = 40909.09m,
                        VatTotal = 4090.91m,
                        DiscountTotal = 0,
                        GrandTotal = 45000,
                        Status = InvoiceStatus.Approved,
                        Description = "Toplu et, köfte ve ayran menü alım faturası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[3].Id, Quantity = 300, UnitPrice = 90, VatRate = 10, VatAmount = 2700, LineTotal = 29700, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    var inv2 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "SFT-2026-201",
                        InvoiceType = InvoiceType.Sales,
                        CustomerId = dbCusts[3].Id,
                        InvoiceDate = DateTime.UtcNow.AddDays(-3),
                        SubTotal = 62500,
                        VatTotal = 12500,
                        DiscountTotal = 0,
                        GrandTotal = 75000,
                        Status = InvoiceStatus.Approved,
                        Description = "Kurumsal plaj kullanımı ve toplu şezlong kirası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[0].Id, Quantity = 250, UnitPrice = 250, VatRate = 20, VatAmount = 12500, LineTotal = 75000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    await context.Invoices.AddRangeAsync(inv1, inv2);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("BELTUR SEED ERROR: " + ex.Message);
        }
    }
    #endregion

    #region 2. MARD INN OTELCİLİK DATA SEEDER (mertmardin@mardin.com)
    public static async Task SeedMardInnHotelDataAsync(AppDbContext context)
    {
        try
        {
            var existingUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "mertmardin@mardin.com");
            Tenant tenant;
            User user;

            if (existingUser != null)
            {
                user = existingUser;
                tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == user.TenantId);
            }
            else
            {
                var maxId = await context.Tenants.IgnoreQueryFilters().AnyAsync() 
                    ? await context.Tenants.IgnoreQueryFilters().MaxAsync(t => t.Id) 
                    : 0;
                int newTenantId = maxId + 1;

                tenant = new Tenant
                {
                    TenantCode = "TEN-002",
                    CompanyName = "Mard Inn Otelcilik ve Turizm A.Ş.",
                    TradeTitle = "Mard Inn Deluxe Hotel & Convention Center A.Ş.",
                    TaxNumber = "6120489201",
                    TaxOffice = "Mardin Vergi Dairesi",
                    Sector = "Otelcilik, Konaklama & Turizm",
                    Phone = "0482 212 5000",
                    Email = "mertmardin@mardin.com",
                    City = "Mardin",
                    Address = "Artuklu Mah. Vali Ozan Cad. No:88 Artuklu / Mardin",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Tenants.AddAsync(tenant);
                await context.SaveChangesAsync();

                user = new User
                {
                    TenantId = tenant.Id,
                    Email = "mertmardin@mardin.com",
                    PasswordHash = Application.Services.AuthService.HashPassword("Musteri123!"),
                    FullName = "Mert Mardinli",
                    Role = UserRole.Admin,
                    Title = "Mard Inn Otel Genel Müdürü (Tenant Admin)",
                    PhoneNumber = "0532 987 6543",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }

            int tId = tenant.Id;

            // Warehouses
            var existingWhs = await context.Warehouses.IgnoreQueryFilters().Where(w => w.TenantId == tId).ToListAsync();
            if (!existingWhs.Any())
            {
                var warehouses = new List<Warehouse>
                {
                    new Warehouse { TenantId = tId, WarehouseCode = "HOT-DEP-01", Name = "Otel Ana Depo", Address = "Otel B1 Katı İdari Depo", City = "Mardin", Phone = "0482 212 5001", IsDefault = true, Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "HOT-DEP-02", Name = "Mutfak & Restoran Soğuk Depo", Address = "Mutfak Katı Soğuk Odalar", City = "Mardin", Phone = "0482 212 5002", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "HOT-DEP-03", Name = "Kat Hizmetleri & Çamaşırhane Depo", Address = "Zemin Kat Çamaşırhane", City = "Mardin", Phone = "0482 212 5003", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "HOT-DEP-04", Name = "SPA & Hamam Sarf Malzeme Depo", Address = "SPA Katı Depo", City = "Mardin", Phone = "0482 212 5004", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Warehouses.AddRangeAsync(warehouses);
                await context.SaveChangesAsync();
            }

            // Categories
            var existingCats = await context.Categories.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            Category catOda, catGurme, catSpa, catMinibar, catHijyen;
            if (!existingCats.Any())
            {
                catOda = new Category { TenantId = tId, Name = "Oda Konaklama Hizmetleri", Description = "Deluxe, King ve Suit oda konaklamaları", CreatedDate = DateTime.UtcNow };
                catGurme = new Category { TenantId = tId, Name = "Restoran & Gourmet Menü", Description = "Geleneksel Mardin mutfağı ve açık büfe", CreatedDate = DateTime.UtcNow };
                catSpa = new Category { TenantId = tId, Name = "SPA & Wellness Masaj Paketleri", Description = "Türk hamamı, sauna ve cilt bakımı", CreatedDate = DateTime.UtcNow };
                catMinibar = new Category { TenantId = tId, Name = "Mini Bar & İçecek Grubu", Description = "Oda içi ikram ve meşrubatlar", CreatedDate = DateTime.UtcNow };
                catHijyen = new Category { TenantId = tId, Name = "Kat Hizmetleri & Buklet Seti", Description = "Tekstil, şampuan ve temizlik ürünleri", CreatedDate = DateTime.UtcNow };

                await context.Categories.AddRangeAsync(new[] { catOda, catGurme, catSpa, catMinibar, catHijyen });
                await context.SaveChangesAsync();
            }
            else
            {
                catOda = existingCats.FirstOrDefault(c => c.Name.Contains("Oda")) ?? existingCats.First();
                catGurme = existingCats.FirstOrDefault(c => c.Name.Contains("Restoran")) ?? existingCats.First();
                catSpa = existingCats.FirstOrDefault(c => c.Name.Contains("SPA")) ?? existingCats.First();
                catMinibar = existingCats.FirstOrDefault(c => c.Name.Contains("Mini")) ?? existingCats.First();
                catHijyen = existingCats.FirstOrDefault(c => c.Name.Contains("Kat")) ?? existingCats.First();
            }

            // Products
            var existingProducts = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();
            if (!existingProducts.Any())
            {
                var products = new List<Product>
                {
                    new Product { TenantId = tId, CategoryId = catOda.Id, ProductCode = "PRD-HOT-001", ProductName = "Deluxe Suit Taş Konak Oda (Gecelik)", ProductType = ProductType.Service, Unit = Unit.Piece, CurrentStock = 120, PurchasePrice = 800, SalePrice = 3500, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catOda.Id, ProductCode = "PRD-HOT-002", ProductName = "Standart King Bed Room (Gecelik)", ProductType = ProductType.Service, Unit = Unit.Piece, CurrentStock = 250, PurchasePrice = 500, SalePrice = 2200, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catGurme.Id, ProductCode = "PRD-HOT-003", ProductName = "Serpme Mardin Kahvaltısı (Kişi Başı)", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 400, PurchasePrice = 120, SalePrice = 450, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catGurme.Id, ProductCode = "PRD-HOT-004", ProductName = "Geleneksel Mardin Kaburga Dolması Menü", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 180, PurchasePrice = 300, SalePrice = 850, PurchaseVatRate = VatRate.Ten, SaleVatRate = VatRate.Ten, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catSpa.Id, ProductCode = "PRD-HOT-005", ProductName = "Tarihi Türk Hamamı & Kese Köpük Masajı", ProductType = ProductType.Service, Unit = Unit.Piece, CurrentStock = 90, PurchasePrice = 250, SalePrice = 1200, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // Customers & Suppliers
            var existingCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            if (!existingCusts.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer { TenantId = tId, CustomerCode = "CAR-HOT-001", Title = "Setur Turizm ve Seyahat A.Ş.", CustomerType = CustomerType.Customer, TaxOffice = "Mardin V.D.", TaxNumber = "7650123456", Phone = "0850 210 0738", City = "Mardin", Balance = 145000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-HOT-002", Title = "ETS Tur Gayrimenkul ve Turizm A.Ş.", CustomerType = CustomerType.Customer, TaxOffice = "Kadıköy V.D.", TaxNumber = "3880456123", Phone = "0444 0 387", City = "İstanbul", Balance = 220000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-HOT-003", Title = "Mardin Yöresel Gıda & Et Tedarik Şti.", CustomerType = CustomerType.Supplier, TaxOffice = "Artuklu V.D.", TaxNumber = "6110987654", Phone = "0482 213 1020", City = "Mardin", Balance = -64000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-HOT-004", Title = "Eczacıbaşı Endüstriyel Hijyen & Temizlik A.Ş.", CustomerType = CustomerType.Supplier, TaxOffice = "Büyük Mükellefler V.D.", TaxNumber = "3230011223", Phone = "0212 371 7000", City = "İstanbul", Balance = -38500, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-HOT-005", Title = "Türk Hava Yolları Anonim Ortaklığı", CustomerType = CustomerType.Customer, TaxOffice = "Zincirlikuyu V.D.", TaxNumber = "8790010011", Phone = "0212 463 6363", City = "İstanbul", Balance = 88000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // Cash Accounts
            var existingCash = await context.CashAccounts.IgnoreQueryFilters().Where(ca => ca.TenantId == tId).ToListAsync();
            if (!existingCash.Any())
            {
                var cashAccounts = new List<CashAccount>
                {
                    new CashAccount { TenantId = tId, AccountCode = "KSA-HOT-01", AccountName = "Otel Resepsiyon Nakit Kasa", AccountType = CashAccountType.Kasa, Balance = 45800, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Giriş Resepsiyon Nakit Kasa", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "KSA-HOT-02", AccountName = "SPA & Restoran POS Kasa", AccountType = CashAccountType.Kasa, Balance = 28400, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Günübirlik POS Tahsilat Kasası", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-HOT-01", AccountName = "Garanti BBVA Mardin Şubesi Ticari", AccountType = CashAccountType.Banka, BankName = "Garanti BBVA", Iban = "TR45 0006 2000 1234 5678 9000 01", Balance = 840000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Otel Ana Tahsilat ve Havale Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-HOT-02", AccountName = "Yapı Kredi Otel Acente Hesabı", AccountType = CashAccountType.Banka, BankName = "Yapı Kredi", Iban = "TR88 0006 7000 9876 5432 1000 02", Balance = 390000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Tur Acenteleri Hakediş Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-HOT-03", AccountName = "T.C. Ziraat Bankası Ödeme Hesabı", AccountType = CashAccountType.Banka, BankName = "Ziraat Bankası", Iban = "TR11 0001 0005 6789 0123 4000 03", Balance = 115000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Tedarikçi ve Maaş Ödeme Hesabı", CreatedDate = DateTime.UtcNow }
                };
                await context.CashAccounts.AddRangeAsync(cashAccounts);
                await context.SaveChangesAsync();
            }

            // Employees
            var existingEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
            if (!existingEmp.Any())
            {
                var employees = new List<Employee>
                {
                    new Employee { TenantId = tId, EmployeeCode = "PRS-HOT-01", FirstName = "Mehmet Ali", LastName = "Artuklu", TCKN = "66778899001", Department = EmployeeDepartment.Management, Title = "Otel Genel Müdürü", Salary = 65000, HireDate = DateTime.UtcNow.AddYears(-4), Phone = "0532 999 0101", Email = "mehmetali@mardinotel.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-HOT-02", FirstName = "Ayşe", LastName = "Şimşek", TCKN = "77889900112", Department = EmployeeDepartment.Sales, Title = "Ön Büro & Resepsiyon Müdürü", Salary = 38000, HireDate = DateTime.UtcNow.AddYears(-2), Phone = "0532 999 0202", Email = "ayse.simsek@mardinotel.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-HOT-03", FirstName = "Mahmut", LastName = "Kabadayı", TCKN = "88990011223", Department = EmployeeDepartment.Warehouse, Title = "Aşçıbaşı / Executive Chef", Salary = 45000, HireDate = DateTime.UtcNow.AddYears(-3), Phone = "0532 999 0303", Email = "mahmut.k@mardinotel.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-HOT-04", FirstName = "Zeynep", LastName = "Kaya", TCKN = "99001122334", Department = EmployeeDepartment.Warehouse, Title = "Kat Hizmetleri Şefi (Housekeeping)", Salary = 28000, HireDate = DateTime.UtcNow.AddYears(-1), Phone = "0532 999 0404", Email = "zeynep.k@mardinotel.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-HOT-05", FirstName = "Caner", LastName = "Yıldız", TCKN = "10112233445", Department = EmployeeDepartment.Sales, Title = "SPA & Wellness Müdürü", Salary = 32000, HireDate = DateTime.UtcNow.AddMonths(-9), Phone = "0532 999 0505", Email = "caner.y@mardinotel.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Employees.AddRangeAsync(employees);
                await context.SaveChangesAsync();
            }

            // Shifts
            var existingShifts = await context.Shifts.IgnoreQueryFilters().Where(s => s.TenantId == tId).ToListAsync();
            if (!existingShifts.Any())
            {
                var dbEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
                if (dbEmp.Count >= 5)
                {
                    var shifts = new List<Shift>
                    {
                        new Shift { TenantId = tId, ShiftCode = "VRD-HOT-01", EmployeeId = dbEmp[0].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(17, 30, 0), ShiftType = ShiftType.FullDay, Status = ShiftStatus.Active, Notes = "Genel Yönetim Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-HOT-02", EmployeeId = dbEmp[1].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(7, 0, 0), EndTime = new TimeSpan(15, 30, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Completed, Notes = "Resepsiyon Sabah Gelen Müşteri Check-In Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-HOT-03", EmployeeId = dbEmp[2].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(11, 0, 0), EndTime = new TimeSpan(21, 0, 0), ShiftType = ShiftType.Evening, Status = ShiftStatus.Active, Notes = "Mutfak Akşam Yemeği Servis Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-HOT-04", EmployeeId = dbEmp[3].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 30, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Planned, Notes = "Oda Temizlik ve Kat Kontrol Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-HOT-05", EmployeeId = dbEmp[4].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(22, 0, 0), ShiftType = ShiftType.Afternoon, Status = ShiftStatus.Planned, Notes = "SPA ve Türk Hamamı Akşam Seansı Vardiyası", CreatedDate = DateTime.UtcNow }
                    };
                    await context.Shifts.AddRangeAsync(shifts);
                    await context.SaveChangesAsync();
                }
            }

            // Income & Expenses
            var existingIE = await context.IncomeExpenses.IgnoreQueryFilters().Where(ie => ie.TenantId == tId).ToListAsync();
            if (!existingIE.Any())
            {
                var incomeExpenses = new List<IncomeExpense>
                {
                    new IncomeExpense { TenantId = tId, TransactionCode = "GLR-HOT-001", Type = IncomeExpenseType.Income, CategoryName = "Düğün & Davet Organizasyon Geliri", Amount = 180000, Description = "Tarihi Taş Bahçe Düğün Organizasyon Paket Geliri", TransactionDate = DateTime.UtcNow.AddDays(-3), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-HOT-001", Type = IncomeExpenseType.Expense, CategoryName = "Çamaşırhane Hizmet Gideri", Amount = 24500, Description = "Çarşaf ve Havlu Yıkama / Kuru Temizleme Bedeli", TransactionDate = DateTime.UtcNow.AddDays(-6), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-HOT-002", Type = IncomeExpenseType.Expense, CategoryName = "Mutfak Doğalgaz & Enerji Gideri", Amount = 42000, Description = "Otel Isınma ve Mutfak Fırınları Gas Kullanım Bedeli", TransactionDate = DateTime.UtcNow.AddDays(-4), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GLR-HOT-002", Type = IncomeExpenseType.Income, CategoryName = "Günübirlik SPA & Hamam Satışları", Amount = 48500, Description = "Dışarıdan Gelen Misafir SPA Paket Tahsilatları", TransactionDate = DateTime.UtcNow.AddDays(-1), CreatedDate = DateTime.UtcNow }
                };
                await context.IncomeExpenses.AddRangeAsync(incomeExpenses);
                await context.SaveChangesAsync();
            }

            // Invoices
            var existingInv = await context.Invoices.IgnoreQueryFilters().Where(i => i.TenantId == tId).ToListAsync();
            if (!existingInv.Any())
            {
                var dbCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
                var dbProds = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();

                if (dbCusts.Count >= 5 && dbProds.Count >= 5)
                {
                    var inv1 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "SFT-2026-HOT01",
                        InvoiceType = InvoiceType.Sales,
                        CustomerId = dbCusts[0].Id, // Setur Turizm
                        InvoiceDate = DateTime.UtcNow.AddDays(-5),
                        SubTotal = 131818.18m,
                        VatTotal = 13181.82m,
                        DiscountTotal = 0,
                        GrandTotal = 145000,
                        Status = InvoiceStatus.Approved,
                        Description = "Setur Kültür Turu Konaklama ve Kahvaltı Hizmet Faturası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[0].Id, Quantity = 40, UnitPrice = 3500, VatRate = 10, VatAmount = 14000, LineTotal = 154000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    var inv2 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "SFT-2026-HOT02",
                        InvoiceType = InvoiceType.Sales,
                        CustomerId = dbCusts[1].Id, // ETS Tur
                        InvoiceDate = DateTime.UtcNow.AddDays(-2),
                        SubTotal = 200000,
                        VatTotal = 20000,
                        DiscountTotal = 0,
                        GrandTotal = 220000,
                        Status = InvoiceStatus.Approved,
                        Description = "ETS Tur Taş Konak Suit ve Kongre Salonu Tahsis Faturası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[1].Id, Quantity = 100, UnitPrice = 2200, VatRate = 10, VatAmount = 22000, LineTotal = 242000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    var inv3 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "AFT-2026-HOT01",
                        InvoiceType = InvoiceType.Purchase,
                        CustomerId = dbCusts[2].Id, // Mardin Yöresel Gıda
                        InvoiceDate = DateTime.UtcNow.AddDays(-7),
                        SubTotal = 58181.82m,
                        VatTotal = 5818.18m,
                        DiscountTotal = 0,
                        GrandTotal = 64000,
                        Status = InvoiceStatus.Approved,
                        Description = "Otel mutfağı yöresel kaburga eti ve kahvaltılık malzeme alımı",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[3].Id, Quantity = 75, UnitPrice = 850, VatRate = 10, VatAmount = 6375, LineTotal = 70125, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    await context.Invoices.AddRangeAsync(inv1, inv2, inv3);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("MARD INN SEED ERROR: " + ex.Message);
        }
    }
    #endregion

    #region 3. AK POLİMER SANAYİ DATA SEEDER (aycapolimer@polimer.com)
    public static async Task SeedAkPolimerDataAsync(AppDbContext context)
    {
        try
        {
            var existingUser = await context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "aycapolimer@polimer.com");
            Tenant tenant;
            User user;

            if (existingUser != null)
            {
                user = existingUser;
                tenant = await context.Tenants.IgnoreQueryFilters().FirstAsync(t => t.Id == user.TenantId);
            }
            else
            {
                var maxId = await context.Tenants.IgnoreQueryFilters().AnyAsync() 
                    ? await context.Tenants.IgnoreQueryFilters().MaxAsync(t => t.Id) 
                    : 0;
                int newTenantId = maxId + 1;

                tenant = new Tenant
                {
                    TenantCode = "TEN-003",
                    CompanyName = "AK Polimer Plastik ve Kimya Sanayi A.Ş.",
                    TradeTitle = "AK Polimer Hammadde ve Plastik Enjeksiyon San. Tic. A.Ş.",
                    TaxNumber = "0890123456",
                    TaxOffice = "Gebze İhtisas Vergi Dairesi",
                    Sector = "Kimya, Plastik & Polimer Sanayi",
                    Phone = "0262 644 8000",
                    Email = "aycapolimer@polimer.com",
                    City = "Kocaeli",
                    Address = "Gebze Plastikçiler OSB 4. Cadde No:12 Gebze / Kocaeli",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Tenants.AddAsync(tenant);
                await context.SaveChangesAsync();

                user = new User
                {
                    TenantId = tenant.Id,
                    Email = "aycapolimer@polimer.com",
                    PasswordHash = Application.Services.AuthService.HashPassword("Musteri123!"),
                    FullName = "Ayça Polat",
                    Role = UserRole.Admin,
                    Title = "Genel Müdür & Kimya Yüksek Mühendisi (Tenant Admin)",
                    PhoneNumber = "0533 111 2233",
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };
                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();
            }

            int tId = tenant.Id;

            // Warehouses
            var existingWhs = await context.Warehouses.IgnoreQueryFilters().Where(w => w.TenantId == tId).ToListAsync();
            if (!existingWhs.Any())
            {
                var warehouses = new List<Warehouse>
                {
                    new Warehouse { TenantId = tId, WarehouseCode = "POL-DEP-01", Name = "Hammadde Silo & Granül Deposu", Address = "Fabrika A Blok Silolar", City = "Kocaeli", Phone = "0262 644 8001", IsDefault = true, Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "POL-DEP-02", Name = "Mamul Ürün & Enjeksiyon Deposu", Address = "Fabrika B Blok Üretim Çıkışı", City = "Kocaeli", Phone = "0262 644 8002", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "POL-DEP-03", Name = "Hurda & Geri Dönüşüm Kırma Deposu", Address = "Geri Dönüşüm Tesis Katı", City = "Kocaeli", Phone = "0262 644 8003", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Warehouse { TenantId = tId, WarehouseCode = "POL-DEP-04", Name = "Sevkiyat & Lojistik Deposu", Address = "Kantar & Tır Yükleme Sahası", City = "Kocaeli", Phone = "0262 644 8004", Status = WarehouseStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Warehouses.AddRangeAsync(warehouses);
                await context.SaveChangesAsync();
            }

            // Categories
            var existingCats = await context.Categories.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            Category catGranul, catKalip, catStrech, catPalet, catKiricilar;
            if (!existingCats.Any())
            {
                catGranul = new Category { TenantId = tId, Name = "Polimer Hammaddeler (HDPE, PP, PVC)", Description = "Petrokimya granül plastik hammaddeler", CreatedDate = DateTime.UtcNow };
                catKalip = new Category { TenantId = tId, Name = "Enjeksiyon Kalıp & Otomotiv Parçaları", Description = "Otomotiv ve beyaz eşya enjeksiyon parçaları", CreatedDate = DateTime.UtcNow };
                catStrech = new Category { TenantId = tId, Name = "Ambalaj & Tarım Streç Filmi", Description = "Endüstriyel sarım ve silaj streçleri", CreatedDate = DateTime.UtcNow };
                catPalet = new Category { TenantId = tId, Name = "Endüstriyel Plastik Palet & Konteynır", Description = "Heavy-Duty lojistik paletler", CreatedDate = DateTime.UtcNow };
                catKiricilar = new Category { TenantId = tId, Name = "Geri Dönüşüm Çapak & Kırma", Description = "Kırma granül ve çapak hammadde", CreatedDate = DateTime.UtcNow };

                await context.Categories.AddRangeAsync(new[] { catGranul, catKalip, catStrech, catPalet, catKiricilar });
                await context.SaveChangesAsync();
            }
            else
            {
                catGranul = existingCats.FirstOrDefault(c => c.Name.Contains("Polimer")) ?? existingCats.First();
                catKalip = existingCats.FirstOrDefault(c => c.Name.Contains("Enjeksiyon")) ?? existingCats.First();
                catStrech = existingCats.FirstOrDefault(c => c.Name.Contains("Streç")) ?? existingCats.First();
                catPalet = existingCats.FirstOrDefault(c => c.Name.Contains("Palet")) ?? existingCats.First();
                catKiricilar = existingCats.FirstOrDefault(c => c.Name.Contains("Geri")) ?? existingCats.First();
            }

            // Products
            var existingProducts = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();
            if (!existingProducts.Any())
            {
                var products = new List<Product>
                {
                    new Product { TenantId = tId, CategoryId = catGranul.Id, ProductCode = "PRD-POL-001", ProductName = "HDPE Polietilen Granül Hammadde (Ton)", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 85, PurchasePrice = 38000, SalePrice = 48500, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catGranul.Id, ProductCode = "PRD-POL-002", ProductName = "Polypropylene (PP) Enjeksiyonluk Granül (Ton)", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 120, PurchasePrice = 41000, SalePrice = 52000, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catPalet.Id, ProductCode = "PRD-POL-003", ProductName = "Endüstriyel Heavy-Duty Plastik Palet 120x100cm", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 450, PurchasePrice = 420, SalePrice = 850, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catKalip.Id, ProductCode = "PRD-POL-004", ProductName = "Otomotiv Yan Sanayi Plastik Enjeksiyon Parçası", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 2400, PurchasePrice = 65, SalePrice = 145, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Product { TenantId = tId, CategoryId = catStrech.Id, ProductCode = "PRD-POL-005", ProductName = "Silaj & Tarım Streç Filmi 500mm x 1500m", ProductType = ProductType.StockedProduct, Unit = Unit.Piece, CurrentStock = 310, PurchasePrice = 750, SalePrice = 1250, PurchaseVatRate = VatRate.Twenty, SaleVatRate = VatRate.Twenty, Status = ProductStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }

            // Customers & Suppliers
            var existingCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
            if (!existingCusts.Any())
            {
                var customers = new List<Customer>
                {
                    new Customer { TenantId = tId, CustomerCode = "CAR-POL-001", Title = "Ford Otosan Otomotiv Sanayi A.Ş.", CustomerType = CustomerType.Customer, TaxOffice = "Gölcük V.D.", TaxNumber = "3880011223", Phone = "0262 315 0000", City = "Kocaeli", Balance = 1850000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-POL-002", Title = "Arçelik Beyaz Eşya Sanayi A.Ş.", CustomerType = CustomerType.Customer, TaxOffice = "Çayırova V.D.", TaxNumber = "0880022334", Phone = "0262 677 1000", City = "Kocaeli", Balance = 940000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-POL-003", Title = "SOCAR Petkim Petrokimya Holding A.Ş.", CustomerType = CustomerType.Supplier, TaxOffice = "Aliağa V.D.", TaxNumber = "7290033445", Phone = "0232 616 1240", City = "İzmir", Balance = -1250000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-POL-004", Title = "Sasa Polyester Sanayi A.Ş.", CustomerType = CustomerType.Supplier, TaxOffice = "Seyhan V.D.", TaxNumber = "7480044556", Phone = "0322 441 0000", City = "Adana", Balance = -680000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Customer { TenantId = tId, CustomerCode = "CAR-POL-005", Title = "Beko Elektronik A.Ş.", CustomerType = CustomerType.Customer, TaxOffice = "Beylikdüzü V.D.", TaxNumber = "1660055667", Phone = "0212 871 0000", City = "İstanbul", Balance = 420000, Status = CustomerStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Customers.AddRangeAsync(customers);
                await context.SaveChangesAsync();
            }

            // Cash Accounts
            var existingCash = await context.CashAccounts.IgnoreQueryFilters().Where(ca => ca.TenantId == tId).ToListAsync();
            if (!existingCash.Any())
            {
                var cashAccounts = new List<CashAccount>
                {
                    new CashAccount { TenantId = tId, AccountCode = "BNK-POL-01", AccountName = "Akbank Gebze OSB Kurumsal Ticari", AccountType = CashAccountType.Banka, BankName = "Akbank", Iban = "TR15 0004 6000 1111 2222 3333 01", Balance = 3450000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Sanayi Şirketi Ana Mevduat Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "BNK-POL-02", AccountName = "Türkiye İş Bankası Gebze Sanayi", AccountType = CashAccountType.Banka, BankName = "İş Bankası", Iban = "TR64 0006 4000 4444 5555 6666 02", Balance = 1820000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Hammadde İthalat ve Akreditif Hesabı", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "KSA-POL-01", AccountName = "Fabrika İdari Merkez Kasa", AccountType = CashAccountType.Kasa, Balance = 65000, Currency = "TRY", Status = CashAccountStatus.Active, Description = "İdari Bina Küçük Nakit Kasa", CreatedDate = DateTime.UtcNow },
                    new CashAccount { TenantId = tId, AccountCode = "KSA-POL-02", AccountName = "Sevkiyat & Kantar Gişe Kasası", AccountType = CashAccountType.Kasa, Balance = 18500, Currency = "TRY", Status = CashAccountStatus.Active, Description = "Fabrika Kantar ve Nakliye Gişesi", CreatedDate = DateTime.UtcNow }
                };
                await context.CashAccounts.AddRangeAsync(cashAccounts);
                await context.SaveChangesAsync();
            }

            // Employees
            var existingEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
            if (!existingEmp.Any())
            {
                var employees = new List<Employee>
                {
                    new Employee { TenantId = tId, EmployeeCode = "PRS-POL-01", FirstName = "Ayça", LastName = "Polat", TCKN = "12345678901", Department = EmployeeDepartment.Management, Title = "Genel Müdür & Kimya Yük. Müh.", Salary = 120000, HireDate = DateTime.UtcNow.AddYears(-6), Phone = "0533 111 2233", Email = "ayca.polat@akpolimer.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-POL-02", FirstName = "Hakan", LastName = "Demir", TCKN = "23456789012", Department = EmployeeDepartment.Warehouse, Title = "Fabrika Üretim & Kalıp Müdürü", Salary = 75000, HireDate = DateTime.UtcNow.AddYears(-4), Phone = "0533 111 3344", Email = "hakan.d@akpolimer.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-POL-03", FirstName = "Murat", LastName = "Öztürk", TCKN = "34567890123", Department = EmployeeDepartment.Warehouse, Title = "Kıdemli Polimer Enjeksiyon Operatörü", Salary = 42000, HireDate = DateTime.UtcNow.AddYears(-3), Phone = "0533 111 4455", Email = "murat.o@akpolimer.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-POL-04", FirstName = "Sibel", LastName = "Çelik", TCKN = "45678901234", Department = EmployeeDepartment.Management, Title = "Kalite Kontrol & Laboratuvar Sorumlusu", Salary = 48000, HireDate = DateTime.UtcNow.AddYears(-2), Phone = "0533 111 5566", Email = "sibel.c@akpolimer.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow },
                    new Employee { TenantId = tId, EmployeeCode = "PRS-POL-05", FirstName = "Volkan", LastName = "Şahin", TCKN = "56789012345", Department = EmployeeDepartment.Sales, Title = "Lojistik & Depo Sorumlusu", Salary = 35000, HireDate = DateTime.UtcNow.AddYears(-1), Phone = "0533 111 6677", Email = "volkan.s@akpolimer.com", Status = EmployeeStatus.Active, CreatedDate = DateTime.UtcNow }
                };
                await context.Employees.AddRangeAsync(employees);
                await context.SaveChangesAsync();
            }

            // Shifts
            var existingShifts = await context.Shifts.IgnoreQueryFilters().Where(s => s.TenantId == tId).ToListAsync();
            if (!existingShifts.Any())
            {
                var dbEmp = await context.Employees.IgnoreQueryFilters().Where(e => e.TenantId == tId).ToListAsync();
                if (dbEmp.Count >= 5)
                {
                    var shifts = new List<Shift>
                    {
                        new Shift { TenantId = tId, ShiftCode = "VRD-POL-01", EmployeeId = dbEmp[0].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(17, 30, 0), ShiftType = ShiftType.FullDay, Status = ShiftStatus.Active, Notes = "Genel İdari Yönetim Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-POL-02", EmployeeId = dbEmp[1].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(16, 0, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Completed, Notes = "Fabrika 1. Üretim Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-POL-03", EmployeeId = dbEmp[2].Id, ShiftDate = DateTime.UtcNow.Date, StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(23, 59, 0), ShiftType = ShiftType.Evening, Status = ShiftStatus.Active, Notes = "Fabrika 2. Gece Enjeksiyon Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-POL-04", EmployeeId = dbEmp[3].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(17, 0, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Planned, Notes = "Laboratuvar Hammadde Test Vardiyası", CreatedDate = DateTime.UtcNow },
                        new Shift { TenantId = tId, ShiftCode = "VRD-POL-05", EmployeeId = dbEmp[4].Id, ShiftDate = DateTime.UtcNow.Date.AddDays(1), StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(16, 30, 0), ShiftType = ShiftType.Morning, Status = ShiftStatus.Planned, Notes = "Ford Otosan Tır Yükleme Lojistik Vardiyası", CreatedDate = DateTime.UtcNow }
                    };
                    await context.Shifts.AddRangeAsync(shifts);
                    await context.SaveChangesAsync();
                }
            }

            // Income & Expenses
            var existingIE = await context.IncomeExpenses.IgnoreQueryFilters().Where(ie => ie.TenantId == tId).ToListAsync();
            if (!existingIE.Any())
            {
                var incomeExpenses = new List<IncomeExpense>
                {
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-POL-001", Type = IncomeExpenseType.Expense, CategoryName = "Sanayi Yüksek Gerilim Elektrik Faturası", Amount = 340000, Description = "Fabrika Enjeksiyon Makineleri Elektrik Gideri", TransactionDate = DateTime.UtcNow.AddDays(-5), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-POL-002", Type = IncomeExpenseType.Expense, CategoryName = "OSB Kalıp Isıtma Doğalgaz Gideri", Amount = 185000, Description = "Kalıp Kurutma ve Fırınlama Gas Faturası", TransactionDate = DateTime.UtcNow.AddDays(-8), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GDR-POL-003", Type = IncomeExpenseType.Expense, CategoryName = "Ekstruder & Kalıp Bakım Gideri", Amount = 95000, Description = "Alman Enjeksiyon Makinesi Hidrolik Yağ ve Kalıp Bakımı", TransactionDate = DateTime.UtcNow.AddDays(-2), CreatedDate = DateTime.UtcNow },
                    new IncomeExpense { TenantId = tId, TransactionCode = "GLR-POL-001", Type = IncomeExpenseType.Income, CategoryName = "Geri Dönüşüm Hurda Plastik Satışı", Amount = 120000, Description = "Fabrika Kırma Çapak ve Takoz Plastik Satış Geliri", TransactionDate = DateTime.UtcNow.AddDays(-1), CreatedDate = DateTime.UtcNow }
                };
                await context.IncomeExpenses.AddRangeAsync(incomeExpenses);
                await context.SaveChangesAsync();
            }

            // Invoices
            var existingInv = await context.Invoices.IgnoreQueryFilters().Where(i => i.TenantId == tId).ToListAsync();
            if (!existingInv.Any())
            {
                var dbCusts = await context.Customers.IgnoreQueryFilters().Where(c => c.TenantId == tId).ToListAsync();
                var dbProds = await context.Products.IgnoreQueryFilters().Where(p => p.TenantId == tId).ToListAsync();

                if (dbCusts.Count >= 5 && dbProds.Count >= 5)
                {
                    var inv1 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "SFT-2026-POL01",
                        InvoiceType = InvoiceType.Sales,
                        CustomerId = dbCusts[0].Id, // Ford Otosan
                        InvoiceDate = DateTime.UtcNow.AddDays(-4),
                        SubTotal = 1541666.67m,
                        VatTotal = 308333.33m,
                        DiscountTotal = 0,
                        GrandTotal = 1850000,
                        Status = InvoiceStatus.Approved,
                        Description = "Ford Otosan Transit Plastik Enjeksiyon Tampon & Aksam Faturası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[3].Id, Quantity = 10000, UnitPrice = 145, VatRate = 20, VatAmount = 290000, LineTotal = 1740000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    var inv2 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "SFT-2026-POL02",
                        InvoiceType = InvoiceType.Sales,
                        CustomerId = dbCusts[1].Id, // Arçelik
                        InvoiceDate = DateTime.UtcNow.AddDays(-2),
                        SubTotal = 783333.33m,
                        VatTotal = 156666.67m,
                        DiscountTotal = 0,
                        GrandTotal = 940000,
                        Status = InvoiceStatus.Approved,
                        Description = "Arçelik Çamaşır Makinesi Plastik Kazan ve Ağır Sanayi Paleti",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[2].Id, Quantity = 1000, UnitPrice = 850, VatRate = 20, VatAmount = 170000, LineTotal = 1020000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    var inv3 = new Invoice
                    {
                        TenantId = tId,
                        InvoiceNumber = "AFT-2026-POL01",
                        InvoiceType = InvoiceType.Purchase,
                        CustomerId = dbCusts[2].Id, // Petkim SOCAR
                        InvoiceDate = DateTime.UtcNow.AddDays(-9),
                        SubTotal = 1041666.67m,
                        VatTotal = 208333.33m,
                        DiscountTotal = 0,
                        GrandTotal = 1250000,
                        Status = InvoiceStatus.Approved,
                        Description = "100 Ton Petrokimya HDPE ve PP Hammadde Silo Alım Faturası",
                        CreatedDate = DateTime.UtcNow,
                        Items = new List<InvoiceItem>
                        {
                            new InvoiceItem { TenantId = tId, ProductId = dbProds[0].Id, Quantity = 25, UnitPrice = 48500, VatRate = 20, VatAmount = 242500, LineTotal = 1455000, CreatedDate = DateTime.UtcNow }
                        }
                    };

                    await context.Invoices.AddRangeAsync(inv1, inv2, inv3);
                    await context.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("AK POLİMER SEED ERROR: " + ex.Message);
        }
    }
    #endregion

    public static async Task SeedCleanSlateAsync(AppDbContext context, string adminEmail = "admin@efbismuhasebe.com", string adminPassword = "Admin123!", string companyName = "Yeni Şirket")
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        // Super Admin + Yeni Şirket Admin
        var superAdmin = new User
        {
            TenantId = 0,
            Email = "superadmin@efbismuhasebe.com",
            PasswordHash = Application.Services.AuthService.HashPassword("SuperAdmin123!"),
            FullName = "Platform Super Admin",
            Role = UserRole.SuperAdmin,
            Title = "Sistem Sahibi & Genel Platform Yöneticisi",
            PhoneNumber = "0500 999 0000",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        var tenantAdmin = new User
        {
            TenantId = 1,
            Email = adminEmail,
            PasswordHash = Application.Services.AuthService.HashPassword(adminPassword),
            FullName = companyName + " Yöneticisi",
            Role = UserRole.Admin,
            Title = companyName + " Genel Müdürü",
            PhoneNumber = "0500 000 0000",
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        await context.Users.AddRangeAsync(superAdmin, tenantAdmin);
        await context.SaveChangesAsync();
    }
}
