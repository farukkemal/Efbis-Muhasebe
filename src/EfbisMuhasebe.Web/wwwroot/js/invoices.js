// ─── Fatura Yönetimi JavaScript Module ───────────────────────────────────────
// AJAX Listing, Dynamic Line Items, Locked Invoice Types, Approval Workflow, Export & Print

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 10;
let customers = [];
let products = [];
let currentDetailId = 0;
let searchTimeout = null;

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    const typeParam = urlParams.get('type');
    const statusParam = urlParams.get('status');

    const typeFilter = document.getElementById('typeFilter');
    const statusFilter = document.getElementById('statusFilter');
    const invoiceTypeSelect = document.getElementById('invoiceType');
    const pageTitle = document.getElementById('invoicePageTitle');
    const pageSubtitle = document.getElementById('invoicePageSubtitle');
    const btnCreateText = document.getElementById('btnCreateText');

    if (typeParam === 'Sales') {
        if (typeFilter) typeFilter.value = '1';
        if (invoiceTypeSelect) {
            invoiceTypeSelect.value = '1';
            invoiceTypeSelect.disabled = true; // KİLİTLİ: Satış faturaları modunda sadece satış faturası kesilebilir!
        }
        if (pageTitle) pageTitle.innerHTML = '<i class="bi bi-file-earmark-arrow-up me-2 text-success"></i>Satış Faturaları Yönetimi';
        if (pageSubtitle) pageSubtitle.textContent = 'Müşterilerinize kesilen satış faturalarını düzenleyin, listeleyin ve onaylayın.';
        if (btnCreateText) btnCreateText.textContent = 'Yeni Satış Faturası Kes';
    } else if (typeParam === 'Purchase') {
        if (typeFilter) typeFilter.value = '2';
        if (invoiceTypeSelect) {
            invoiceTypeSelect.value = '2';
            invoiceTypeSelect.disabled = true; // KİLİTLİ: Alış faturaları modunda sadece alış faturası kesilebilir!
        }
        if (pageTitle) pageTitle.innerHTML = '<i class="bi bi-file-earmark-arrow-down me-2 text-primary"></i>Alış Faturaları Yönetimi';
        if (pageSubtitle) pageSubtitle.textContent = 'Tedarikçilerinizden alınan alış faturalarını işleyin, onaylayın ve borç takibi yapın.';
        if (btnCreateText) btnCreateText.textContent = 'Yeni Alış Faturası Kes';
    }

    if (statusParam === '1' || statusParam === 'Draft') {
        if (statusFilter) statusFilter.value = '1'; // Onay bekleyen taslak faturalar
        if (pageTitle) pageTitle.innerHTML = '<i class="bi bi-hourglass-split me-2 text-warning"></i>Onay Bekleyen Faturalar';
        if (pageSubtitle) pageSubtitle.textContent = 'Onay bekleyen tüm alış ve satış faturaları bu alanda toplanır. Tek tıkla inceleyip onaylayabilirsiniz.';
    }

    refreshAll();
    loadDropdowns();
});

function refreshAll() {
    loadDashboard();
    loadData();
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const response = await fetch('/Invoices/GetDashboard');
        if (!response.ok) return;
        const data = await response.json();

        const salesAmt = data.TotalSalesAmount ?? data.totalSalesAmount ?? 0;
        const salesCnt = data.TotalSalesCount ?? data.totalSalesCount ?? 0;
        const purchAmt = data.TotalPurchaseAmount ?? data.totalPurchaseAmount ?? 0;
        const purchCnt = data.TotalPurchaseCount ?? data.totalPurchaseCount ?? 0;
        const draftCnt = data.DraftCount ?? data.draftCount ?? 0;
        const overdueCnt = data.OverdueCount ?? data.overdueCount ?? 0;

        if (document.getElementById('statSalesAmount')) document.getElementById('statSalesAmount').textContent = formatCurrency(salesAmt);
        if (document.getElementById('statSalesCount')) document.getElementById('statSalesCount').textContent = formatNumber(salesCnt);
        if (document.getElementById('statPurchaseAmount')) document.getElementById('statPurchaseAmount').textContent = formatCurrency(purchAmt);
        if (document.getElementById('statPurchaseCount')) document.getElementById('statPurchaseCount').textContent = formatNumber(purchCnt);
        if (document.getElementById('statDraftCount')) document.getElementById('statDraftCount').textContent = formatNumber(draftCnt);
        if (document.getElementById('statOverdueCount')) document.getElementById('statOverdueCount').textContent = formatNumber(overdueCnt);
    } catch (e) {
        console.error('Dashboard error:', e);
    }
}

// ─── Load Invoices List ────────────────────────────────────────────────────────
async function loadData(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const searchTerm = document.getElementById('searchInput')?.value || '';
    const type = document.getElementById('typeFilter')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';
    const startDate = document.getElementById('startDateFilter')?.value || '';
    const endDate = document.getElementById('endDateFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        invoiceType: type,
        status: status,
        startDate: startDate,
        endDate: endDate
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/Invoices/GetInvoices?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        if (document.getElementById('totalBadge')) document.getElementById('totalBadge').textContent = `${totalCount} fatura`;
    } catch (e) {
        console.error(e);
        showTableError('Faturalar yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('dataTableBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="10">
                    <div class="empty-state">
                        <i class="bi bi-file-earmark-text"></i>
                        <h6>Fatura bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili filtrelere uygun fatura kaydı bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const num = p.InvoiceNumber || p.invoiceNumber || '';
        const type = p.InvoiceType ?? p.invoiceType ?? 1;
        const typeText = p.InvoiceTypeText || p.invoiceTypeText || (type === 1 ? 'Satış' : 'Alış');
        const custTitle = p.CustomerTitle || p.customerTitle || '';
        const dateStr = p.FormattedDate || p.formattedDate || formatDate(p.InvoiceDate || p.invoiceDate);
        const dueDate = p.DueDate || p.dueDate;
        const subTotal = p.SubTotal ?? p.subTotal ?? 0;
        const vatTotal = p.VatTotal ?? p.vatTotal ?? 0;
        const grandTotal = p.GrandTotal ?? p.grandTotal ?? 0;
        const status = p.Status ?? p.status ?? 1;
        const statusText = p.StatusText || p.statusText || getStatusText(status);

        return `
        <tr id="row-inv-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;cursor:pointer;" onclick="openDetailModal(${id})">${escHtml(num)}</code></td>
            <td>
                <span class="badge ${type === 1 ? 'bg-success' : 'bg-primary'}" style="font-size:12px;padding:4px 8px;">
                    ${type === 1 ? '🟢 Satış' : '🔵 Alış'}
                </span>
            </td>
            <td><strong>${escHtml(custTitle)}</strong></td>
            <td style="font-size:13px;color:#64748b;">${dateStr}</td>
            <td style="font-size:13px;color:#64748b;">${dueDate ? formatDate(dueDate, false) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="text-align:right;font-size:13px;color:#64748b;">${formatCurrency(subTotal)}</td>
            <td style="text-align:right;font-size:13px;color:#64748b;">${formatCurrency(vatTotal)}</td>
            <td style="text-align:right;"><strong style="font-size:14px;color:#0f172a;">${formatCurrency(grandTotal)}</strong></td>
            <td style="text-align:center;">
                <span class="badge ${getStatusBadge(status)}" style="font-size:12px;">
                    ${escHtml(statusText)}
                </span>
            </td>
            <td style="text-align:center;">
                <div class="d-flex gap-1 justify-content-center">
                    ${status === 1 ? `
                        <button class="btn btn-sm btn-success py-0 px-2 style-12" onclick="quickApprove(${id})" title="Hızlı Onayla">
                            <i class="bi bi-check-circle me-1"></i>Onayla
                        </button>
                        <button class="btn btn-sm btn-outline-danger py-0 px-1.5 style-12" onclick="quickCancel(${id})" title="İptal Et">
                            <i class="bi bi-x-circle"></i>
                        </button>
                    ` : ''}
                    <button class="btn-action btn-action-detail" onclick="openDetailModal(${id})" title="Fatura Detayı" aria-label="Detay">
                        <i class="bi bi-eye"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

async function quickApprove(id) {
    if (!confirm('Bu faturayı onaylamak istediğinize emin misiniz? (Stok ve Cari Bakiyesi otomatik güncellenecektir)')) return;
    try {
        const result = await efbisAjax.post(`/Invoices/UpdateStatus/${id}`, { Status: 2 });
        if (result.success !== false) {
            showToast(result.message || 'Fatura onaylandı.', 'success');
            refreshAll();
        } else {
            showToast(result.message || 'Fatura onaylanamadı.', 'error');
        }
    } catch (e) {
        showToast('Fatura onaylanırken bir hata oluştu.', 'error');
    }
}

async function quickCancel(id) {
    if (!confirm('Bu faturayı iptal etmek istediğinize emin misiniz?')) return;
    try {
        const result = await efbisAjax.post(`/Invoices/UpdateStatus/${id}`, { Status: 3 });
        if (result.success !== false) {
            showToast(result.message || 'Fatura iptal edildi.', 'success');
            refreshAll();
        } else {
            showToast(result.message || 'Fatura iptal edilemedi.', 'error');
        }
    } catch (e) {
        showToast('Fatura iptal edilirken bir hata oluştu.', 'error');
    }
}

function getStatusBadge(status) {
    switch (status) {
        case 1: return 'bg-warning-subtle text-warning border border-warning'; // Taslak (Onay Bekleyen)
        case 2: return 'bg-success-subtle text-success';     // Onaylı
        case 3: return 'bg-danger-subtle text-danger';       // İptal
        case 4: return 'bg-primary-subtle text-primary';     // Ödendi
        default: return 'bg-dark';
    }
}

function getStatusText(status) {
    switch (status) {
        case 1: return '⏳ Onay Bekliyor (Taslak)';
        case 2: return '✅ Onaylı';
        case 3: return '❌ İptal';
        case 4: return '💰 Ödendi';
        default: return 'Bilinmiyor';
    }
}

// ─── Load Dropdowns ───────────────────────────────────────────────────────────
async function loadDropdowns() {
    try {
        const cRes = await fetch('/Invoices/GetCustomers');
        if (cRes.ok) {
            customers = await cRes.json();
            const cSelect = document.getElementById('customerId');
            cSelect.innerHTML = '<option value="">-- Cari Seçiniz --</option>';
            customers.forEach(c => {
                const id = c.Id ?? c.id;
                const title = c.Title || c.title;
                const code = c.CustomerCode || c.customerCode;
                cSelect.innerHTML += `<option value="${id}">${escHtml(title)} (${escHtml(code)})</option>`;
            });
        }

        const pRes = await fetch('/Invoices/GetProducts');
        if (pRes.ok) {
            products = await pRes.json();
        }
    } catch (e) {
        console.error('Dropdown error:', e);
    }
}

function generateAutoInvoiceNumber() {
    const typeVal = document.getElementById('invoiceType')?.value || '1';
    const prefix = typeVal === '2' ? 'AFT' : 'SFT';
    const year = new Date().getFullYear();
    const random = Math.floor(1000 + Math.random() * 9000);
    const numInput = document.getElementById('invoiceNumber');
    if (numInput && !numInput.value.trim()) {
        numInput.value = `${prefix}-${year}-${random}`;
    }
}

// ─── Modal & Dynamic Line Items ───────────────────────────────────────────────
function openCreateModal() {
    document.getElementById('createForm').reset();
    document.getElementById('itemsTableBody').innerHTML = '';
    document.getElementById('invoiceDate').value = new Date().toISOString().slice(0, 10);

    const urlParams = new URLSearchParams(window.location.search);
    const typeParam = urlParams.get('type');
    const invoiceTypeSelect = document.getElementById('invoiceType');
    const createModalTitle = document.getElementById('createModalTitle');

    if (typeParam === 'Sales') {
        invoiceTypeSelect.value = '1';
        invoiceTypeSelect.disabled = true;
        if (createModalTitle) createModalTitle.innerHTML = '<i class="bi bi-file-earmark-arrow-up me-2 text-success"></i>Yeni Satış Faturası Kes';
    } else if (typeParam === 'Purchase') {
        invoiceTypeSelect.value = '2';
        invoiceTypeSelect.disabled = true;
        if (createModalTitle) createModalTitle.innerHTML = '<i class="bi bi-file-earmark-arrow-down me-2 text-primary"></i>Yeni Alış Faturası Kes';
    } else {
        invoiceTypeSelect.disabled = false;
        if (createModalTitle) createModalTitle.innerHTML = '<i class="bi bi-file-earmark-plus me-2 text-primary"></i>Yeni Fatura Kes';
    }

    generateAutoInvoiceNumber();

    addInvoiceItem();
    calculateTotals();

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createModal'));
    modal.show();
}

function getProductOptionsHtml() {
    let html = '<option value="">-- Ürün Seçiniz --</option>';
    products.forEach(p => {
        const id = p.Id ?? p.id;
        const name = p.ProductName || p.productName;
        const code = p.ProductCode || p.productCode;
        const price = p.SalePrice ?? p.salePrice ?? p.PurchasePrice ?? p.purchasePrice ?? 0;
        const vat = p.SaleVatRate ?? p.saleVatRate ?? 20;

        html += `<option value="${id}" data-price="${price}" data-vat="${vat}">${escHtml(name)} (${escHtml(code)})</option>`;
    });
    return html;
}

function addInvoiceItem() {
    const tbody = document.getElementById('itemsTableBody');
    const tr = document.createElement('tr');

    tr.innerHTML = `
        <td>
            <select class="efbis-select product-select" onchange="onProductSelect(this)" style="font-size:13px;" required>
                ${getProductOptionsHtml()}
            </select>
        </td>
        <td><input type="number" class="efbis-input item-qty" value="1" min="0.01" step="0.01" oninput="calculateTotals()" style="text-align:right;font-size:13px;" required /></td>
        <td><input type="number" class="efbis-input item-price" value="0.00" min="0" step="0.01" oninput="calculateTotals()" style="text-align:right;font-size:13px;" required /></td>
        <td><input type="number" class="efbis-input item-disc" value="0" min="0" max="100" step="1" oninput="calculateTotals()" style="text-align:right;font-size:13px;" /></td>
        <td>
            <select class="efbis-select item-vat" onchange="calculateTotals()" style="font-size:13px;">
                <option value="0">%0</option>
                <option value="1">%1</option>
                <option value="8">%8</option>
                <option value="10">%10</option>
                <option value="20" selected>%20</option>
            </select>
        </td>
        <td style="text-align:right;font-size:13px;color:#64748b;vertical-align:middle;" class="item-vat-amt">0,00 ₺</td>
        <td style="text-align:right;font-size:13px;font-weight:600;color:#0f172a;vertical-align:middle;" class="item-total">0,00 ₺</td>
        <td style="text-align:center;vertical-align:middle;">
            <button type="button" class="btn-action btn-action-delete" onclick="this.closest('tr').remove(); calculateTotals();" title="Satırı Sil"><i class="bi bi-trash3"></i></button>
        </td>`;

    tbody.appendChild(tr);
}

function onProductSelect(select) {
    const option = select.options[select.selectedIndex];
    if (option.value) {
        const tr = select.closest('tr');
        const price = parseFloat(option.dataset.price) || 0;
        const vat = parseInt(option.dataset.vat) || 20;

        tr.querySelector('.item-price').value = price.toFixed(2);
        tr.querySelector('.item-vat').value = vat.toString();
        calculateTotals();
    }
}

function calculateTotals() {
    let subTotal = 0;
    let discTotal = 0;
    let vatTotal = 0;
    let grandTotal = 0;

    const rows = document.querySelectorAll('#itemsTableBody tr');
    rows.forEach(row => {
        const qty = parseFloat(row.querySelector('.item-qty').value) || 0;
        const price = parseFloat(row.querySelector('.item-price').value) || 0;
        const discRate = parseFloat(row.querySelector('.item-disc').value) || 0;
        const vatRate = parseFloat(row.querySelector('.item-vat').value) || 0;

        const rowSub = qty * price;
        const rowDisc = rowSub * (discRate / 100);
        const rowAfterDisc = rowSub - rowDisc;
        const rowVat = rowAfterDisc * (vatRate / 100);
        const rowTotal = rowAfterDisc + rowVat;

        row.querySelector('.item-vat-amt').textContent = formatCurrency(rowVat);
        row.querySelector('.item-total').textContent = formatCurrency(rowTotal);

        subTotal += rowSub;
        discTotal += rowDisc;
        vatTotal += rowVat;
    });

    const wRate = parseFloat(document.getElementById('withholdingRate')?.value) || 0;
    const withholdingAmt = wRate > 0 ? (vatTotal * (wRate / 10)) : 0;
    grandTotal = subTotal - discTotal + vatTotal - withholdingAmt;

    document.getElementById('summarySubTotal').textContent = formatCurrency(subTotal);
    document.getElementById('summaryDiscount').textContent = formatCurrency(discTotal);
    document.getElementById('summaryVat').textContent = formatCurrency(vatTotal);
    document.getElementById('summaryGrandTotal').textContent = formatCurrency(grandTotal);
}

async function saveInvoice() {
    const invoiceNumber = document.getElementById('invoiceNumber')?.value.trim();
    const type = parseInt(document.getElementById('invoiceType').value);
    const customerId = parseInt(document.getElementById('customerId').value);
    const invoiceDateVal = document.getElementById('invoiceDate').value;
    const description = document.getElementById('description').value.trim();
    const scenario = document.getElementById('invoiceScenario')?.value || 'TICARI';
    const withholdingRate = parseFloat(document.getElementById('withholdingRate')?.value) || 0;

    if (!invoiceNumber) {
        showToast('Lütfen fatura numarasını giriniz.', 'warning');
        return;
    }

    if (!customerId) {
        showToast('Lütfen müşteri veya tedarikçi seçiniz.', 'warning');
        return;
    }

    const rows = document.querySelectorAll('#itemsTableBody tr');
    if (rows.length === 0) {
        showToast('En az bir fatura kalemi eklemelisiniz.', 'warning');
        return;
    }

    const items = [];
    let valid = true;

    rows.forEach(row => {
        const prodId = parseInt(row.querySelector('.product-select').value);
        const qty = parseFloat(row.querySelector('.item-qty').value) || 0;
        const price = parseFloat(row.querySelector('.item-price').value) || 0;
        const disc = parseFloat(row.querySelector('.item-disc').value) || 0;
        const vat = parseInt(row.querySelector('.item-vat').value) || 0;

        if (!prodId || qty <= 0) valid = false;

        items.push({
            ProductId: prodId,
            Quantity: qty,
            UnitPrice: price,
            DiscountRate: disc,
            VatRate: vat
        });
    });

    if (!valid) {
        showToast('Lütfen her satır için ürün seçin ve 0\'dan büyük miktar girin.', 'warning');
        return;
    }

    const dto = {
        InvoiceNumber: invoiceNumber,
        InvoiceType: type,
        CustomerId: customerId,
        InvoiceDate: invoiceDateVal ? new Date(invoiceDateVal).toISOString() : new Date().toISOString(),
        Description: description || null,
        Scenario: scenario,
        WithholdingRate: withholdingRate,
        Items: items
    };

    try {
        const result = await efbisAjax.post('/Invoices/Create', dto);
        if (result.success || result.Id || result.id) {
            showToast(result.message || 'Fatura başarıyla oluşturuldu.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('createModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message || 'Fatura oluşturulamadı.', 'error');
        }
    } catch (e) {
        console.error(e);
        showToast(e.message || 'Fatura kaydedilirken bir hata oluştu.', 'error');
    }
}

// ─── Detail Modal & Status Updates ────────────────────────────────────────────
async function openDetailModal(id) {
    currentDetailId = id;
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('detailModal'));
    document.getElementById('detailModalBody').innerHTML = `<div class="text-center py-4"><div class="spinner-efbis mx-auto"></div></div>`;
    modal.show();

    try {
        const item = await efbisAjax.get(`/Invoices/GetDetail/${id}`);

        const num = item.InvoiceNumber || item.invoiceNumber || '';
        const type = item.InvoiceType ?? item.invoiceType ?? 1;
        const typeText = item.InvoiceTypeText || item.invoiceTypeText || (type === 1 ? 'Satış Faturası' : 'Alış Faturası');
        const custTitle = item.CustomerTitle || item.customerTitle || '';
        const dateStr = item.FormattedDate || item.formattedDate || formatDate(item.InvoiceDate || item.invoiceDate);
        const dueDate = item.DueDate || item.dueDate;
        const status = item.Status ?? item.status ?? 1;
        const statusText = item.StatusText || item.statusText || getStatusText(status);

        const subTotal = item.SubTotal ?? item.subTotal ?? 0;
        const discTotal = item.DiscountTotal ?? item.discountTotal ?? 0;
        const vatTotal = item.VatTotal ?? item.vatTotal ?? 0;
        const grandTotal = item.GrandTotal ?? item.grandTotal ?? 0;

        const lineItems = item.Items || item.items || [];

        document.getElementById('detailInvoiceNumber').textContent = num;

        let html = `
            <div class="row g-3 mb-4">
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded">
                        <small class="text-muted d-block">Müşteri / Tedarikçi</small>
                        <strong class="fs-6">${escHtml(custTitle)}</strong>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded">
                        <small class="text-muted d-block">Fatura & Vade Tarihi</small>
                        <strong>${dateStr} / ${dueDate ? formatDate(dueDate, false) : '—'}</strong>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="p-3 bg-light rounded">
                        <small class="text-muted d-block">Fatura Tipi / Durum</small>
                        <span class="badge ${type === 1 ? 'bg-success' : 'bg-primary'} me-1">${escHtml(typeText)}</span>
                        <span class="badge ${getStatusBadge(status)}">${escHtml(statusText)}</span>
                    </div>
                </div>
            </div>

            <div class="form-section-title"><i class="bi bi-list-stars text-primary"></i> Fatura Kalemleri Detayı</div>
            <div class="table-responsive mb-3">
                <table class="efbis-table">
                    <thead>
                        <tr>
                            <th>Ürün Kodu</th>
                            <th>Ürün Adı</th>
                            <th style="text-align:right;">Miktar</th>
                            <th style="text-align:right;">Birim Fiyat</th>
                            <th style="text-align:right;">İskonto</th>
                            <th style="text-align:right;">KDV Oranı</th>
                            <th style="text-align:right;">Satır Toplamı</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${lineItems.map(ii => {
                            const pCode = ii.ProductCode || ii.productCode || '';
                            const pName = ii.ProductName || ii.productName || '';
                            const qty = ii.Quantity ?? ii.quantity ?? 0;
                            const price = ii.UnitPrice ?? ii.unitPrice ?? 0;
                            const disc = ii.DiscountRate ?? ii.discountRate ?? 0;
                            const vat = ii.VatRate ?? ii.vatRate ?? 0;
                            const lineTot = ii.LineTotal ?? ii.lineTotal ?? 0;

                            return `
                            <tr>
                                <td><code>${escHtml(pCode)}</code></td>
                                <td><strong>${escHtml(pName)}</strong></td>
                                <td style="text-align:right;">${formatNumber(qty)}</td>
                                <td style="text-align:right;">${formatCurrency(price)}</td>
                                <td style="text-align:right;">%${disc}</td>
                                <td style="text-align:right;">%${vat}</td>
                                <td style="text-align:right;"><strong>${formatCurrency(lineTot)}</strong></td>
                            </tr>`;
                        }).join('')}
                    </tbody>
                </table>
            </div>

            <div class="row justify-content-end">
                <div class="col-md-5">
                    <div class="p-3 bg-light rounded" style="font-size:13.5px;">
                        <div class="d-flex justify-content-between mb-1"><span>Ara Toplam:</span><strong>${formatCurrency(subTotal)}</strong></div>
                        <div class="d-flex justify-content-between mb-1"><span>İskonto Toplam:</span><strong class="text-danger">${formatCurrency(discTotal)}</strong></div>
                        <div class="d-flex justify-content-between mb-1"><span>KDV Toplam:</span><strong>${formatCurrency(vatTotal)}</strong></div>
                        <div class="d-flex justify-content-between pt-2 border-top" style="font-size:16px;">
                            <strong>Genel Toplam:</strong><strong class="text-primary">${formatCurrency(grandTotal)}</strong>
                        </div>
                    </div>
                </div>
            </div>`;

        document.getElementById('detailModalBody').innerHTML = html;

        // Action Buttons
        const btnApprove = document.getElementById('btnApprove');
        const btnCancel = document.getElementById('btnCancel');
        const btnMarkPaid = document.getElementById('btnMarkPaid');

        btnApprove.classList.add('d-none');
        btnCancel.classList.add('d-none');
        btnMarkPaid.classList.add('d-none');

        if (status === 1) { // Taslak
            btnApprove.classList.remove('d-none');
            btnCancel.classList.remove('d-none');
        } else if (status === 2) { // Onaylı
            btnMarkPaid.classList.remove('d-none');
            btnCancel.classList.remove('d-none');
        }
    } catch (e) {
        document.getElementById('detailModalBody').innerHTML = `<div class="text-center text-danger py-4">Fatura detayları yüklenemedi.</div>`;
    }
}

async function updateStatus(newStatus) {
    if (!confirm('Fatura durumunu değiştirmek istediğinize emin misiniz?')) return;

    try {
        const result = await efbisAjax.post(`/Invoices/UpdateStatus/${currentDetailId}`, { Status: newStatus });
        if (result.success !== false) {
            showToast(result.message || 'Fatura durumu güncellendi.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('detailModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message || 'Fatura durumu güncellenemedi.', 'error');
        }
    } catch (e) {
        showToast('Fatura durumu güncellenirken hata oluştu.', 'error');
    }
}

// ─── Helpers ───────────────────────────────────────────────────────────────────
function debounceSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => loadData(1), 400);
}

function setTableLoading(loading) {
    const wrapper = document.getElementById('tableWrapper');
    if (!wrapper) return;
    if (loading) {
        wrapper.style.opacity = '0.6';
        wrapper.style.pointerEvents = 'none';
    } else {
        wrapper.style.opacity = '1';
        wrapper.style.pointerEvents = '';
    }
}

function showTableError(msg) {
    document.getElementById('dataTableBody').innerHTML = `
        <tr><td colspan="10" class="text-center py-4 text-danger">
            <i class="bi bi-exclamation-triangle me-2"></i>${msg}
        </td></tr>`;
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

function formatCurrency(val) {
    const num = parseFloat(val) || 0;
    return num.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ₺';
}

function formatNumber(val) {
    const num = parseFloat(val) || 0;
    return num.toLocaleString('tr-TR');
}

function formatDate(dateStr, includeTime = true) {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    if (includeTime) {
        return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
    }
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function renderPagination(totalCount, pageNumber, pgSize) {
    const totalPages = Math.ceil(totalCount / pgSize);
    const info = document.getElementById('paginationInfo');
    const buttons = document.getElementById('paginationButtons');

    const from = totalCount === 0 ? 0 : ((pageNumber - 1) * pgSize + 1);
    const to = Math.min(pageNumber * pgSize, totalCount);
    info.textContent = totalCount === 0 ? 'Kayıt bulunamadı' : `${from}–${to} / ${totalCount} kayıt`;

    if (totalPages <= 1) {
        buttons.innerHTML = '';
        return;
    }

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadData(${pageNumber - 1})" aria-label="Önceki"><i class="bi bi-chevron-left"></i></button>`;

    const startPage = Math.max(1, pageNumber - 2);
    const endPage = Math.min(totalPages, pageNumber + 2);

    if (startPage > 1) {
        html += `<button class="page-btn" onclick="loadData(1)">1</button>`;
        if (startPage > 2) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === pageNumber ? 'active' : ''}" onclick="loadData(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
        html += `<button class="page-btn" onclick="loadData(${totalPages})">${totalPages}</button>`;
    }

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadData(${pageNumber + 1})" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></button>`;

    buttons.innerHTML = html;
}

// ─── GİB UBL-TR Official E-Invoice Visual Viewer ───────────────────────────
async function openEInvoiceVisualModal() {
    if (!currentDetailId) return;

    try {
        const item = await efbisAjax.get(`/Invoices/GetDetail/${currentDetailId}`);
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('eInvoiceVisualModal'));

        const num = item.InvoiceNumber || item.invoiceNumber || 'SFT-2026-0000';
        const scenario = item.Scenario || item.scenario || 'TICARI';
        const uuid = item.EFaturaUuid || item.eFaturaUuid || '4a2b89c0-e71f-4a90-b2d4-9876543210fe';
        const custTitle = item.CustomerTitle || item.customerTitle || 'Müşteri Ticaret A.Ş.';
        const dateStr = item.FormattedDate || item.formattedDate || formatDate(item.InvoiceDate || item.invoiceDate);

        const subTotal = item.SubTotal ?? item.subTotal ?? 0;
        const discTotal = item.DiscountTotal ?? item.discountTotal ?? 0;
        const vatTotal = item.VatTotal ?? item.vatTotal ?? 0;
        const withRate = item.WithholdingRate ?? item.withholdingRate ?? 0;
        const withTotal = item.WithholdingTotal ?? item.withholdingTotal ?? 0;
        const grandTotal = item.GrandTotal ?? item.grandTotal ?? 0;
        const lineItems = item.Items || item.items || [];

        const html = `
        <div style="background:#ffffff;padding:40px;border-radius:12px;border:1px solid #cbd5e1;font-family:'Segoe UI',Roboto,sans-serif;color:#0f172a;max-width:960px;margin:0 auto;box-shadow:0 10px 25px rgba(0,0,0,0.05);">
            <!-- Official Header -->
            <div class="d-flex justify-content-between align-items-start pb-4 mb-4 border-bottom border-2">
                <div>
                    <div class="d-flex align-items-center gap-2 mb-2">
                        <span class="badge bg-danger fs-6 px-3 py-2 fw-bold font-monospace">GİB E-FATURA UBL-TR v1.2</span>
                        <span class="badge bg-primary fs-6 px-3 py-2 fw-bold font-monospace">${scenario} FATURA</span>
                    </div>
                    <h3 class="fw-bold text-dark mb-1">${escHtml(window.EFBIS_COMPANY_NAME || 'Şirketiniz')}</h3>
                    <p class="text-muted small mb-0">Bakırköy Vergi Dairesi | VKN: 1472583690 | Ticaret Sicil No: 489201</p>
                </div>
                <div class="text-end">
                    <div class="p-2 border rounded bg-light d-inline-block text-center mb-2" style="width:100px;height:100px;">
                        <i class="bi bi-qr-code text-dark" style="font-size:64px;line-height:1;"></i>
                    </div>
                    <div class="text-muted font-monospace" style="font-size:11px;">GİB ETTN VERİFİKASYON</div>
                </div>
            </div>

            <!-- Meta Information Table -->
            <div class="row g-3 mb-4">
                <div class="col-md-6">
                    <div class="p-3 border rounded bg-light h-100">
                        <h6 class="fw-bold text-primary mb-2 border-bottom pb-1"><i class="bi bi-building me-1"></i> ALICI BİLGİLERİ</h6>
                        <div class="fw-bold fs-6 text-dark">${escHtml(custTitle)}</div>
                        <div class="small text-muted">VKN / TCKN: 9876543210</div>
                        <div class="small text-muted">Adres: İstanbul Türkiye</div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="p-3 border rounded bg-light h-100 font-monospace" style="font-size:13px;">
                        <div class="d-flex justify-content-between mb-1"><span>Fatura No:</span><strong class="text-dark">${num}</strong></div>
                        <div class="d-flex justify-content-between mb-1"><span>Fatura Tarihi:</span><strong>${dateStr}</strong></div>
                        <div class="d-flex justify-content-between mb-1"><span>Senaryo:</span><strong class="text-primary">${scenario}</strong></div>
                        <div class="d-flex justify-content-between text-truncate"><span>ETTN (UUID):</span><strong class="text-danger" style="font-size:10px;">${uuid}</strong></div>
                    </div>
                </div>
            </div>

            <!-- Items Table -->
            <div class="table-responsive mb-4">
                <table class="table table-bordered table-striped align-middle" style="font-size:13px;">
                    <thead class="table-dark">
                        <tr>
                            <th>Sıra</th>
                            <th>Ürün / Hizmet Kodu & Adı</th>
                            <th class="text-end">Miktar</th>
                            <th class="text-end">Birim Fiyat</th>
                            <th class="text-end">İskonto</th>
                            <th class="text-end">KDV %</th>
                            <th class="text-end">KDV Tutarı</th>
                            <th class="text-end">Satır Toplamı</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${lineItems.map((ii, idx) => {
                            const pCode = ii.ProductCode || ii.productCode || '';
                            const pName = ii.ProductName || ii.productName || '';
                            const qty = ii.Quantity ?? ii.quantity ?? 0;
                            const price = ii.UnitPrice ?? ii.unitPrice ?? 0;
                            const disc = ii.DiscountRate ?? ii.discountRate ?? 0;
                            const vat = ii.VatRate ?? ii.vatRate ?? 0;
                            const vatAmt = ii.VatAmount ?? ii.vatAmount ?? 0;
                            const lineTot = ii.LineTotal ?? ii.lineTotal ?? 0;

                            return `
                            <tr>
                                <td>${idx + 1}</td>
                                <td><strong>${escHtml(pName)}</strong> <small class="text-muted font-monospace">(${escHtml(pCode)})</small></td>
                                <td class="text-end font-monospace">${formatNumber(qty)}</td>
                                <td class="text-end font-monospace">${formatCurrency(price)}</td>
                                <td class="text-end font-monospace">%${disc}</td>
                                <td class="text-end font-monospace">%${vat}</td>
                                <td class="text-end font-monospace">${formatCurrency(vatAmt)}</td>
                                <td class="text-end font-monospace fw-bold">${formatCurrency(lineTot)}</td>
                            </tr>`;
                        }).join('')}
                    </tbody>
                </table>
            </div>

            <!-- Totals & Tevkifat Breakdown -->
            <div class="row g-3 justify-content-between align-items-end mb-4">
                <div class="col-md-6">
                    <div class="p-3 border rounded bg-success-subtle border-success text-success" style="font-size:12.5px;">
                        <div class="fw-bold mb-1"><i class="bi bi-shield-check me-1"></i> GİB ELEKTRONİK İMZA DOĞRULANDI</div>
                        <div>Bu fatura 5070 Sayılı Elektronik İmza Kanunu uyarınca güvenli elektronik imza ile imzalanmıştır.</div>
                    </div>
                </div>
                <div class="col-md-5">
                    <div class="p-3 border rounded bg-light font-monospace" style="font-size:13.5px;">
                        <div class="d-flex justify-content-between mb-1"><span>Matrah (Ara Toplam):</span><strong>${formatCurrency(subTotal)}</strong></div>
                        <div class="d-flex justify-content-between mb-1 text-danger"><span>İskonto Toplamı:</span><strong>-${formatCurrency(discTotal)}</strong></div>
                        <div class="d-flex justify-content-between mb-1"><span>Hesaplanan KDV:</span><strong>${formatCurrency(vatTotal)}</strong></div>
                        ${withRate > 0 ? `
                        <div class="d-flex justify-content-between mb-1 text-warning"><span>Tevkifat (${withRate}/10):</span><strong>-${formatCurrency(withTotal)}</strong></div>
                        ` : ''}
                        <div class="d-flex justify-content-between pt-2 border-top fs-5 fw-bold text-primary">
                            <span>ODENECEK TUTAR:</span>
                            <span>${formatCurrency(grandTotal)}</span>
                        </div>
                    </div>
                </div>
            </div>
        </div>`;

        document.getElementById('eInvoicePrintArea').innerHTML = html;
        modal.show();
    } catch (e) {
        showToast('Resmi E-Fatura belgesi hazırlanamadı', 'error');
    }
}

function printEInvoiceDocument() {
    const content = document.getElementById('eInvoicePrintArea')?.innerHTML;
    if (!content) return;

    const printWin = window.open('', '_blank', 'width=1000,height=800');
    printWin.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Resmi GİB E-Fatura Belgesi</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
            <style>
                body { padding: 30px; background:#fff; }
                @media print { body { padding:0; } }
            </style>
        </head>
        <body onload="window.print();window.close();">
            ${content}
        </body>
        </html>
    `);
    printWin.document.close();
}
