// ─── Raporlar JS Module ────────────────────────────────────────────────────────
// Stok Değer, Cari Bakiye, En Çok Satılan, Kritik Stok raporları

'use strict';

document.addEventListener('DOMContentLoaded', () => {
    loadDashboard();
    loadIncomeStatement();
    loadVatReport();
    loadStockValueReport();
    loadCustomerBalanceReport();
    loadTopSellingReport();
    loadCriticalStockReport();
    loadCustomerDropdownForLedger();
});

const fmt = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

// ─── Resmi Gelir Tablosu (TDHP) ──────────────────────────────────────────────
async function loadIncomeStatement() {
    try {
        const res = await fetch('/api/Reports/income-statement');
        if (!res.ok) return;
        const d = await res.json();

        const grossSales = d.grossSales ?? d.GrossSales ?? 0;
        const salesDiscounts = d.salesDiscounts ?? d.SalesDiscounts ?? 0;
        const netSales = d.netSales ?? d.NetSales ?? 0;
        const cogs = d.costOfGoodsSold ?? d.CostOfGoodsSold ?? 0;
        const grossProfit = d.grossProfit ?? d.GrossProfit ?? 0;
        const opExpenses = d.operatingExpenses ?? d.OperatingExpenses ?? 0;
        const netProfit = d.netOperatingProfit ?? d.NetOperatingProfit ?? 0;

        document.getElementById('isGrossSales').textContent = fmt(grossSales) + ' ₺';
        document.getElementById('isGrossSalesSub').textContent = fmt(grossSales) + ' ₺';
        document.getElementById('isSalesDiscounts').textContent = '-' + fmt(salesDiscounts) + ' ₺';
        document.getElementById('isNetSales').textContent = fmt(netSales) + ' ₺';
        document.getElementById('isCOGS').textContent = '-' + fmt(cogs) + ' ₺';
        document.getElementById('isGrossProfit').textContent = fmt(grossProfit) + ' ₺';
        document.getElementById('isOperatingExpenses').textContent = '-' + fmt(opExpenses) + ' ₺';
        document.getElementById('isNetOperatingProfit').textContent = fmt(netProfit) + ' ₺';

        const marginRatio = netSales > 0 ? ((grossProfit / netSales) * 100).toFixed(1) : 0;
        const expenseRatio = netSales > 0 ? ((opExpenses / netSales) * 100).toFixed(1) : 0;
        document.getElementById('isProfitMarginRatio').textContent = `% ${marginRatio}`;
        document.getElementById('isExpenseRatio').textContent = `% ${expenseRatio}`;
    } catch (e) {
        console.error('Income Statement load error:', e);
    }
}

// ─── KDV Özet & Beyanname Raporu ─────────────────────────────────────────────
async function loadVatReport() {
    try {
        const res = await fetch('/api/Reports/vat');
        if (!res.ok) return;
        const d = await res.json();

        const calc = d.calculatedVat ?? d.CalculatedVat ?? 0;
        const ded = d.deductibleVat ?? d.DeductibleVat ?? 0;
        const payable = d.netVatPayable ?? d.NetVatPayable ?? 0;
        const carried = d.netVatCarriedForward ?? d.NetVatCarriedForward ?? 0;

        document.getElementById('vatCalculated').textContent = fmt(calc) + ' ₺';
        document.getElementById('vatDeductible').textContent = fmt(ded) + ' ₺';
        document.getElementById('vatNetPayable').textContent = fmt(payable) + ' ₺';
        document.getElementById('vatCarriedForward').textContent = fmt(carried) + ' ₺';

        const tbody = document.getElementById('vatRateBreakdownBody');
        const items = d.rateBreakdown || d.RateBreakdown || [];

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-3">KDV matrah kaydı bulunamadı.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => {
            const rate = item.vatRate ?? item.VatRate ?? 20;
            const sBase = item.salesTaxableBase ?? item.SalesTaxableBase ?? 0;
            const sVat = item.salesVatAmount ?? item.SalesVatAmount ?? 0;
            const pBase = item.purchaseTaxableBase ?? item.PurchaseTaxableBase ?? 0;
            const pVat = item.purchaseVatAmount ?? item.PurchaseVatAmount ?? 0;
            const diff = sVat - pVat;

            return `
            <tr>
                <td><span class="badge bg-dark px-2.5 py-1.5 font-monospace">% ${rate} KDV</span></td>
                <td class="text-end font-monospace">${fmt(sBase)} ₺</td>
                <td class="text-end font-monospace text-primary fw-bold">${fmt(sVat)} ₺</td>
                <td class="text-end font-monospace">${fmt(pBase)} ₺</td>
                <td class="text-end font-monospace text-info">${fmt(pVat)} ₺</td>
                <td class="text-end font-monospace fw-bold ${diff >= 0 ? 'text-danger' : 'text-success'}">${fmt(diff)} ₺</td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('VAT Report load error:', e);
    }
}

// ─── Cari Seçim Dropdown & Ekstre ────────────────────────────────────────────
async function loadCustomerDropdownForLedger() {
    try {
        const res = await fetch('/api/Reports/customer-balance');
        if (!res.ok) return;
        const data = await res.json();
        const items = Array.isArray(data) ? data : (data.items || data.Items || []);

        const select = document.getElementById('ledgerCustomerSelect');
        if (!select) return;

        items.forEach(c => {
            const id = c.customerId ?? c.CustomerId;
            const title = c.title ?? c.Title;
            const code = c.customerCode ?? c.CustomerCode;
            const option = document.createElement('option');
            option.value = id;
            option.textContent = `${title} (${code})`;
            select.appendChild(option);
        });
    } catch (e) {
        console.error('Customer dropdown load error:', e);
    }
}

async function loadCustomerLedgerStatement(customerId) {
    const container = document.getElementById('ledgerStatementContainer');
    if (!customerId) {
        container.innerHTML = '<div class="text-center text-muted py-5"><i class="bi bi-journal-arrow-down fs-1 d-block mb-2"></i>Ekstresini görüntülemek istediğiniz müşteriyi yukarıdaki menüden seçiniz.</div>';
        return;
    }

    try {
        const res = await fetch(`/api/Reports/customer-ledger/${customerId}`);
        if (!res.ok) {
            container.innerHTML = '<div class="text-center text-danger py-4">Cari ekstre yüklenemedi.</div>';
            return;
        }
        const d = await res.json();

        const title = d.customerTitle ?? d.CustomerTitle;
        const code = d.customerCode ?? c.CustomerCode ?? '';
        const totalDebit = d.totalDebit ?? d.TotalDebit ?? 0;
        const totalCredit = d.totalCredit ?? d.TotalCredit ?? 0;
        const finalBal = d.finalBalance ?? d.FinalBalance ?? 0;
        const txs = d.transactions || d.Transactions || [];

        container.innerHTML = `
        <div class="p-3 bg-light rounded-3 border mb-3 d-flex justify-content-between align-items-center">
            <div>
                <h6 class="fw-bold text-dark mb-0">${escHtml(title)}</h6>
                <small class="text-muted font-monospace">Cari Kodu: ${escHtml(code)}</small>
            </div>
            <div class="d-flex gap-3">
                <div><span class="text-muted small">Toplam Borç:</span> <strong class="text-danger font-monospace">${fmt(totalDebit)} ₺</strong></div>
                <div><span class="text-muted small">Toplam Alacak:</span> <strong class="text-success font-monospace">${fmt(totalCredit)} ₺</strong></div>
                <div><span class="text-muted small">Net Bakiye:</span> <strong class="text-primary font-monospace fs-5">${fmt(finalBal)} ₺</strong></div>
            </div>
        </div>
        <div class="table-responsive">
            <table class="table table-hover align-middle border">
                <thead class="table-light">
                    <tr>
                        <th>Tarih</th>
                        <th>Evrak No</th>
                        <th>İşlem Tipi</th>
                        <th>Açıklama</th>
                        <th class="text-end">Borç (₺)</th>
                        <th class="text-end">Alacak (₺)</th>
                        <th class="text-end">Yürüyen Bakiye (₺)</th>
                    </tr>
                </thead>
                <tbody>
                    ${txs.length === 0 ? '<tr><td colspan="7" class="text-center text-muted py-4">Cari hesaba ait hareket bulunmamaktadır.</td></tr>' : 
                    txs.map(t => `
                        <tr>
                            <td class="font-monospace">${new Date(t.date || t.Date).toLocaleDateString('tr-TR')}</td>
                            <td class="font-monospace text-muted">${escHtml(t.documentNo || t.DocumentNo || '')}</td>
                            <td><span class="badge bg-light text-dark border">${escHtml(t.transactionType || t.TransactionType || '')}</span></td>
                            <td>${escHtml(t.description || t.Description || '')}</td>
                            <td class="text-end font-monospace text-danger">${(t.debit || t.Debit) > 0 ? fmt(t.debit || t.Debit) + ' ₺' : '—'}</td>
                            <td class="text-end font-monospace text-success">${(t.credit || t.Credit) > 0 ? fmt(t.credit || t.Credit) + ' ₺' : '—'}</td>
                            <td class="text-end font-monospace fw-bold">${fmt(t.runningBalance || t.RunningBalance)} ₺</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        </div>`;
    } catch (e) {
        console.error('Ledger statement load error:', e);
    }
}

function openJournalVoucherModal(type) {
    const el = document.getElementById('journalVoucherModal');
    if (!el) return;
    const modal = bootstrap.Modal.getOrCreateInstance(el);
    modal.show();
}

// ─── KPI Dashboard ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const res = await fetch('/api/Reports/dashboard');
        if (!res.ok) return;
        const d = await res.json();

        const totalProducts = d.totalProducts ?? d.TotalProducts ?? 0;
        const totalStockValue = d.totalStockValue ?? d.TotalStockValue ?? 0;
        const totalReceivables = d.totalReceivables ?? d.TotalReceivables ?? 0;
        const criticalStockCount = d.criticalStockCount ?? d.CriticalStockCount ?? 0;

        document.getElementById('kpiTotalProducts').textContent = totalProducts;
        document.getElementById('kpiStockValue').textContent = fmt(totalStockValue) + ' ₺';
        document.getElementById('kpiReceivables').textContent = fmt(totalReceivables) + ' ₺';
        document.getElementById('kpiCriticalStock').textContent = criticalStockCount;
    } catch (e) {
        console.error('KPI Dashboard load error:', e);
    }
}

// ─── Stok Değer Raporu ──────────────────────────────────────────────────────
async function loadStockValueReport() {
    try {
        const res = await fetch('/api/Reports/stock-value');
        if (!res.ok) return;
        const d = await res.json();

        const totalPurchase = d.totalPurchaseValue ?? d.TotalPurchaseValue ?? 0;
        const totalSale = d.totalSaleValue ?? d.TotalSaleValue ?? 0;
        const potentialProfit = d.potentialProfit ?? d.PotentialProfit ?? 0;

        document.getElementById('svPurchase').textContent = fmt(totalPurchase) + ' ₺';
        document.getElementById('svSale').textContent = fmt(totalSale) + ' ₺';
        document.getElementById('svProfit').textContent = fmt(potentialProfit) + ' ₺';

        const tbody = document.getElementById('stockValueBody');
        const items = d.items || d.Items || [];

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="10" class="text-center text-muted py-4">Stokta ürün bulunmamaktadır.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => {
            const name = item.productName || item.ProductName || '';
            const code = item.productCode || item.ProductCode || '';
            const cat = item.categoryName || item.CategoryName || '—';
            const stock = item.currentStock ?? item.CurrentStock ?? 0;
            const unit = item.unit || item.Unit || 'Adet';
            const buyPrice = item.purchasePrice ?? item.PurchasePrice ?? 0;
            const sellPrice = item.salePrice ?? item.SalePrice ?? 0;
            const buyVal = item.stockPurchaseValue ?? item.StockPurchaseValue ?? 0;
            const sellVal = item.stockSaleValue ?? item.StockSaleValue ?? 0;
            const profit = item.profit ?? item.Profit ?? 0;

            return `
            <tr>
                <td><span class="fw-600">${escHtml(name)}</span></td>
                <td><span style="font-size:12px;color:#94a3b8;">${escHtml(code)}</span></td>
                <td>${escHtml(cat)}</td>
                <td class="stock-number">${fmt(stock)}</td>
                <td>${escHtml(unit)}</td>
                <td class="price-cell">${fmt(buyPrice)} ₺</td>
                <td class="price-cell">${fmt(sellPrice)} ₺</td>
                <td class="price-cell">${fmt(buyVal)} ₺</td>
                <td class="price-cell fw-600">${fmt(sellVal)} ₺</td>
                <td><span class="${profit >= 0 ? 'text-success' : 'text-danger'} fw-600">${fmt(profit)} ₺</span></td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('Stock Value Report error:', e);
    }
}

// ─── Cari Bakiye Raporu ─────────────────────────────────────────────────────
async function loadCustomerBalanceReport() {
    try {
        const res = await fetch('/api/Reports/customer-balance');
        if (!res.ok) return;
        const data = await res.json();

        const tbody = document.getElementById('customerBalanceBody');
        const items = Array.isArray(data) ? data : (data.items || data.Items || []);

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted py-4">Bakiyeli cari hesap bulunmamaktadır.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(c => {
            const code = c.customerCode || c.CustomerCode || '';
            const title = c.title || c.Title || '';
            const type = c.customerType || c.CustomerType || 'Müşteri';
            const balance = c.balance ?? c.Balance ?? 0;
            const status = c.balanceStatus || c.BalanceStatus || (balance > 0 ? 'Borçlu' : 'Alacaklı');
            const phone = c.phone || c.Phone || '—';
            const city = c.city || c.City || '—';

            const badgeClass = type === 'Müşteri' ? 'bg-primary' : (type === 'Tedarikçi' ? 'bg-warning text-dark' : 'bg-info');

            return `
            <tr>
                <td><span style="font-size:12px;color:#94a3b8;">${escHtml(code)}</span></td>
                <td><span class="fw-600">${escHtml(title)}</span></td>
                <td><span class="badge ${badgeClass}" style="font-size:11px;">${escHtml(type)}</span></td>
                <td class="price-cell fw-600 ${balance > 0 ? 'text-success' : 'text-danger'}">${fmt(Math.abs(balance))} ₺</td>
                <td><span class="badge ${status === 'Borçlu' ? 'bg-success' : 'bg-danger'}" style="font-size:11px;">${status === 'Borçlu' ? '📥 Alacağımız' : '📤 Borcumuz'}</span></td>
                <td>${escHtml(phone)}</td>
                <td>${escHtml(city)}</td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('Customer Balance Report error:', e);
    }
}

// ─── En Çok Satılan Ürünler ─────────────────────────────────────────────────
async function loadTopSellingReport() {
    try {
        const res = await fetch('/api/Reports/top-selling?top=10');
        if (!res.ok) return;
        const data = await res.json();

        const tbody = document.getElementById('topSellingBody');
        const items = Array.isArray(data) ? data : (data.items || data.Items || []);

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">Henüz satış faturası veya işlem bulunmamaktadır.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map((item, idx) => {
            const name = item.productName || item.ProductName || '';
            const code = item.productCode || item.ProductCode || '';
            const qty = item.totalQuantity ?? item.TotalQuantity ?? 0;
            const rev = item.totalRevenue ?? item.TotalRevenue ?? 0;
            const count = item.invoiceCount ?? item.InvoiceCount ?? 0;

            const badgeClass = idx === 0 ? 'bg-warning text-dark' : idx === 1 ? 'bg-secondary' : idx === 2 ? 'bg-danger' : 'bg-dark';
            const rankIcon = idx < 3 ? ['🥇', '🥈', '🥉'][idx] : (idx + 1);

            return `
            <tr>
                <td>
                    <span class="badge ${badgeClass}" style="font-size:12px;">
                        ${rankIcon}
                    </span>
                </td>
                <td><span class="fw-600">${escHtml(name)}</span></td>
                <td><span style="font-size:12px;color:#94a3b8;">${escHtml(code)}</span></td>
                <td class="fw-600">${fmt(qty)}</td>
                <td class="price-cell text-success fw-600">${fmt(rev)} ₺</td>
                <td><span class="badge bg-primary" style="font-size:11px;">${count} işlem</span></td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('Top Selling Report error:', e);
    }
}

// ─── Kritik Stok Raporu ─────────────────────────────────────────────────────
async function loadCriticalStockReport() {
    try {
        const res = await fetch('/api/Reports/critical-stock');
        if (!res.ok) return;
        const data = await res.json();

        const tbody = document.getElementById('criticalStockBody');
        const items = Array.isArray(data) ? data : (data.items || data.Items || []);

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-4">Kritik stokta ürün bulunmamaktadır.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => {
            const name = item.productName || item.ProductName || '';
            const code = item.productCode || item.ProductCode || '';
            const cat = item.categoryName || item.CategoryName || '—';
            const stock = item.currentStock ?? item.CurrentStock ?? 0;
            const minStock = item.minimumStock ?? item.MinimumStock ?? 0;
            const deficit = item.deficit ?? item.Deficit ?? 0;
            const unit = item.unit || item.Unit || 'Adet';
            const status = item.stockStatus || item.StockStatus || 'Kritik';

            const badgeClass = status === 'Stok Yok' ? 'bg-danger' : status === 'Kritik' ? 'bg-warning text-dark' : 'bg-info';
            const icon = status === 'Stok Yok' ? '⚫' : status === 'Kritik' ? '🔴' : '🟡';

            return `
            <tr>
                <td><span class="fw-600">${escHtml(name)}</span></td>
                <td><span style="font-size:12px;color:#94a3b8;">${escHtml(code)}</span></td>
                <td>${escHtml(cat)}</td>
                <td class="stock-number fw-600 ${stock === 0 ? 'text-danger' : 'text-warning'}">${fmt(stock)}</td>
                <td class="stock-number">${fmt(minStock)}</td>
                <td class="text-danger fw-600">${fmt(deficit)}</td>
                <td>${escHtml(unit)}</td>
                <td>
                    <span class="badge ${badgeClass}" style="font-size:11px;">
                        ${icon} ${escHtml(status)}
                    </span>
                </td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('Critical Stock Report error:', e);
    }
}

// ─── Vergi Ödeme Takvimi ───────────────────────────────────────────────────
async function loadTaxCalendar() {
    try {
        const res = await fetch('/api/Reports/tax-calendar');
        if (!res.ok) return;
        const items = await res.json();
        const tbody = document.getElementById('taxCalendarBody');
        if (!tbody) return;

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">Vergi takvimi bulunamadı.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => {
            const type = item.taxType || item.TaxType;
            const period = item.period || item.Period;
            const due = new Date(item.dueDate || item.DueDate).toLocaleDateString('tr-TR');
            const amount = item.estimatedAmount ?? item.EstimatedAmount ?? 0;
            const status = item.status || item.Status;
            const days = item.daysRemaining ?? item.DaysRemaining ?? 0;

            const badge = status === 'Gecikmede' ? 'bg-danger' : status === 'Devreden KDV' ? 'bg-info' : 'bg-warning text-dark';

            return `
            <tr>
                <td><strong class="text-dark">${escHtml(type)}</strong></td>
                <td><span class="badge bg-light text-dark border">${escHtml(period)}</span></td>
                <td><span class="font-monospace fw-bold">${due}</span></td>
                <td><span class="badge bg-secondary font-monospace">${days} Gün Kaldı</span></td>
                <td class="font-monospace fw-bold text-end fs-6 text-primary">${fmt(amount)} ₺</td>
                <td><span class="badge ${badge}">${escHtml(status)}</span></td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('Tax Calendar error:', e);
    }
}

// ─── Aylık KDV Ödeme Takibi ───────────────────────────────────────────────
async function loadVatTracking() {
    try {
        const res = await fetch('/api/Reports/vat-tracking?year=2026');
        if (!res.ok) return;
        const items = await res.json();
        const tbody = document.getElementById('vatTrackingBody');
        if (!tbody) return;

        if (!items || items.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="text-center text-muted py-4">KDV takip verisi bulunamadı.</td></tr>';
            return;
        }

        tbody.innerHTML = items.map(item => {
            const mName = item.monthName || item.MonthName;
            const calc = item.calculatedVat ?? item.CalculatedVat ?? 0;
            const ded = item.deductibleVat ?? item.DeductibleVat ?? 0;
            const net = item.netVatAmount ?? item.NetVatAmount ?? 0;
            const isPayable = item.isPayable ?? item.IsPayable ?? false;
            const due = new Date(item.dueDate || item.DueDate).toLocaleDateString('tr-TR');
            const status = item.status || item.Status;

            const diffColor = isPayable ? 'text-danger' : 'text-info';
            const diffText = isPayable ? `${fmt(net)} ₺ (ÖDENECEK)` : `${fmt(net)} ₺ (DEVREDEN KDV)`;
            const badge = isPayable ? (status === 'Gecikmede' ? 'bg-danger' : 'bg-warning text-dark') : 'bg-info';

            return `
            <tr>
                <td><strong class="text-dark">${escHtml(mName)} 2026</strong></td>
                <td class="font-monospace text-end text-muted">${fmt(calc)} ₺</td>
                <td class="font-monospace text-end text-muted">${fmt(ded)} ₺</td>
                <td class="font-monospace text-end fw-bold ${diffColor}">${diffText}</td>
                <td><span class="font-monospace">${due}</span></td>
                <td><span class="badge ${badge}">${escHtml(status)}</span></td>
            </tr>`;
        }).join('');
    } catch (e) {
        console.error('VAT Tracking error:', e);
    }
}

// ─── Export & Print Helpers ─────────────────────────────────────────────────
function getActiveReportTableInfo() {
    const activeTab = document.querySelector('#reportTabs .nav-link.active');
    const id = activeTab ? activeTab.id : 'stockValue-tab';
    if (id === 'customerBalance-tab') return { tableId: 'customerBalanceTable', filename: 'cari_bakiye_raporu.xls', title: 'Cari Bakiye Raporu' };
    if (id === 'topSelling-tab') return { tableId: 'topSellingTable', filename: 'en_cok_satilanlar.xls', title: 'En Çok Satılan Ürünler' };
    if (id === 'criticalStock-tab') return { tableId: 'criticalStockTable', filename: 'kritik_stok_raporu.xls', title: 'Kritik Stok Raporu' };
    if (id === 'taxCalendar-tab') return { tableId: 'taxCalendarTable', filename: 'vergi_ödeme_takvimi.xls', title: 'Vergi Ödeme Takvimi' };
    if (id === 'vatTracking-tab') return { tableId: 'vatTrackingTable', filename: 'aylik_kdv_takip.xls', title: 'Aylık KDV Takibi' };
    if (id === 'trialBalance-tab') return { tableId: 'trialBalanceTable', filename: 'genel_gecici_mizan.xls', title: 'Genel Geçici Mizan' };
    return { tableId: 'stockValueTable', filename: 'stok_deger_raporu.xls', title: 'Stok Değer Raporu' };
}

// ─── TDHP Genel Geçici Mizan ────────────────────────────────────────────────
async function loadTrialBalance() {
    try {
        const res = await fetch('/api/Reports/trial-balance');
        if (!res.ok) return;
        const d = await res.json();

        const tbody = document.getElementById('trialBalanceBody');
        const tfoot = document.getElementById('trialBalanceFooter');
        if (!tbody) return;

        const rows = d.accountRows || d.AccountRows || [];
        const totDebit = d.totalDebit ?? d.TotalDebit ?? 0;
        const totCredit = d.totalCredit ?? d.TotalCredit ?? 0;
        const totDebitBal = d.totalDebitBalance ?? d.TotalDebitBalance ?? 0;
        const totCreditBal = d.totalCreditBalance ?? d.TotalCreditBalance ?? 0;

        tbody.innerHTML = rows.map(r => {
            const code = r.accountCode || r.AccountCode;
            const name = r.accountName || r.AccountName;
            const debit = r.debit ?? r.Debit ?? 0;
            const credit = r.credit ?? r.Credit ?? 0;
            const debitBal = r.debitBalance ?? r.DebitBalance ?? 0;
            const creditBal = r.creditBalance ?? r.CreditBalance ?? 0;

            return `
            <tr>
                <td><code class="fw-bold text-primary font-monospace">${escHtml(code)}</code></td>
                <td><strong class="text-dark">${escHtml(name)}</strong></td>
                <td class="font-monospace text-end">${fmt(debit)} ₺</td>
                <td class="font-monospace text-end">${fmt(credit)} ₺</td>
                <td class="font-monospace text-end fw-bold text-success">${fmt(debitBal)} ₺</td>
                <td class="font-monospace text-end fw-bold text-danger">${fmt(creditBal)} ₺</td>
            </tr>`;
        }).join('');

        if (tfoot) {
            tfoot.innerHTML = `
            <tr>
                <td colspan="2" class="text-uppercase">GENEL TOPLAM</td>
                <td class="font-monospace text-end">${fmt(totDebit)} ₺</td>
                <td class="font-monospace text-end">${fmt(totCredit)} ₺</td>
                <td class="font-monospace text-end text-success fs-6">${fmt(totDebitBal)} ₺</td>
                <td class="font-monospace text-end text-danger fs-6">${fmt(totCreditBal)} ₺</td>
            </tr>`;
        }
    } catch (e) {
        console.error('Trial Balance load error:', e);
    }
}

// ─── Özet Bilanço ────────────────────────────────────────────────────────────
async function loadBalanceSheet() {
    try {
        const res = await fetch('/api/Reports/balance-sheet');
        if (!res.ok) return;
        const d = await res.json();

        const content = document.getElementById('balanceSheetContent');
        if (!content) return;

        const liquid = d.liquidAssets ?? d.LiquidAssets ?? 0;
        const rec = d.tradeReceivables ?? d.TradeReceivables ?? 0;
        const inv = d.inventories ?? d.Inventories ?? 0;
        const vatCarried = d.vatCarriedForward ?? d.VatCarriedForward ?? 0;
        const totCurrentAssets = d.totalCurrentAssets ?? d.TotalCurrentAssets ?? (liquid + rec + inv + vatCarried);

        const payables = d.tradePayables ?? d.TradePayables ?? 0;
        const vatPay = d.vatPayable ?? d.VatPayable ?? 0;
        const persPay = d.personnelPayables ?? d.PersonnelPayables ?? 0;
        const totShortLiab = d.totalShortTermLiabilities ?? d.TotalShortTermLiabilities ?? (payables + vatPay + persPay);

        const equity = d.equityCapital ?? d.EquityCapital ?? 100000;
        const profit = d.netPeriodProfit ?? d.NetPeriodProfit ?? 0;
        const totEquity = d.totalEquity ?? d.TotalEquity ?? (equity + profit);
        const totPassives = d.totalPassives ?? d.TotalPassives ?? (totShortLiab + totEquity);

        content.innerHTML = `
        <!-- AKTİF (VARLIKLAR) -->
        <div class="col-md-6">
            <div class="p-4 rounded-4 bg-white border shadow-sm">
                <div class="d-flex align-items-center justify-content-between pb-3 mb-3 border-bottom border-2">
                    <h5 class="fw-bold text-primary mb-0"><i class="bi bi-box-arrow-in-down-left me-2"></i>I. AKTİF (VARLIKLAR)</h5>
                    <span class="badge bg-primary fs-6 font-monospace">${fmt(totCurrentAssets)} ₺</span>
                </div>
                
                <h6 class="fw-bold text-dark border-bottom pb-2 mb-3">1. DÖNEN VARLIKLAR</h6>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>100 / 102 Kasa ve Bankalar (Likit Varlıklar)</span>
                    <strong class="font-monospace text-success">${fmt(liquid)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>120 Ticari Alacaklar (Müşteri Bakiyeleri)</span>
                    <strong class="font-monospace text-primary">${fmt(rec)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>153 Stoklar (Mal Varlığı Alış Değeri)</span>
                    <strong class="font-monospace text-dark">${fmt(inv)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>191 Devreden KDV Alacağı</span>
                    <strong class="font-monospace text-info">${fmt(vatCarried)} ₺</strong>
                </div>

                <div class="d-flex justify-content-between pt-3 mt-2 fs-5 fw-bold text-dark border-top border-2">
                    <span>AKTİF TOPLAMI:</span>
                    <span class="text-primary font-monospace">${fmt(totCurrentAssets)} ₺</span>
                </div>
            </div>
        </div>

        <!-- PASİF (KAYNAKLAR) -->
        <div class="col-md-6">
            <div class="p-4 rounded-4 bg-white border shadow-sm">
                <div class="d-flex align-items-center justify-content-between pb-3 mb-3 border-bottom border-2">
                    <h5 class="fw-bold text-danger mb-0"><i class="bi bi-box-arrow-up-right me-2"></i>II. PASİF (KAYNAKLAR)</h5>
                    <span class="badge bg-danger fs-6 font-monospace">${fmt(totPassives)} ₺</span>
                </div>
                
                <h6 class="fw-bold text-dark border-bottom pb-2 mb-3">3. KISA VADELİ YABANCI KAYNAKLAR</h6>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>320 Ticari Borçlar (Tedarikçi Borçları)</span>
                    <strong class="font-monospace text-danger">${fmt(payables)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>360 Ödenecek KDV & Vergi Borçları</span>
                    <strong class="font-monospace text-warning">${fmt(vatPay)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>335 Personele Borçlar (Ödenmemiş Maaşlar)</span>
                    <strong class="font-monospace text-secondary">${fmt(persPay)} ₺</strong>
                </div>

                <h6 class="fw-bold text-dark border-bottom pb-2 mb-3 mt-4">5. ÖZ KAYNAKLAR</h6>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>500 Ödenmiş Sermaye / Kuruluş Öz Kaynağı</span>
                    <strong class="font-monospace text-dark">${fmt(equity)} ₺</strong>
                </div>
                <div class="d-flex justify-content-between py-2 border-bottom">
                    <span>590 Net Dönem Kârı / (Zararı)</span>
                    <strong class="font-monospace ${profit >= 0 ? 'text-success' : 'text-danger'}">${fmt(profit)} ₺</strong>
                </div>

                <div class="d-flex justify-content-between pt-3 mt-2 fs-5 fw-bold text-dark border-top border-2">
                    <span>PASİF TOPLAMI:</span>
                    <span class="text-danger font-monospace">${fmt(totPassives)} ₺</span>
                </div>
            </div>
        </div>`;
    } catch (e) {
        console.error('Balance Sheet load error:', e);
    }
}

function exportActiveReportTable() {
    const info = getActiveReportTableInfo();
    exportTableToExcel(info.tableId, info.filename);
}

function printActiveReportTable() {
    const info = getActiveReportTableInfo();
    printTable(info.tableId, info.title);
}

function escHtml(text) {
    if (text == null) return '';
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
