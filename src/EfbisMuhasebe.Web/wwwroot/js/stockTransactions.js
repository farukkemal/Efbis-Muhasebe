// ─── Stok Hareketleri JavaScript Module ──────────────────────────────────
// AJAX Listing, Creation, Filter, Pagination, Export & Print

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 10;
let searchTimeout = null;

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
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
        const response = await fetch('/StockTransactions/GetDashboard');
        if (!response.ok) return;
        const data = await response.json();

        const totalIn = data.TotalIn ?? data.totalIn ?? 0;
        const totalOut = data.TotalOut ?? data.totalOut ?? 0;
        const todayTx = data.TodayTransactions ?? data.todayTransactions ?? 0;
        const monthlyTx = data.MonthlyTransactions ?? data.monthlyTransactions ?? 0;

        document.getElementById('statTotalIn').textContent = formatNumber(totalIn);
        document.getElementById('statTotalOut').textContent = formatNumber(totalOut);
        document.getElementById('statTodayTx').textContent = formatNumber(todayTx);
        document.getElementById('statMonthlyTx').textContent = formatNumber(monthlyTx);
    } catch (e) {
        console.error('Dashboard yüklenemedi:', e);
    }
}

// ─── Load Transactions ────────────────────────────────────────────────────────
async function loadData(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const searchTerm = document.getElementById('searchInput')?.value || '';
    const type = document.getElementById('typeFilter')?.value || '';
    const startDate = document.getElementById('startDateFilter')?.value || '';
    const endDate = document.getElementById('endDateFilter')?.value || '';

    const params = new URLSearchParams({
        page: currentPage,
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        transactionType: type,
        startDate: startDate,
        endDate: endDate
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/StockTransactions/GetTransactions?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} kayıt`;
    } catch (e) {
        console.error(e);
        showTableError('Stok hareketleri yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('transactionsTableBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9">
                    <div class="empty-state">
                        <i class="bi bi-arrow-left-right"></i>
                        <h6>Stok hareketi bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili filtrelere uygun stok hareketi kaydı bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const code = p.TransactionCode || p.transactionCode || '';
        const type = p.TransactionType ?? p.transactionType ?? 1;
        const typeText = p.TransactionTypeText || p.transactionTypeText || getTypeText(type);
        const prodName = p.ProductName || p.productName || '';
        const prodCode = p.ProductCode || p.productCode || '';
        const whName = p.WarehouseName || p.warehouseName || '';
        const qty = p.Quantity ?? p.quantity ?? 0;
        const price = p.UnitPrice ?? p.unitPrice ?? 0;
        const total = p.TotalAmount ?? p.totalAmount ?? (qty * price);
        const dateStr = p.FormattedDate || p.formattedDate || formatDate(p.TransactionDate || p.transactionDate);

        const badgeClass = getBadgeClass(type);

        return `
        <tr id="row-stk-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td><span class="badge ${badgeClass}" style="font-size:12px;padding:4px 8px;">${escHtml(typeText)}</span></td>
            <td>
                <strong>${escHtml(prodName)}</strong>
                <small class="text-muted d-block" style="font-size:11px;">${escHtml(prodCode)}</small>
            </td>
            <td>${whName ? escHtml(whName) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="text-align:right;">
                <strong style="font-size:13.5px;color:${type === 1 ? '#059669' : type === 2 ? '#dc2626' : '#2563eb'};">
                    ${type === 1 ? '+' : type === 2 ? '-' : ''}${formatNumber(qty)}
                </strong>
            </td>
            <td style="text-align:right;font-size:13px;color:#64748b;">${formatCurrency(price)}</td>
            <td style="text-align:right;"><strong>${formatCurrency(total)}</strong></td>
            <td style="font-size:13px;color:#64748b;">${dateStr}</td>
            <td style="text-align:center;">
                <button class="btn-action btn-action-detail" onclick="showDetails(${id})" title="Detay Göster" aria-label="Detay">
                    <i class="bi bi-eye"></i>
                </button>
            </td>
        </tr>`;
    }).join('');
}

function getBadgeClass(type) {
    switch (type) {
        case 1: return 'bg-success'; // Stok Girişi
        case 2: return 'bg-danger';  // Stok Çıkışı
        case 3: return 'bg-primary'; // Depo Transferi
        case 4: return 'bg-warning text-dark'; // Sayım Farkı
        case 5: return 'bg-secondary'; // Fire
        default: return 'bg-dark';
    }
}

function getTypeText(type) {
    switch (type) {
        case 1: return 'Stok Girişi';
        case 2: return 'Stok Çıkışı';
        case 3: return 'Depo Transferi';
        case 4: return 'Sayım Farkı';
        case 5: return 'Fire';
        default: return 'Bilinmiyor';
    }
}

// ─── Load Dropdowns ───────────────────────────────────────────────────────────
async function loadDropdowns() {
    try {
        // Products
        const prodRes = await fetch('/Products/GetProducts?pageSize=500');
        if (prodRes.ok) {
            const data = await prodRes.json();
            const items = data.Items || data.items || [];
            const select = document.getElementById('ProductId');
            select.innerHTML = '<option value="">-- Ürün Seçiniz --</option>';
            items.forEach(p => {
                const id = p.Id ?? p.id;
                const name = p.ProductName || p.productName;
                const code = p.ProductCode || p.productCode;
                const stock = p.CurrentStock ?? p.currentStock ?? 0;
                select.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)}) — Stok: ${stock}</option>`;
            });
        }

        // Active Warehouses
        const whRes = await fetch('/Warehouses/GetActive');
        if (whRes.ok) {
            const warehouses = await whRes.json();
            const select = document.getElementById('WarehouseId');
            select.innerHTML = '<option value="">-- Depo Seçiniz --</option>';
            warehouses.forEach(w => {
                const id = w.Id ?? w.id;
                const name = w.Name || w.name;
                const code = w.WarehouseCode || w.warehouseCode;
                select.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)})</option>`;
            });
        }

        // Invoices
        const invRes = await fetch('/Invoices/GetInvoices?pageSize=100');
        if (invRes.ok) {
            const data = await invRes.json();
            const invs = data.Items || data.items || [];
            const select = document.getElementById('InvoiceSelect');
            if (select) {
                select.innerHTML = '<option value="">-- Bağımsız İşlem / Fatura Seçmeyiniz --</option>';
                invs.forEach(inv => {
                    const id = inv.Id ?? inv.id;
                    const num = inv.InvoiceNumber || inv.invoiceNumber;
                    const cust = inv.CustomerTitle || inv.customerTitle;
                    const type = (inv.InvoiceType ?? inv.invoiceType) === 1 ? 'Satış' : 'Alış';
                    select.innerHTML += `<option value="${id}">${escHtml(num)} — ${escHtml(cust)} (${type})</option>`;
                });
            }
        }
    } catch (e) {
        console.error('Dropdown verileri yüklenemedi:', e);
    }
}

// ─── Modal Actions ────────────────────────────────────────────────────────────
function openCreateModal() {
    document.getElementById('transactionForm').reset();
    document.getElementById('TransactionDate').value = new Date().toISOString().slice(0, 16);

    const invSelect = document.getElementById('InvoiceSelect');
    if (invSelect) invSelect.value = '';

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('transactionModal'));
    modal.show();
}

async function onInvoiceSelectChange(select) {
    const invId = select.value;
    if (!invId) return;

    try {
        const inv = await efbisAjax.get(`/Invoices/GetDetail/${invId}`);
        if (!inv) return;

        const num = inv.InvoiceNumber || inv.invoiceNumber || '';
        const type = inv.InvoiceType ?? inv.invoiceType ?? 1;

        // Auto-fill ReferenceNo & TransactionType
        const refInput = document.getElementById('ReferenceNo');
        if (refInput) refInput.value = num;

        const typeSelect = document.getElementById('TransactionType');
        if (typeSelect) {
            typeSelect.value = type === 1 ? '2' : '1'; // Satış faturası = Stok Çıkışı (2), Alış faturası = Stok Girişi (1)
        }

        const items = inv.Items || inv.items || [];
        if (items.length > 0) {
            const first = items[0];
            const prodId = first.ProductId ?? first.productId;
            const qty = first.Quantity ?? first.quantity;
            const price = first.UnitPrice ?? first.unitPrice;
            const prodName = first.ProductName ?? first.productName ?? 'Ürün';

            const prodSelect = document.getElementById('ProductId');
            if (prodSelect) prodSelect.value = prodId;

            const qtyInput = document.getElementById('Quantity');
            if (qtyInput) qtyInput.value = qty;

            const priceInput = document.getElementById('UnitPrice');
            if (priceInput) priceInput.value = price;

            const descInput = document.getElementById('Description');
            if (descInput) descInput.value = `${num} numaralı ${type === 1 ? 'Satış' : 'Alış'} Faturasına istinaden stok işlemi.`;

            showToast(`${num} faturasındaki '${prodName}' (${qty} adet) form alanlarına otomatik aktarıldı!`, 'info');
        }
    } catch (e) {
        console.error('Invoice detail load error:', e);
    }
}

async function saveTransaction() {
    const type = parseInt(document.getElementById('TransactionType').value);
    const productId = parseInt(document.getElementById('ProductId').value);
    const warehouseId = parseInt(document.getElementById('WarehouseId').value) || null;
    const quantity = parseFloat(document.getElementById('Quantity').value) || 0;
    const unitPrice = parseFloat(document.getElementById('UnitPrice').value) || 0;
    const refNo = document.getElementById('ReferenceNo').value.trim();
    const dateVal = document.getElementById('TransactionDate').value;
    const desc = document.getElementById('Description').value.trim();

    if (!productId || quantity <= 0) {
        showToast('Lütfen ürün seçiniz ve 0\'dan büyük miktar giriniz.', 'warning');
        return;
    }

    if (type === 1 && !refNo) {
        showToast('⚠️ KURAL İHLALİ: Sisteme stok girişi yapabilmek için ürünün geldiğine dair fatura seçilmeli veya Fatura No girilmelidir!', 'warning');
        return;
    }

    const dto = {
        TransactionType: type,
        ProductId: productId,
        WarehouseId: warehouseId,
        Quantity: quantity,
        UnitPrice: unitPrice,
        ReferenceNo: refNo || null,
        TransactionDate: dateVal ? new Date(dateVal).toISOString() : new Date().toISOString(),
        Description: desc || null
    };

    try {
        const result = await efbisAjax.post('/StockTransactions/Create', dto);
        if (result.success) {
            showToast(result.message || 'Stok hareketi kaydedildi.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('transactionModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Stok hareketi kaydedilirken hata oluştu.', 'error');
    }
}

async function showDetails(id) {
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('detailsModal'));
    document.getElementById('detailsContent').innerHTML = `<div class="text-center py-4"><div class="spinner-efbis mx-auto"></div></div>`;
    modal.show();

    try {
        const item = await efbisAjax.get(`/StockTransactions/GetDetail/${id}`);

        const code = item.TransactionCode || item.transactionCode || '';
        const type = item.TransactionType ?? item.transactionType ?? 1;
        const typeText = item.TransactionTypeText || item.transactionTypeText || getTypeText(type);
        const prodName = item.ProductName || item.productName || '';
        const prodCode = item.ProductCode || item.productCode || '';
        const whName = item.WarehouseName || item.warehouseName || '';
        const custTitle = item.CustomerTitle || item.customerTitle || '';
        const qty = item.Quantity ?? item.quantity ?? 0;
        const price = item.UnitPrice ?? item.unitPrice ?? 0;
        const total = item.TotalAmount ?? item.totalAmount ?? (qty * price);
        const dateStr = item.FormattedDate || item.formattedDate || formatDate(item.TransactionDate || item.transactionDate);
        const refNo = item.ReferenceNo || item.referenceNo || '';
        const desc = item.Description || item.description || '';

        const badgeClass = getBadgeClass(type);

        document.getElementById('detailsContent').innerHTML = `
            <div class="row g-3">
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-info-circle text-primary"></i> Genel Bilgiler</div>
                            ${detailRow('İşlem Kodu', `<code style="background:#e2e8f0;padding:2px 7px;border-radius:5px;">${escHtml(code)}</code>`)}
                            ${detailRow('İşlem Tipi', `<span class="badge ${badgeClass}">${escHtml(typeText)}</span>`)}
                            ${detailRow('İşlem Tarihi', dateStr)}
                            ${detailRow('Referans / Belge No', refNo ? escHtml(refNo) : '—')}
                            ${custTitle ? detailRow('İlişkili Cari', escHtml(custTitle)) : ''}
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-box-seam text-success"></i> Ürün & Miktar</div>
                            ${detailRow('Ürün Adı', escHtml(prodName))}
                            ${detailRow('Ürün Kodu', escHtml(prodCode))}
                            ${detailRow('Depo', whName ? escHtml(whName) : '—')}
                            ${detailRow('Miktar', `<strong>${formatNumber(qty)}</strong>`)}
                            ${detailRow('Birim Fiyat', formatCurrency(price))}
                            ${detailRow('Toplam Tutar', `<strong style="font-size:14px;color:#059669;">${formatCurrency(total)}</strong>`)}
                        </div>
                    </div>
                </div>
                ${desc ? `
                <div class="col-12">
                    <div class="p-3 bg-light rounded" style="font-size:13px;">
                        <strong>Açıklama / Not:</strong> ${escHtml(desc)}
                    </div>
                </div>` : ''}
            </div>`;
    } catch (e) {
        document.getElementById('detailsContent').innerHTML = `<div class="text-center text-danger py-4">İşlem detayları yüklenemedi.</div>`;
    }
}

function detailRow(label, value) {
    return `<div class="d-flex justify-content-between align-items-center py-2 border-bottom" style="border-color:#e2e8f0 !important;">
                <span style="font-size:12.5px;color:#64748b;font-weight:500;">${label}</span>
                <span style="font-size:13.5px;font-weight:500;">${value}</span>
            </div>`;
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
    document.getElementById('transactionsTableBody').innerHTML = `
        <tr><td colspan="9" class="text-center py-4 text-danger">
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

function formatDate(dateStr) {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
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
