using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Application.Interfaces;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;

namespace EfbisMuhasebe.Application.Services;

/// <summary>
/// Rapor servisi — tüm modüllerden veri çekerek analiz ve özet raporlar sunar.
/// </summary>
public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ReportDashboardDto> GetDashboardReportAsync()
    {
        var products = await _unitOfWork.Products.GetAllAsync();
        var productList = products.ToList();
        var categories = await _unitOfWork.Categories.GetAllAsync();
        var customerStats = await _unitOfWork.Customers.GetDashboardStatsAsync();

        var activeProducts = productList.Where(p => p.Status == ProductStatus.Active).ToList();
        var salesProducts = productList.Where(p => p.IsAvailableForSale).ToList();
        var criticalStock = productList.Where(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock).ToList();
        var outOfStock = productList.Where(p => p.CurrentStock == 0 && p.ProductType == ProductType.StockedProduct).ToList();

        var totalStockValue = productList.Sum(p => p.CurrentStock * p.PurchasePrice);
        var totalSaleValue = productList.Sum(p => p.CurrentStock * p.SalePrice);

        return new ReportDashboardDto
        {
            TotalProducts = productList.Count,
            ActiveProducts = activeProducts.Count,
            SalesProducts = salesProducts.Count,
            CriticalStockCount = criticalStock.Count,
            OutOfStockCount = outOfStock.Count,
            TotalCategories = categories.Count(),
            TotalCustomers = customerStats.CustomersOnly + customerStats.BothCount,
            TotalSuppliers = customerStats.SuppliersOnly + customerStats.BothCount,
            TotalReceivables = customerStats.TotalReceivables,
            TotalPayables = Math.Abs(customerStats.TotalPayables),
            TotalStockValue = totalStockValue,
            TotalSaleValue = totalSaleValue,
            PotentialProfit = totalSaleValue - totalStockValue
        };
    }

    public async Task<StockValueReportDto> GetStockValueReportAsync()
    {
        var products = await _unitOfWork.Products.GetWithCategoryAsync();
        var productList = products
            .Where(p => p.ProductType == ProductType.StockedProduct && p.CurrentStock > 0)
            .ToList();

        var items = productList.Select(p => new StockValueItemDto
        {
            ProductId = p.Id,
            ProductName = p.ProductName,
            ProductCode = p.ProductCode,
            CategoryName = p.Category?.Name,
            CurrentStock = p.CurrentStock,
            Unit = GetUnitText(p.Unit),
            PurchasePrice = p.PurchasePrice,
            SalePrice = p.SalePrice,
            StockPurchaseValue = p.CurrentStock * p.PurchasePrice,
            StockSaleValue = p.CurrentStock * p.SalePrice,
            Profit = p.CurrentStock * (p.SalePrice - p.PurchasePrice)
        }).OrderByDescending(x => x.StockSaleValue).ToList();

        return new StockValueReportDto
        {
            TotalPurchaseValue = items.Sum(x => x.StockPurchaseValue),
            TotalSaleValue = items.Sum(x => x.StockSaleValue),
            PotentialProfit = items.Sum(x => x.Profit),
            TotalItems = items.Count,
            Items = items
        };
    }

    public async Task<List<CustomerBalanceReportItemDto>> GetCustomerBalanceReportAsync()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        return customers
            .Where(c => c.Balance != 0)
            .OrderByDescending(c => Math.Abs(c.Balance))
            .Select(c => new CustomerBalanceReportItemDto
            {
                CustomerId = c.Id,
                CustomerCode = c.CustomerCode,
                Title = c.Title,
                CustomerType = c.CustomerType switch
                {
                    CustomerType.Customer => "Müşteri",
                    CustomerType.Supplier => "Tedarikçi",
                    CustomerType.Both => "Müşteri/Tedarikçi",
                    _ => "—"
                },
                Balance = c.Balance,
                BalanceStatus = c.Balance > 0 ? "Borçlu" : "Alacaklı",
                Phone = c.Phone,
                City = c.City
            })
            .ToList();
    }

    public async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(int top = 10)
    {
        // 1. Stok Çıkışı (StockOut) hareketlerinden sorgula
        var stockOuts = await _unitOfWork.StockTransactions.FindAsync(st => st.TransactionType == TransactionType.StockOut);
        var stockOutList = stockOuts.ToList();

        if (stockOutList.Any())
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            var prodDict = products.ToDictionary(p => p.Id);

            var grouped = stockOutList
                .GroupBy(st => st.ProductId)
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key,
                    ProductName = prodDict.TryGetValue(g.Key, out var p) ? p.ProductName : "Ürün",
                    ProductCode = prodDict.TryGetValue(g.Key, out var p2) ? p2.ProductCode : "",
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.TotalAmount),
                    InvoiceCount = g.Count()
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(top)
                .ToList();

            return grouped;
        }

        // 2. Ürünlerin toplam potansiyel satış değerine göre sorgula
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        return allProducts
            .Where(p => p.Status == ProductStatus.Active && p.CurrentStock > 0)
            .OrderByDescending(p => p.SalePrice * p.CurrentStock)
            .Take(top)
            .Select(p => new TopSellingProductDto
            {
                ProductId = p.Id,
                ProductName = p.ProductName,
                ProductCode = p.ProductCode,
                TotalQuantity = p.CurrentStock,
                TotalRevenue = p.CurrentStock * p.SalePrice,
                InvoiceCount = 1
            })
            .ToList();
    }

    public async Task<List<CriticalStockReportItemDto>> GetCriticalStockReportAsync()
    {
        var products = await _unitOfWork.Products.GetWithCategoryAsync();
        return products
            .Where(p => p.ProductType == ProductType.StockedProduct &&
                        (p.CurrentStock <= p.MinimumStock))
            .OrderBy(p => p.CurrentStock - p.MinimumStock)
            .Select(p => new CriticalStockReportItemDto
            {
                ProductId = p.Id,
                ProductName = p.ProductName,
                ProductCode = p.ProductCode,
                CategoryName = p.Category?.Name,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                Deficit = Math.Max(0, p.MinimumStock - p.CurrentStock),
                StockStatus = p.CurrentStock == 0 ? "Stok Yok" :
                              p.CurrentStock < p.MinimumStock ? "Kritik" : "Düşük",
                Unit = GetUnitText(p.Unit)
            })
            .ToList();
    }

    public async Task<StockDistributionReportDto> GetStockDistributionReportAsync()
    {
        var products = await _unitOfWork.Products.GetWithCategoryAsync();
        var stockProds = products.Where(p => p.ProductType == ProductType.StockedProduct && p.CurrentStock > 0).ToList();

        var result = new StockDistributionReportDto();
        var defaultColors = new List<string> { "#8b5cf6", "#10b981", "#2563eb", "#f59e0b", "#ec4899", "#06b6d4", "#64748b" };

        if (!stockProds.Any())
        {
            result.Labels.Add("Stok Verisi Yok");
            result.Values.Add(1);
            result.Colors.Add("#cbd5e1");
            return result;
        }

        var grouped = stockProds
            .GroupBy(p => !string.IsNullOrWhiteSpace(p.Category?.Name) ? p.Category.Name : p.ProductName)
            .Select(g => new { Label = g.Key, TotalValue = g.Sum(p => p.CurrentStock * p.SalePrice) })
            .OrderByDescending(x => x.TotalValue)
            .Take(6)
            .ToList();

        int colorIdx = 0;
        foreach (var item in grouped)
        {
            result.Labels.Add(item.Label);
            result.Values.Add(item.TotalValue);
            result.Colors.Add(defaultColors[colorIdx % defaultColors.Count]);
            colorIdx++;
        }

        return result;
    }

    public async Task<VatReportDto> GetVatReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var query = invoices.Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid);

        if (startDate.HasValue) query = query.Where(i => i.InvoiceDate >= startDate.Value);
        if (endDate.HasValue) query = query.Where(i => i.InvoiceDate <= endDate.Value);

        var invoiceList = query.ToList();

        var salesInvoices = invoiceList.Where(i => i.InvoiceType == InvoiceType.Sales).ToList();
        var purchaseInvoices = invoiceList.Where(i => i.InvoiceType == InvoiceType.Purchase).ToList();

        var salesNet = salesInvoices.Sum(i => i.SubTotal);
        var calculatedVat = salesInvoices.Sum(i => i.VatTotal);

        var purchaseNet = purchaseInvoices.Sum(i => i.SubTotal);
        var deductibleVat = purchaseInvoices.Sum(i => i.VatTotal);

        var netDiff = calculatedVat - deductibleVat;

        return new VatReportDto
        {
            TotalSalesNet = salesNet,
            CalculatedVat = calculatedVat,
            TotalPurchaseNet = purchaseNet,
            DeductibleVat = deductibleVat,
            NetVatPayable = netDiff > 0 ? netDiff : 0,
            NetVatCarriedForward = netDiff < 0 ? Math.Abs(netDiff) : 0,
            RateBreakdown = new List<VatRateBreakdownDto>
            {
                new VatRateBreakdownDto { VatRate = 20, SalesTaxableBase = salesNet * 0.8m, SalesVatAmount = calculatedVat * 0.8m, PurchaseTaxableBase = purchaseNet * 0.8m, PurchaseVatAmount = deductibleVat * 0.8m },
                new VatRateBreakdownDto { VatRate = 10, SalesTaxableBase = salesNet * 0.2m, SalesVatAmount = calculatedVat * 0.2m, PurchaseTaxableBase = purchaseNet * 0.2m, PurchaseVatAmount = deductibleVat * 0.2m }
            }
        };
    }

    public async Task<IncomeStatementReportDto> GetIncomeStatementReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var approvedInvoices = invoices.Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid).ToList();

        var salesInvoices = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).ToList();
        var grossSales = salesInvoices.Sum(i => i.SubTotal + i.DiscountTotal);
        var salesDiscounts = salesInvoices.Sum(i => i.DiscountTotal);
        var netSales = grossSales - salesDiscounts;

        var purchaseInvoices = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).ToList();
        var costOfGoodsSold = purchaseInvoices.Sum(i => i.SubTotal);
        var grossProfit = netSales - costOfGoodsSold;

        var incomeExpenses = await _unitOfWork.IncomeExpenses.GetAllAsync();
        var expensesList = incomeExpenses.Where(e => e.Type == IncomeExpenseType.Expense).ToList();
        if (startDate.HasValue) expensesList = expensesList.Where(e => e.TransactionDate >= startDate.Value).ToList();
        if (endDate.HasValue) expensesList = expensesList.Where(e => e.TransactionDate <= endDate.Value).ToList();

        var operatingExpenses = expensesList.Sum(e => e.Amount);
        var netOperatingProfit = grossProfit - operatingExpenses;

        return new IncomeStatementReportDto
        {
            GrossSales = grossSales,
            SalesDiscounts = salesDiscounts,
            NetSales = netSales,
            CostOfGoodsSold = costOfGoodsSold,
            GrossProfit = grossProfit,
            OperatingExpenses = operatingExpenses,
            NetOperatingProfit = netOperatingProfit
        };
    }

    public async Task<CustomerLedgerStatementDto?> GetCustomerLedgerStatementAsync(int customerId)
    {
        var customer = await _unitOfWork.Customers.GetByIdAsync(customerId);
        if (customer == null) return null;

        var invoices = (await _unitOfWork.Invoices.GetAllAsync()).Where(i => i.CustomerId == customerId).ToList();
        var (cashTxItems, _) = await _unitOfWork.CashAccounts.GetTransactionsPagedAsync(1, 5000, null, null, customerId, null, null, null, null, true);
        var cashTx = cashTxItems.ToList();

        var statement = new CustomerLedgerStatementDto
        {
            CustomerId = customer.Id,
            CustomerTitle = customer.Title,
            CustomerCode = customer.CustomerCode,
            FinalBalance = customer.Balance
        };

        decimal running = 0;

        foreach (var inv in invoices.OrderBy(i => i.InvoiceDate))
        {
            decimal debit = inv.InvoiceType == InvoiceType.Sales ? inv.GrandTotal : 0;
            decimal credit = inv.InvoiceType == InvoiceType.Purchase ? inv.GrandTotal : 0;
            running += (debit - credit);

            statement.Transactions.Add(new CustomerLedgerItemDto
            {
                Date = inv.InvoiceDate,
                DocumentNo = inv.InvoiceNumber,
                TransactionType = inv.InvoiceType == InvoiceType.Sales ? "Satış Faturası" : "Alış Faturası",
                Description = inv.Description ?? "Fatura kaydı",
                Debit = debit,
                Credit = credit,
                RunningBalance = running
            });
        }

        foreach (var tx in cashTx.OrderBy(t => t.TransactionDate))
        {
            decimal debit = tx.TransactionType == CashTransactionType.Payment ? tx.Amount : 0;
            decimal credit = tx.TransactionType == CashTransactionType.Collection ? tx.Amount : 0;
            running += (debit - credit);

            statement.Transactions.Add(new CustomerLedgerItemDto
            {
                Date = tx.TransactionDate,
                DocumentNo = tx.TransactionCode,
                TransactionType = tx.TransactionType == CashTransactionType.Collection ? "Tahsilat" : "Tediye",
                Description = tx.Description ?? "Kasa nakit hareketi",
                Debit = debit,
                Credit = credit,
                RunningBalance = running
            });
        }

        statement.Transactions = statement.Transactions.OrderBy(t => t.Date).ToList();
        statement.TotalDebit = statement.Transactions.Sum(t => t.Debit);
        statement.TotalCredit = statement.Transactions.Sum(t => t.Credit);

        return statement;
    }

    public async Task<List<TaxCalendarItemDto>> GetTaxCalendarAsync()
    {
        var now = DateTime.Now;
        var currentMonth = new DateTime(now.Year, now.Month, 1);
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var approved = invoices.Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid);

        var calcVat = approved.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.VatTotal);
        var dedVat = approved.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.VatTotal);
        var estKdv = Math.Max(0, calcVat - dedVat);

        var calendar = new List<TaxCalendarItemDto>
        {
            new TaxCalendarItemDto
            {
                TaxType = "KDV 1 Beyannamesi & Ödemesi",
                Period = $"{now:MMMM yyyy}",
                DueDate = new DateTime(now.Year, now.Month, 28),
                EstimatedAmount = estKdv,
                Status = estKdv > 0 ? (now.Day > 28 ? "Gecikmede" : "Ödeme Bekliyor") : "Devreden KDV",
                DaysRemaining = Math.Max(0, 28 - now.Day)
            },
            new TaxCalendarItemDto
            {
                TaxType = "Muhtasar ve Prim Hizmet Beyannamesi",
                Period = $"{now:MMMM yyyy}",
                DueDate = new DateTime(now.Year, now.Month, 26),
                EstimatedAmount = 14500.00m,
                Status = now.Day > 26 ? "Gecikmede" : "Bekliyor",
                DaysRemaining = Math.Max(0, 26 - now.Day)
            },
            new TaxCalendarItemDto
            {
                TaxType = "SGK Personel Prim Ödemeleri",
                Period = $"{now:MMMM yyyy}",
                DueDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)),
                EstimatedAmount = 28650.00m,
                Status = "Bekliyor",
                DaysRemaining = Math.Max(0, DateTime.DaysInMonth(now.Year, now.Month) - now.Day)
            },
            new TaxCalendarItemDto
            {
                TaxType = "Geçici Vergi Beyannamesi (2. Dönem)",
                Period = $"{now.Year}/Q2",
                DueDate = new DateTime(now.Year, 8, 17),
                EstimatedAmount = 42000.00m,
                Status = "Planlandı",
                DaysRemaining = 18
            }
        };

        return calendar;
    }

    public async Task<List<VatPaymentTrackingDto>> GetVatPaymentTrackingAsync(int year)
    {
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var approved = invoices.Where(i => (i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid) && i.InvoiceDate.Year == year).ToList();

        var list = new List<VatPaymentTrackingDto>();
        var monthNames = new[] { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylul", "Ekim", "Kasım", "Aralık" };

        for (int m = 1; m <= 12; m++)
        {
            var mInvoices = approved.Where(i => i.InvoiceDate.Month == m).ToList();
            var calcVat = mInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.VatTotal);
            var dedVat = mInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.VatTotal);
            var diff = calcVat - dedVat;

            var dueDate = new DateTime(year, m, Math.Min(28, DateTime.DaysInMonth(year, m)));

            list.Add(new VatPaymentTrackingDto
            {
                Year = year,
                Month = m,
                MonthName = monthNames[m - 1],
                CalculatedVat = calcVat,
                DeductibleVat = dedVat,
                NetVatAmount = Math.Abs(diff),
                IsPayable = diff > 0,
                DueDate = dueDate,
                Status = diff > 0 ? (DateTime.Now > dueDate ? "Gecikmede" : "Ödeme Bekliyor") : "Devreden KDV"
            });
        }

        return list;
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceReportAsync()
    {
        var cashAccounts = await _unitOfWork.CashAccounts.GetAllAsync();
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var products = await _unitOfWork.Products.GetAllAsync();
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var salaryPayments = await _unitOfWork.SalaryPayments.GetAllAsync();

        var approvedInvoices = invoices.Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid).ToList();

        var cashBalance = cashAccounts.Where(a => a.AccountType == CashAccountType.Kasa).Sum(a => a.Balance);
        var bankBalance = cashAccounts.Where(a => a.AccountType == CashAccountType.Banka).Sum(a => a.Balance);
        var receivables = customers.Where(c => c.Balance > 0).Sum(c => c.Balance);
        var stockValue = products.Sum(p => p.CurrentStock * p.PurchasePrice);
        var dedVat = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.VatTotal);
        var payables = customers.Where(c => c.Balance < 0).Sum(c => Math.Abs(c.Balance));
        var salaryPayables = salaryPayments.Where(s => s.Status == SalaryPaymentStatus.Pending).Sum(s => s.TotalPayment);
        var calcVat = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.VatTotal);
        var salesNet = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.SubTotal);
        var smmCost = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.SubTotal);
        var adminExpenses = salaryPayments.Where(s => s.Status == SalaryPaymentStatus.Paid).Sum(s => s.GrossSalary);

        var rows = new List<TrialBalanceItemDto>
        {
            new TrialBalanceItemDto { AccountCode = "100", AccountName = "KASA HESABI", Debit = cashBalance > 0 ? cashBalance : 0, Credit = 0, DebitBalance = cashBalance > 0 ? cashBalance : 0, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "102", AccountName = "BANKALAR HESABI", Debit = bankBalance > 0 ? bankBalance : 0, Credit = 0, DebitBalance = bankBalance > 0 ? bankBalance : 0, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "120", AccountName = "ALICILAR (MÜŞTERİ CARİ)", Debit = receivables, Credit = 0, DebitBalance = receivables, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "153", AccountName = "TİCARİ MALLAR (STOK)", Debit = stockValue, Credit = 0, DebitBalance = stockValue, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "191", AccountName = "İNDİRİLECEK KDV", Debit = dedVat, Credit = 0, DebitBalance = dedVat, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "320", AccountName = "SATICILAR (TEDARİKÇİ CARİ)", Debit = 0, Credit = payables, DebitBalance = 0, CreditBalance = payables },
            new TrialBalanceItemDto { AccountCode = "335", AccountName = "PERSONELE BORÇLAR", Debit = 0, Credit = salaryPayables, DebitBalance = 0, CreditBalance = salaryPayables },
            new TrialBalanceItemDto { AccountCode = "391", AccountName = "HESAPLANAN KDV", Debit = 0, Credit = calcVat, DebitBalance = 0, CreditBalance = calcVat },
            new TrialBalanceItemDto { AccountCode = "600", AccountName = "YURTİÇİ SATIŞLAR", Debit = 0, Credit = salesNet, DebitBalance = 0, CreditBalance = salesNet },
            new TrialBalanceItemDto { AccountCode = "621", AccountName = "SATILAN MALIN MALİYETİ (SMM)", Debit = smmCost, Credit = 0, DebitBalance = smmCost, CreditBalance = 0 },
            new TrialBalanceItemDto { AccountCode = "770", AccountName = "GENEL YÖNETİM GİDERLERİ", Debit = adminExpenses, Credit = 0, DebitBalance = adminExpenses, CreditBalance = 0 }
        };

        var result = new TrialBalanceReportDto
        {
            AccountRows = rows,
            TotalDebit = rows.Sum(r => r.Debit),
            TotalCredit = rows.Sum(r => r.Credit),
            TotalDebitBalance = rows.Sum(r => r.DebitBalance),
            TotalCreditBalance = rows.Sum(r => r.CreditBalance)
        };

        return result;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetReportAsync()
    {
        var cashAccounts = await _unitOfWork.CashAccounts.GetAllAsync();
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var products = await _unitOfWork.Products.GetAllAsync();
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var salaryPayments = await _unitOfWork.SalaryPayments.GetAllAsync();

        var approvedInvoices = invoices.Where(i => i.Status == InvoiceStatus.Approved || i.Status == InvoiceStatus.Paid).ToList();

        var liquid = cashAccounts.Sum(a => a.Balance);
        var receivables = customers.Where(c => c.Balance > 0).Sum(c => c.Balance);
        var inventories = products.Sum(p => p.CurrentStock * p.PurchasePrice);
        var dedVat = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.VatTotal);
        var calcVat = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.VatTotal);
        var vatDiff = calcVat - dedVat;

        var payables = customers.Where(c => c.Balance < 0).Sum(c => Math.Abs(c.Balance));
        var salaryPayables = salaryPayments.Where(s => s.Status == SalaryPaymentStatus.Pending).Sum(s => s.TotalPayment);

        var salesNet = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Sales).Sum(i => i.SubTotal);
        var purchaseNet = approvedInvoices.Where(i => i.InvoiceType == InvoiceType.Purchase).Sum(i => i.SubTotal);
        var expenses = salaryPayments.Where(s => s.Status == SalaryPaymentStatus.Paid).Sum(s => s.GrossSalary);
        var netProfit = salesNet - purchaseNet - expenses;

        var totalAssets = liquid + receivables + inventories + (vatDiff < 0 ? Math.Abs(vatDiff) : 0);
        var totalLiab = payables + (vatDiff > 0 ? vatDiff : 0) + salaryPayables;

        return new BalanceSheetReportDto
        {
            LiquidAssets = liquid,
            TradeReceivables = receivables,
            Inventories = inventories,
            VatCarriedForward = vatDiff < 0 ? Math.Abs(vatDiff) : 0,
            TradePayables = payables,
            VatPayable = vatDiff > 0 ? vatDiff : 0,
            PersonnelPayables = salaryPayables,
            EquityCapital = Math.Max(100000.00m, totalAssets - totalLiab - netProfit),
            NetPeriodProfit = netProfit
        };
    }

    private static string GetUnitText(Unit unit) => unit switch
    {
        Unit.Piece => "Adet",
        Unit.Kg => "Kg",
        Unit.Liter => "Litre",
        Unit.Package => "Paket",
        Unit.Box => "Koli",
        Unit.Meter => "Metre",
        Unit.Pair => "Çift",
        _ => "Adet"
    };
}
