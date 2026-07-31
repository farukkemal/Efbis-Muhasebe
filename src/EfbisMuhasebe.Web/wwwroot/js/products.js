// ─── Products Module JavaScript ────────────────────────────────────────────────
// Ürün listesi, AJAX CRUD, filtre, sayfalama, sıralama

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 10;
let sortColumn = 'ProductName';
let sortAscending = true;
let searchTimeout = null;
let editingProductId = null;
let deletingProductId = null;
let isLoading = false;

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    loadProducts();
});

// ─── Load Products ─────────────────────────────────────────────────────────────
async function loadProducts(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const searchTerm = document.getElementById('searchInput')?.value || '';
    const categoryId = document.getElementById('categoryFilter')?.value || '';
    const productType = document.getElementById('typeFilter')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        categoryId: categoryId,
        productType: productType,
        status: status,
        sortBy: sortColumn,
        ascending: sortAscending
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/Products/GetProducts?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        updateStats(items);

        document.getElementById('totalBadge').textContent = `${totalCount} ürün`;
    } catch (err) {
        console.error(err);
        showTableError('Ürünler yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

// ─── Render Table ──────────────────────────────────────────────────────────────
function renderTable(items) {
    const tbody = document.getElementById('productsBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="12">
                    <div class="empty-state">
                        <i class="bi bi-box-seam"></i>
                        <h6>Ürün bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Arama kriterlerinizi değiştirmeyi veya yeni ürün eklemeyi deneyin.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const name = p.ProductName || p.productName || '';
        const code = p.ProductCode || p.productCode || '';
        const barcode = p.Barcode || p.barcode || '';
        const categoryName = p.CategoryName || p.categoryName || '';
        const typeDisplay = p.ProductTypeDisplay || p.productTypeDisplay || '';
        const unitDisplay = p.UnitDisplay || p.unitDisplay || '';
        const purchasePrice = p.PurchasePrice ?? p.purchasePrice ?? 0;
        const salePrice = p.SalePrice ?? p.salePrice ?? 0;
        const currentStock = p.CurrentStock ?? p.currentStock ?? 0;
        const minimumStock = p.MinimumStock ?? p.minimumStock ?? 0;
        const stockStatus = p.StockStatus ?? p.stockStatus ?? 1;
        const status = p.Status ?? p.status ?? 1;

        return `
        <tr id="row-${id}">
            <td>
                <div class="product-name-cell">
                    <span class="product-name">${escHtml(name)}</span>
                </div>
                    ${barcode ? `<small style="font-size:11px;color:#94a3b8;"><i class="bi bi-upc"></i> ${escHtml(barcode)}</small>` : ''}
                </div>
            </td>
            <td>
                <span class="badge-category">${escHtml(categoryName)}</span>
            </td>
            <td class="stock-cell">
                <span class="stock-amount">${formatNumber(currentStock)}</span>
                <span class="stock-unit">${escHtml(unit)}</span>
            </td>
            <td>${renderStockBadge(stockStatus)}</td>
            <td class="price-cell">${formatCurrency(purchasePrice)}</td>
            <td class="price-cell font-weight-bold text-success">${formatCurrency(salePrice)}</td>
            <td>${renderStatusBadge(status)}</td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-detail" title="Barkod Etiketi" onclick="openProductBarcodeModal(${id}, '${escHtml(code)}', '${escHtml(name)}', ${salePrice}, '${escHtml(barcode)}')">
                        <i class="bi bi-upc-scan text-warning"></i>
                    </button>
                    <button class="btn-action btn-action-detail" title="Detay" onclick="showDetail(${id})" aria-label="Detay">
                        <i class="bi bi-eye"></i>
                    </button>
                    <button class="btn-action btn-action-edit" title="Düzenle" onclick="editProduct(${id})" aria-label="Düzenle">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action btn-action-status" title="${status === 1 ? 'Pasife Al' : 'Aktife Al'}" onclick="toggleStatus(${id})" aria-label="Durum Değiştir">
                        <i class="bi bi-arrow-left-right"></i>
                    </button>
                    <button class="btn-action btn-action-delete" title="Sil" onclick="deleteProduct(${id}, '${escHtml(name)}')" aria-label="Sil">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

function renderStockBadge(status) {
    switch (status) {
        case 1: return '<span class="badge-stock-sufficient">🟢 Yeterli</span>';
        case 2: return '<span class="badge-stock-low">🟡 Az Stok</span>';
        case 3: return '<span class="badge-stock-critical">🔴 Kritik Stok</span>';
        default: return '<span class="badge-stock-sufficient">🟢 Yeterli</span>';
    }
}

function renderStatusBadge(status) {
    return status === 1
        ? '<span class="badge-status-active">Aktif</span>'
        : '<span class="badge-status-passive">Pasif</span>';
}

// ─── Stats ─────────────────────────────────────────────────────────────────────
function updateStats(items) {
    const active = items.filter(p => (p.Status ?? p.status) === 1).length;
    const low = items.filter(p => (p.StockStatus ?? p.stockStatus) === 2).length;
    const critical = items.filter(p => (p.StockStatus ?? p.stockStatus) === 3).length;

    animateNumber('statTotal', items.length);
    animateNumber('statActive', active);
    animateNumber('statLow', low);
    animateNumber('statCritical', critical);
}

function animateNumber(id, target) {
    const el = document.getElementById(id);
    if (!el) return;
    el.textContent = target;
}

// ─── Pagination ────────────────────────────────────────────────────────────────
function renderPagination(totalCount, pageNumber, pgSize) {
    const totalPages = Math.ceil(totalCount / pgSize);
    const info = document.getElementById('paginationInfo');
    const buttons = document.getElementById('paginationButtons');

    const from = totalCount === 0 ? 0 : ((pageNumber - 1) * pgSize + 1);
    const to = Math.min(pageNumber * pgSize, totalCount);
    info.textContent = totalCount === 0 ? 'Sonuç bulunamadı' : `${from}–${to} / ${totalCount} kayıt`;

    if (totalPages <= 1) {
        buttons.innerHTML = '';
        return;
    }

    let html = '';

    // Prev
    html += `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadProducts(${pageNumber - 1})" aria-label="Önceki">
                <i class="bi bi-chevron-left"></i>
             </button>`;

    // Page numbers
    const startPage = Math.max(1, pageNumber - 2);
    const endPage = Math.min(totalPages, pageNumber + 2);

    if (startPage > 1) {
        html += `<button class="page-btn" onclick="loadProducts(1)">1</button>`;
        if (startPage > 2) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === pageNumber ? 'active' : ''}" onclick="loadProducts(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
        html += `<button class="page-btn" onclick="loadProducts(${totalPages})">${totalPages}</button>`;
    }

    // Next
    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadProducts(${pageNumber + 1})" aria-label="Sonraki">
                <i class="bi bi-chevron-right"></i>
             </button>`;

    buttons.innerHTML = html;
}

// ─── Sorting ───────────────────────────────────────────────────────────────────
function sortBy(column) {
    if (sortColumn === column) {
        sortAscending = !sortAscending;
    } else {
        sortColumn = column;
        sortAscending = true;
    }

    // Update header icons
    document.querySelectorAll('.efbis-table thead th').forEach(th => {
        th.classList.remove('sort-asc', 'sort-desc');
        const icon = th.querySelector('.sort-icon');
        if (icon) icon.className = 'bi bi-arrow-down-up sort-icon';
    });

    const activeHeader = document.getElementById(`th-${column}`);
    if (activeHeader) {
        activeHeader.classList.add(sortAscending ? 'sort-asc' : 'sort-desc');
        const icon = activeHeader.querySelector('.sort-icon');
        if (icon) icon.className = `bi bi-arrow-${sortAscending ? 'up' : 'down'} sort-icon`;
    }

    loadProducts(1);
}

// ─── Search ────────────────────────────────────────────────────────────────────
function debounceSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => loadProducts(1), 400);
}

function resetFilters() {
    document.getElementById('searchInput').value = '';
    document.getElementById('categoryFilter').value = '';
    document.getElementById('typeFilter').value = '';
    document.getElementById('statusFilter').value = '';
    document.getElementById('pageSizeSelect').value = '10';
    currentPage = 1;
    loadProducts();
}

// ─── Loading State ─────────────────────────────────────────────────────────────
function setTableLoading(loading) {
    isLoading = loading;
    const wrapper = document.getElementById('tableWrapper');
    if (loading) {
        wrapper.style.opacity = '0.6';
        wrapper.style.pointerEvents = 'none';
    } else {
        wrapper.style.opacity = '1';
        wrapper.style.pointerEvents = '';
    }
}

function showTableError(msg) {
    document.getElementById('productsBody').innerHTML = `
        <tr><td colspan="12" class="text-center py-4 text-danger">
            <i class="bi bi-exclamation-triangle me-2"></i>${msg}
        </td></tr>`;
}

// ─── CREATE ────────────────────────────────────────────────────────────────────
function openCreateModal() {
    editingProductId = null;
    document.getElementById('modalTitleText').textContent = 'Yeni Ürün Ekle';
    document.getElementById('saveBtnText').textContent = 'Kaydet';
    document.getElementById('initialStockWrapper').style.display = '';
    clearForm();
    clearErrors();
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productModal'));
    modal.show();
}

// ─── EDIT ──────────────────────────────────────────────────────────────────────
async function editProduct(id) {
    editingProductId = id;
    document.getElementById('modalTitleText').textContent = 'Ürünü Düzenle';
    document.getElementById('saveBtnText').textContent = 'Güncelle';
    document.getElementById('initialStockWrapper').style.display = 'none';
    clearErrors();

    try {
        const data = await efbisAjax.get(`/Products/Edit/${id}`);
        populateForm(data);
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productModal'));
        modal.show();
    } catch (err) {
        showToast('Ürün verileri yüklenemedi.', 'error');
    }
}

function populateForm(data) {
    setValue('productId', data.id);
    setValue('productName', data.productName);
    setValue('productCode', data.productCode);
    setValue('barcode', data.barcode || '');
    setValue('categoryId', data.categoryId || '');
    setValue('productType', data.productType);
    setValue('unit', data.unit);
    setValue('purchasePrice', data.purchasePrice);
    setValue('purchaseVatRate', data.purchaseVatRate);
    setValue('discountType', data.discountType);
    setValue('discountValue', data.discountValue);
    setValue('salePrice', data.salePrice);
    setValue('saleVatRate', data.saleVatRate);
    setValue('specialTaxType', data.specialTaxType);
    setValue('specialTaxValue', data.specialTaxValue || '');
    setValue('communicationTaxRate', data.communicationTaxRate || '');
    setValue('description', data.description || '');
    setValue('minimumStock', data.minimumStock);

    // Radio buttons
    document.querySelectorAll('input[name="purchaseVatIncluded"]').forEach(r => {
        r.checked = (r.value === String(data.purchaseVatIncluded).toLowerCase());
    });
    document.querySelectorAll('input[name="saleVatIncluded"]').forEach(r => {
        r.checked = (r.value === String(data.saleVatIncluded).toLowerCase());
    });

    toggleDiscountField();
    toggleSpecialTax();
    calcProfit();
}

// ─── SAVE ──────────────────────────────────────────────────────────────────────
async function saveProduct() {
    clearErrors();

    const isEdit = editingProductId !== null;
    const dto = buildDto(isEdit);

    if (!validateForm(dto)) return;

    setSaveLoading(true);

    try {
        const url = isEdit ? '/Products/Update' : '/Products/Create';
        const result = await efbisAjax.post(url, dto);

        if (result.success) {
            bootstrap.Modal.getOrCreateInstance(document.getElementById('productModal')).hide();
            showToast(result.message, 'success');
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (err) {
        showToast('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
    } finally {
        setSaveLoading(false);
    }
}

function buildDto(isEdit) {
    const categoryVal = getValue('categoryId');
    const specialTaxVal = getValue('specialTaxValue');
    const commTaxVal = getValue('communicationTaxRate');

    const dto = {
        ProductName: getValue('productName').trim(),
        ProductCode: getValue('productCode').trim(),
        Barcode: getValue('barcode').trim() || null,
        CategoryId: (categoryVal && !isNaN(parseInt(categoryVal))) ? parseInt(categoryVal) : null,
        ProductType: parseInt(getValue('productType')) || 1,
        Unit: parseInt(getValue('unit')) || 1,
        PurchasePrice: parseFloat(getValue('purchasePrice')) || 0,
        PurchaseVatRate: !isNaN(parseInt(getValue('purchaseVatRate'))) ? parseInt(getValue('purchaseVatRate')) : 20,
        PurchaseVatIncluded: document.querySelector('input[name="purchaseVatIncluded"]:checked')?.value === 'true',
        DiscountType: parseInt(getValue('discountType')) || 0,
        DiscountValue: parseFloat(getValue('discountValue')) || 0,
        SalePrice: parseFloat(getValue('salePrice')) || 0,
        SaleVatRate: !isNaN(parseInt(getValue('saleVatRate'))) ? parseInt(getValue('saleVatRate')) : 20,
        SaleVatIncluded: document.querySelector('input[name="saleVatIncluded"]:checked')?.value === 'true',
        SpecialTaxType: parseInt(getValue('specialTaxType')) || 0,
        SpecialTaxValue: (specialTaxVal && !isNaN(parseFloat(specialTaxVal))) ? parseFloat(specialTaxVal) : null,
        CommunicationTaxRate: (commTaxVal && !isNaN(parseFloat(commTaxVal))) ? parseFloat(commTaxVal) : null,
        Description: getValue('description').trim() || null,
        MinimumStock: parseFloat(getValue('minimumStock')) || 0
    };

    if (!isEdit) {
        dto.InitialStock = parseFloat(getValue('initialStock')) || 0;
    } else {
        dto.Id = editingProductId;
    }

    return dto;
}

function validateForm(dto) {
    let valid = true;

    if (!dto.ProductName?.trim()) {
        showFieldError('err-productName', 'Ürün adı zorunludur.');
        document.getElementById('productName').classList.add('is-invalid');
        valid = false;
    }
    if (!dto.ProductCode?.trim()) {
        showFieldError('err-productCode', 'Ürün kodu zorunludur.');
        document.getElementById('productCode').classList.add('is-invalid');
        valid = false;
    }
    if (dto.PurchasePrice < 0) {
        showToast('Alış fiyatı negatif olamaz.', 'warning');
        valid = false;
    }
    if (dto.SalePrice < 0) {
        showToast('Satış fiyatı negatif olamaz.', 'warning');
        valid = false;
    }
    if (dto.MinimumStock < 0) {
        showToast('Minimum stok miktarı negatif olamaz.', 'warning');
        valid = false;
    }

    return valid;
}

// ─── DELETE ────────────────────────────────────────────────────────────────────
function deleteProduct(id, name) {
    deletingProductId = id;
    document.getElementById('deleteProductName').textContent = name;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteModal')).show();
}

async function confirmDelete() {
    if (!deletingProductId) return;

    const formData = new FormData();
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    try {
        const response = await fetch(`/Products/Delete/${deletingProductId}`, {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteModal')).hide();

        if (result.success) {
            showToast(result.message, 'success');
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (err) {
        showToast('Silme işlemi başarısız.', 'error');
    }
}

// ─── TOGGLE STATUS ─────────────────────────────────────────────────────────────
async function toggleStatus(id) {
    const formData = new FormData();
    formData.append('__RequestVerificationToken', document.querySelector('input[name="__RequestVerificationToken"]').value);

    try {
        const response = await fetch(`/Products/ToggleStatus/${id}`, {
            method: 'POST',
            body: formData
        });
        const result = await response.json();

        if (result.success) {
            showToast(result.message, 'success');
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (err) {
        showToast('Durum değiştirme işlemi başarısız.', 'error');
    }
}

// ─── DETAIL ────────────────────────────────────────────────────────────────────
async function showDetail(id) {
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('detailModal'));
    document.getElementById('detailBody').innerHTML = `<div class="text-center py-4"><div class="spinner-efbis mx-auto"></div></div>`;
    modal.show();

    try {
        const p = await efbisAjax.get(`/Products/Detail/${id}`);

        document.getElementById('detailBody').innerHTML = `
            <div class="row g-3">
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-info-circle"></i> Temel Bilgiler</div>
                            ${detailRow('Ürün Adı', p.productName)}
                            ${detailRow('Ürün Kodu', `<code style="background:#e2e8f0;padding:2px 7px;border-radius:5px;">${escHtml(p.productCode)}</code>`)}
                            ${detailRow('Barkod', p.barcode || '—')}
                            ${detailRow('Kategori', p.categoryName || '—')}
                            ${detailRow('Ürün Tipi', p.productTypeDisplay)}
                            ${detailRow('Birim', p.unitDisplay)}
                            ${detailRow('Durum', renderStatusBadge(p.status))}
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-currency-dollar"></i> Fiyat & Stok</div>
                            ${detailRow('Alış Fiyatı', `<strong>${formatCurrency(p.purchasePrice)}</strong>`)}
                            ${detailRow('Satış Fiyatı', `<strong>${formatCurrency(p.salePrice)}</strong>`)}
                            ${detailRow('Kâr Marjı', `<span class="text-success fw-600">%${p.profitMarginPercent}</span>`)}
                            ${detailRow('Güncel Stok', `<span class="stock-number">${formatNumber(p.currentStock)}</span>`)}
                            ${detailRow('Minimum Stok', formatNumber(p.minimumStock))}
                            ${detailRow('Stok Durumu', renderStockBadge(p.stockStatus))}
                            ${detailRow('Oluşturma Tarihi', formatDate(p.createdDate))}
                            ${p.updatedDate ? detailRow('Güncelleme Tarihi', formatDate(p.updatedDate)) : ''}
                        </div>
                    </div>
                </div>
            </div>`;
    } catch (err) {
        document.getElementById('detailBody').innerHTML = `<div class="text-center text-danger py-4">Detay yüklenemedi.</div>`;
    }
}

function detailRow(label, value) {
    return `<div class="d-flex justify-content-between align-items-center py-2 border-bottom" style="border-color:#e2e8f0 !important;">
                <span style="font-size:12.5px;color:#64748b;font-weight:500;">${label}</span>
                <span style="font-size:13.5px;font-weight:500;">${value}</span>
            </div>`;
}

// ─── Form Helpers ──────────────────────────────────────────────────────────────
function clearForm() {
    const ids = ['productId','productName','productCode','barcode','categoryId','purchasePrice',
                 'discountValue','salePrice','specialTaxValue','communicationTaxRate','description',
                 'initialStock','minimumStock'];
    ids.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = id === 'productId' ? '0' : (id.includes('Price') || id.includes('Stock') || id.includes('Value') ? '0' : '');
    });

    // Selects
    setValue('productType', '1');
    setValue('unit', '1');
    setValue('purchaseVatRate', '20');
    setValue('saleVatRate', '20');
    setValue('discountType', '0');
    setValue('specialTaxType', '0');

    // Radios
    document.getElementById('vatExcluded')?.click();
    document.getElementById('saleVatExcluded')?.click();

    toggleDiscountField();
    toggleSpecialTax();

    document.getElementById('profitIndicator').style.display = 'none';
}

function clearErrors() {
    document.querySelectorAll('[id^="err-"]').forEach(el => el.textContent = '');
    document.querySelectorAll('.efbis-input.is-invalid').forEach(el => el.classList.remove('is-invalid'));
}

function showFieldError(id, msg) {
    const el = document.getElementById(id);
    if (el) el.textContent = msg;
}

function getValue(id) {
    return document.getElementById(id)?.value || '';
}

function setValue(id, val) {
    const el = document.getElementById(id);
    if (el) el.value = val;
}

function setSaveLoading(loading) {
    const btn = document.getElementById('btnSaveProduct');
    const icon = document.getElementById('saveBtnIcon');
    const text = document.getElementById('saveBtnText');
    if (!btn) return;
    btn.disabled = loading;
    if (loading) {
        icon.innerHTML = '<span class="spinner-efbis" style="width:16px;height:16px;border-width:2px;"></span>';
        text.textContent = ' Kaydediliyor...';
    } else {
        icon.innerHTML = '<i class="bi bi-check-lg me-1"></i>';
        text.textContent = editingProductId ? 'Güncelle' : 'Kaydet';
    }
}

// ─── Dynamic Field Toggles ─────────────────────────────────────────────────────
function toggleDiscountField() {
    const type = parseInt(getValue('discountType'));
    const wrapper = document.getElementById('discountValueWrapper');
    const label = document.getElementById('discountValueLabel');
    if (type === 0) {
        wrapper.style.display = 'none';
    } else {
        wrapper.style.display = '';
        label.textContent = type === 1 ? 'İskonto Yüzdesi (%)' : 'İskonto Tutarı (₺)';
    }
}

function toggleSpecialTax() {
    const type = parseInt(getValue('specialTaxType'));
    const wrapper = document.getElementById('specialTaxValueWrapper');
    const label = document.getElementById('specialTaxValueLabel');
    if (type === 0) {
        wrapper.style.display = 'none';
    } else {
        wrapper.style.display = '';
        label.textContent = type === 1 ? 'ÖTV Oranı (%)' : 'ÖTV Tutarı (₺)';
    }
}

function calcProfit() {
    const purchasePrice = parseFloat(getValue('purchasePrice')) || 0;
    const salePrice = parseFloat(getValue('salePrice')) || 0;
    const indicator = document.getElementById('profitIndicator');

    if (purchasePrice > 0 && salePrice > 0) {
        const profit = salePrice - purchasePrice;
        const margin = ((profit / purchasePrice) * 100).toFixed(1);
        document.getElementById('profitPercent').textContent = `%${margin}`;
        document.getElementById('profitAmount').textContent = formatCurrency(profit);
        indicator.style.display = '';
        indicator.style.background = profit >= 0 ? '#f0fdf4' : '#fef2f2';
        indicator.style.borderColor = profit >= 0 ? '#bbf7d0' : '#fecaca';
        document.querySelectorAll('#profitIndicator strong').forEach(el => {
            el.className = profit >= 0 ? 'text-success' : 'text-danger';
        });
    } else {
        indicator.style.display = 'none';
    }
}

function updateStockPreview() {
    // Stock preview is static info text — no dynamic update needed for now
}

// ─── Utility ───────────────────────────────────────────────────────────────────
function escHtml(text) {
    if (text == null) return '';
    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}

// ─── Product Barcode Label Generator & Printing ─────────────────────────────
function openProductBarcodeModal(id, code, name, price, barcode) {
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('productBarcodeModal'));
    document.getElementById('barcodeModalProdName').textContent = name;
    document.getElementById('barcodeModalProdCode').textContent = `KOD: ${code}`;
    document.getElementById('barcodeModalBarcodeVal').textContent = barcode || code || `869${id}0001`;
    document.getElementById('barcodeModalPrice').textContent = formatCurrency(price);
    modal.show();
}

function printProductBarcodeLabel() {
    const content = document.getElementById('barcodePrintArea')?.innerHTML;
    if (!content) return;

    const printWin = window.open('', '_blank', 'width=500,height=500');
    printWin.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Ürün Barkod Etiketi</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
            <style>
                body { padding: 20px; background:#fff; text-align:center; }
                @media print { body { padding:0; } }
            </style>
        </head>
        <body onload="window.print();window.close();">
            <div style="width:300px;margin:0 auto;border:1px solid #000;padding:15px;border-radius:8px;">
                ${content}
            </div>
        </body>
        </html>
    `);
    printWin.document.close();
}
