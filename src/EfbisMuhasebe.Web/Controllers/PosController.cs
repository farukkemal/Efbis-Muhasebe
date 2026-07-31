using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfbisMuhasebe.Application.DTOs;
using EfbisMuhasebe.Domain.Entities;
using EfbisMuhasebe.Domain.Enums;
using EfbisMuhasebe.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EfbisMuhasebe.Web.Controllers;

/// <summary>
/// Hızlı Satış (POS Kasa Ekranı) Controller
/// Perakende ve dokunmatik hızlı kasa satışlarını yönetir.
/// </summary>
public class PosController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public PosController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        var cashAccounts = await _unitOfWork.CashAccounts.GetAllActiveAsync();
        var categories = await _unitOfWork.Categories.GetAllAsync();

        ViewBag.Customers = customers.OrderBy(c => c.Title).ToList();
        ViewBag.CashAccounts = cashAccounts.ToList();
        ViewBag.Categories = categories.OrderBy(c => c.Name).ToList();

        return View();
    }

    [HttpGet("api/[controller]/products")]
    public async Task<IActionResult> GetPosProducts([FromQuery] int? categoryId, [FromQuery] string? search)
    {
        try
        {
            var products = await _unitOfWork.Products.GetWithCategoryAsync();
            var salesProducts = products.Where(p => p.Status == ProductStatus.Active && p.IsAvailableForSale);

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                salesProducts = salesProducts.Where(p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.ToLower();
                salesProducts = salesProducts.Where(p => 
                    p.ProductName.ToLower().Contains(term) || 
                    p.ProductCode.ToLower().Contains(term) || 
                    (!string.IsNullOrEmpty(p.Barcode) && p.Barcode.ToLower().Contains(term)));
            }

            var list = salesProducts.Select(p => new
            {
                p.Id,
                p.ProductName,
                p.ProductCode,
                p.Barcode,
                CategoryName = p.Category?.Name ?? "Genel",
                p.SalePrice,
                p.CurrentStock,
                p.Unit,
                VatRate = (int)p.SaleVatRate
            }).ToList();

            return Json(list);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("api/[controller]/z-report")]
    public async Task<IActionResult> GetZReport()
    {
        try
        {
            var today = DateTime.Today;
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var posTodayInvoices = invoices
                .Where(i => i.InvoiceNumber.StartsWith("POS-") && i.InvoiceDate >= today)
                .ToList();

            var cashSales = posTodayInvoices.Where(i => i.Description != null && i.Description.Contains("Nakit")).Sum(i => i.GrandTotal);
            var cardSales = posTodayInvoices.Where(i => i.Description != null && i.Description.Contains("KrediKarti")).Sum(i => i.GrandTotal);
            var mealSales = posTodayInvoices.Where(i => i.Description != null && i.Description.Contains("YemekKarti")).Sum(i => i.GrandTotal);

            var report = new PosZReportDto
            {
                Date = today,
                TotalSalesCount = posTodayInvoices.Count,
                TotalCashSales = cashSales,
                TotalCreditCardSales = cardSales,
                TotalMealCardSales = mealSales,
                GrandTotalSales = posTodayInvoices.Sum(i => i.GrandTotal)
            };

            return Json(report);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("api/[controller]/checkout")]
    public async Task<IActionResult> Checkout([FromBody] PosCheckoutDto dto)
    {
        if (dto == null || dto.Items == null || !dto.Items.Any())
        {
            return BadRequest(new { success = false, message = "Sepette ürün bulunmamaktadır." });
        }

        try {
            await _unitOfWork.BeginTransactionAsync();

            // 1. Müşteri Tespiti (yoksa Perakende Müşteri)
            int customerId = dto.CustomerId ?? 0;
            if (customerId == 0)
            {
                var customers = await _unitOfWork.Customers.GetAllAsync();
                var retailCust = customers.FirstOrDefault(c => c.CustomerCode == "PRK-001" || c.Title.Contains("Perakende"));
                if (retailCust != null)
                {
                    customerId = retailCust.Id;
                }
                else
                {
                    var newCust = new Customer
                    {
                        CustomerCode = "PRK-001",
                        Title = "Perakende Müşteri (POS)",
                        CustomerType = CustomerType.Customer,
                        Status = CustomerStatus.Active
                    };
                    await _unitOfWork.Customers.AddAsync(newCust);
                    await _unitOfWork.SaveChangesAsync();
                    customerId = newCust.Id;
                }
            }

            // 2. Kasa Hesabı Tespiti (Ödeme Tipine Göre Otomatik Kasa Ayrımı)
            int cashAccountId = dto.CashAccountId;
            if (cashAccountId <= 0)
            {
                var activeAccounts = await _unitOfWork.CashAccounts.GetAllActiveAsync();
                
                if (dto.PaymentType != null && (dto.PaymentType.Contains("Kredi") || dto.PaymentType.Contains("POS") || dto.PaymentType.Contains("Kart")))
                {
                    // POS / Kredi Kartı Satışı -> POS Kasası veya Banka Hesabına Aktar
                    var posAcc = activeAccounts.FirstOrDefault(a => a.AccountType == CashAccountType.POS || a.AccountType == CashAccountType.KrediKarti || a.AccountType == CashAccountType.Banka);
                    cashAccountId = posAcc?.Id ?? activeAccounts.FirstOrDefault()?.Id ?? 0;
                }
                else
                {
                    // Nakit Satış -> Fiziksel Nakit Kasasına Aktar
                    var cashAcc = activeAccounts.FirstOrDefault(a => a.AccountType == CashAccountType.Kasa);
                    cashAccountId = cashAcc?.Id ?? activeAccounts.FirstOrDefault()?.Id ?? 0;
                }
            }

            // 3. Fatura Oluşturma (Approved & Paid)
            var year = DateTime.Now.Year;
            var invoiceCount = (await _unitOfWork.Invoices.GetAllAsync()).Count() + 1;
            var invoiceNum = $"POS-{year}-{invoiceCount:D4}";

            decimal subTotal = 0;
            decimal vatTotal = 0;

            var invoiceItems = new List<InvoiceItem>();

            foreach (var item in dto.Items)
            {
                var prod = await _unitOfWork.Products.GetByIdAsync(item.ProductId);
                if (prod != null)
                {
                    // Stok düşümü
                    prod.CurrentStock = Math.Max(0, prod.CurrentStock - item.Quantity);
                    _unitOfWork.Products.Update(prod);

                    // Stok Hareket Kaydı (Stock Movement Linkage)
                    var stTx = new StockTransaction
                    {
                        TransactionCode = $"STK-POS-{invoiceCount:D4}-{prod.Id}",
                        TransactionType = TransactionType.StockOut,
                        ProductId = prod.Id,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalAmount = item.Quantity * item.UnitPrice,
                        CustomerId = customerId,
                        Description = $"POS Satış Stok Çıkışı ({invoiceNum})",
                        TransactionDate = DateTime.Now,
                        ReferenceNo = invoiceNum
                    };
                    await _unitOfWork.StockTransactions.AddAsync(stTx);
                }

                var lineNet = item.UnitPrice * item.Quantity;
                var lineVat = lineNet * (item.VatRate / 100m);
                var lineGrand = lineNet + lineVat;

                subTotal += lineNet;
                vatTotal += lineVat;

                invoiceItems.Add(new InvoiceItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    VatRate = item.VatRate,
                    VatAmount = lineVat,
                    LineTotal = lineGrand
                });
            }

            var grandTotal = subTotal + vatTotal;

            var invoice = new Invoice
            {
                InvoiceNumber = invoiceNum,
                InvoiceType = InvoiceType.Sales,
                CustomerId = customerId,
                InvoiceDate = DateTime.Now,
                SubTotal = subTotal,
                VatTotal = vatTotal,
                GrandTotal = grandTotal,
                Status = InvoiceStatus.Paid,
                Description = $"POS Kasa Hızlı Satış ({dto.PaymentType})"
            };

            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            // Fatura Kalemlerini kaydet
            foreach (var item in invoiceItems)
            {
                item.InvoiceId = invoice.Id;
            }

            // 4. Kasa Tahsilat Hareketi (Bakiye Artışı)
            if (cashAccountId > 0)
            {
                var cashAcc = await _unitOfWork.CashAccounts.GetByIdAsync(cashAccountId);
                if (cashAcc != null)
                {
                    cashAcc.Balance += grandTotal;
                    
                    var tx = new CashTransaction
                    {
                        TransactionCode = $"THS-POS-{invoice.Id}",
                        CashAccountId = cashAccountId,
                        TransactionType = CashTransactionType.Collection,
                        Amount = grandTotal,
                        CustomerId = customerId,
                        InvoiceId = invoice.Id,
                        Description = $"POS Satış Tahsilatı ({invoiceNum} - {dto.PaymentType})",
                        TransactionDate = DateTime.Now
                    };
                    await _unitOfWork.CashAccounts.AddTransactionAsync(tx);
                }
            }

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var custObj = await _unitOfWork.Customers.GetByIdAsync(customerId);

            var receipt = new PosReceiptDto
            {
                InvoiceNumber = invoiceNum,
                Date = invoice.InvoiceDate,
                CustomerTitle = custObj?.Title ?? "Perakende Müşteri",
                PaymentType = dto.PaymentType,
                SubTotal = subTotal,
                VatTotal = vatTotal,
                GrandTotal = grandTotal,
                ReceivedAmount = dto.ReceivedAmount > 0 ? dto.ReceivedAmount : grandTotal,
                ChangeAmount = dto.ChangeAmount,
                Items = dto.Items
            };

            return Json(new { success = true, message = "Satış başarıyla tamamlandı!", receipt = receipt });
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return BadRequest(new { success = false, message = "Satış işlemi gerçekleştirilemedi: " + ex.Message });
        }
    }
}
