// ─── Satışta Olan Ürünler JavaScript Module ────────────────────────────────────
// Anlık Toggle Switch, Toplu İşlemler, Filtreleme, Detay Modal, Sayfalama

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 10;
let sortColumn = 'ProductName';
let sortAscending = true;
let searchTimeout = null;
let selectedIds = new Set();
let pendingToggleProduct = null; // { id, targetStatus, name, isPassive }

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    refreshAll();
});

function refreshAll() {
    loadDashboard();
    loadProducts();
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const res = await fetch('/SalesProducts/GetDashboard');
        if (!res.ok) return;
        const stats = await res.json();

        document.getElementById('dashTotal').textContent = stats.totalProducts ?? stats.TotalProducts ?? 0;
        document.getElementById('dashAvailable').textContent = stats.availableForSale ?? stats.AvailableForSale ?? 0;
        document.getElementById('dashUnavailable').textContent = stats.notAvailableForSale ?? stats.NotAvailableForSale ?? 0;
        document.getElementById('dashPassive').textContent = stats.passiveProducts ?? stats.PassiveProducts ?? 0;
        document.getElementById('dashCritical').textContent = stats.criticalStock ?? stats.CriticalStock ?? 0;
        document.getElementById('dashOutOfStock').textContent = stats.outOfStock ?? stats.OutOfStock ?? 0;
    } catch (e) {
        console.error('Dashboard yüklenemedi:', e);
    }
}

// ─── Stat Card Click Filtering ────────────────────────────────────────────────
function filterByCard(type) {
    document.querySelectorAll('.sales-stat-card').forEach(c => c.classList.remove('active-filter'));

    resetFilterInputs(false);

    if (type === 'all') {
        document.getElementById('card-total').classList.add('active-filter');
    } else if (type === 'available') {
        document.getElementById('card-available').classList.add('active-filter');
        document.getElementById('saleStatusFilter').value = 'true';
    } else if (type === 'unavailable') {
        document.getElementById('card-unavailable').classList.add('active-filter');
        document.getElementById('saleStatusFilter').value = 'false';
    } else if (type === 'passive') {
        document.getElementById('card-passive').classList.add('active-filter');
        document.getElementById('productStatusFilter').value = '2';
    } else if (type === 'critical') {
        document.getElementById('card-critical').classList.add('active-filter');
        document.getElementById('stockStatusFilter').value = '3';
    } else if (type === 'outofstock') {
        document.getElementById('card-outofstock').classList.add('active-filter');
        document.getElementById('stockStatusFilter').value = '4';
    }

    loadProducts(1);
}

// ─── Load Products List ────────────────────────────────────────────────────────
async function loadProducts(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: document.getElementById('searchInput')?.value || '',
        categoryId: document.getElementById('categoryFilter')?.value || '',
        isAvailableForSale: document.getElementById('saleStatusFilter')?.value || '',
        status: document.getElementById('productStatusFilter')?.value || '',
        stockStatusFilter: document.getElementById('stockStatusFilter')?.value || '',
        onlyBelowMinStock: document.getElementById('belowMinStock')?.checked || false,
        onlyOutOfStock: document.getElementById('outOfStockOnly')?.checked || false,
        minPrice: document.getElementById('minPrice')?.value || '',
        maxPrice: document.getElementById('maxPrice')?.value || '',
        sortBy: sortColumn,
        ascending: sortAscending
    });

    setTableLoading(true);

    try {
        const res = await fetch(`/SalesProducts/GetProducts?${params}`);
        if (!res.ok) throw new Error('Sunucu yanıt vermedi');
        const data = await res.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} ürün`;

        updateSelectAllState();
    } catch (err) {
        console.error(err);
        showTableError('Satış ürünleri yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

// ─── Render Table ──────────────────────────────────────────────────────────────
function renderTable(items) {
    const tbody = document.getElementById('salesBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="14">
                    <div class="empty-state">
                        <i class="bi bi-bag-x"></i>
                        <h6>Ürün bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili filtrelere uygun ürün bulunamadı.</p>
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
        const unitDisplay = p.UnitDisplay || p.unitDisplay || '';
        const salePrice = p.SalePrice ?? p.salePrice ?? 0;
        const profitMarginPercent = p.ProfitMarginPercent ?? p.profitMarginPercent ?? 0;
        const currentStock = p.CurrentStock ?? p.currentStock ?? 0;
        const minimumStock = p.MinimumStock ?? p.minimumStock ?? 0;
        const stockStatus = p.StockStatus ?? p.stockStatus ?? 1;
        const isAvailableForSale = p.IsAvailableForSale ?? p.isAvailableForSale ?? false;
        const status = p.Status ?? p.status ?? 1;
        const saleStatusUpdatedDate = p.SaleStatusUpdatedDate || p.saleStatusUpdatedDate;
        const updatedDate = p.UpdatedDate || p.updatedDate;
        const createdDate = p.CreatedDate || p.createdDate;

        const isSelected = selectedIds.has(id);
        const isPassive = status === 2; // ProductStatus.Passive

        return `
        <tr id="row-${id}" class="${isSelected ? 'row-selected' : ''}">
            <td>
                <input type="checkbox" class="form-check-input row-checkbox m-0"
                       value="${id}" ${isSelected ? 'checked' : ''}
                       onchange="toggleRowSelect(${id}, this.checked)" />
            </td>
            <td>
                <div class="product-name-cell">
                    <span class="product-name">${escHtml(name)}</span>
                </div>
            </td>
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td style="color:#94a3b8;font-size:12px;">${barcode ? escHtml(barcode) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td><span style="font-size:13px;">${categoryName ? escHtml(categoryName) : '<span style="color:#cbd5e1;">—</span>'}</span></td>
            <td style="font-size:13px;color:#64748b;">${escHtml(unitDisplay)}</td>
            <td class="price-cell">
                ${formatCurrency(salePrice)}
                ${profitMarginPercent !== 0 ? `
                    <span class="profit-badge ${profitMarginPercent > 0 ? 'positive' : 'negative'}">
                        %${profitMarginPercent}
                    </span>` : ''}
            </td>
            <td>
                <span class="stock-number">${formatNumber(currentStock)}</span>
            </td>
            <td style="font-size:13px;color:#64748b;">${formatNumber(minimumStock)}</td>
            <td>${renderStockBadge(stockStatus)}</td>
            <td>
                <div class="d-flex align-items-center gap-2">
                    <label class="toggle-switch" title="${isPassive ? 'Pasif ürün satışa açılamaz' : 'Satış Durumunu Değiştir'}">
                        <input type="checkbox" id="toggle-${id}"
                               ${isAvailableForSale ? 'checked' : ''}
                               ${isPassive ? 'disabled' : ''}
                               onchange="handleToggleClick(${id}, this.checked, '${escHtml(name)}', ${isPassive})" />
                        <span class="toggle-slider"></span>
                    </label>
                    <span class="toggle-label ${isAvailableForSale ? 'on' : 'off'}" id="toggle-label-${id}">
                        ${isAvailableForSale ? 'Satışta' : 'Satış Dışı'}
                    </span>
                </div>
            </td>
            <td>${renderProductStatusBadge(status)}</td>
            <td style="font-size:12px;color:#94a3b8;">${formatDate(saleStatusUpdatedDate || updatedDate || createdDate)}</td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-detail" title="Ürün Kartı" onclick="showDetail(${id})" aria-label="Detay">
                        <i class="bi bi-eye"></i>
                    </button>
                    <button class="btn-action btn-action-edit" title="Fiyat Değiştir" onclick="openPriceModal(${id}, '${escHtml(name)}', ${salePrice}, ${p.SaleVatRate || p.saleVatRate || 20})" aria-label="Fiyat">
                        <i class="bi bi-tag"></i>
                    </button>
                    <a class="btn-action btn-action-status" title="Tüm Ürünler'de Düzenle" href="/Products" aria-label="Düzenle">
                        <i class="bi bi-pencil"></i>
                    </a>
                </div>
            </td>
        </tr>`;
    }).join('');
}

// ─── Badges ────────────────────────────────────────────────────────────────────
function renderStockBadge(status) {
    switch (status) {
        case 1: return '<span class="badge-stock-sufficient">🟢 YETERLİ</span>';
        case 2: return '<span class="badge-stock-low">🟡 AZ STOK</span>';
        case 3: return '<span class="badge-stock-critical">🔴 KRİTİK STOK</span>';
        case 4: return '<span class="badge-stock-outofstock">⚫ STOK YOK</span>';
        default: return '<span class="badge-stock-sufficient">🟢 YETERLİ</span>';
    }
}

function renderProductStatusBadge(status) {
    return status === 1
        ? '<span class="badge-status-active">Aktif</span>'
        : '<span class="badge-status-passive">Pasif</span>';
}

// ─── Toggle Switch Action ──────────────────────────────────────────────────────
function handleToggleClick(productId, newCheckedState, productName, isPassive) {
    const toggleEl = document.getElementById(`toggle-${productId}`);

    if (isPassive && newCheckedState) {
        if (toggleEl) toggleEl.checked = false;
        showToast('Pasif durumdaki ürün satışa açılamaz.', 'warning');
        return;
    }

    pendingToggleProduct = {
        id: productId,
        targetStatus: newCheckedState,
        name: productName,
        isPassive: isPassive
    };

    const iconDiv = document.getElementById('saleStatusIcon');
    const titleEl = document.getElementById('saleStatusTitle');
    const msgEl = document.getElementById('saleStatusMessage');
    const btn = document.getElementById('btnConfirmSaleStatus');

    if (newCheckedState) {
        iconDiv.style.background = '#d1fae5';
        iconDiv.innerHTML = '<i class="bi bi-bag-check" style="color:#059669;font-size:28px;"></i>';
        titleEl.textContent = 'Satışa Aç';
        msgEl.innerHTML = `<strong>${productName}</strong> ürününü satışa açmak istiyor musunuz? Bu ürün artık satış işlemlerinde seçilebilecektir.`;
        btn.className = 'btn btn-success fw-600';
    } else {
        iconDiv.style.background = '#ffedd5';
        iconDiv.innerHTML = '<i class="bi bi-bag-x" style="color:#ea580c;font-size:28px;"></i>';
        titleEl.textContent = 'Satıştan Kaldır';
        msgEl.innerHTML = `<strong>${productName}</strong> ürününü satıştan kaldırmak istiyor musunuz? Bu ürün yeni satış işlemlerinde seçilemeyecektir.`;
        btn.className = 'btn btn-warning fw-600';
    }

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('saleStatusModal'));
    modal.show();

    const modalEl = document.getElementById('saleStatusModal');
    const onHide = () => {
        if (toggleEl && pendingToggleProduct && pendingToggleProduct.id === productId) {
            toggleEl.checked = !newCheckedState;
        }
        modalEl.removeEventListener('hide.bs.modal', onHide);
    };
    modalEl.addEventListener('hide.bs.modal', onHide);
}

async function confirmSaleStatusChange() {
    if (!pendingToggleProduct) return;
    const { id, targetStatus } = pendingToggleProduct;
    pendingToggleProduct = null;

    const modal = bootstrap.Modal.getInstance(document.getElementById('saleStatusModal'));
    if (modal) modal.hide();

    try {
        const result = await efbisAjax.post('/SalesProducts/UpdateSaleStatus', {
            ProductId: id,
            IsAvailableForSale: targetStatus,
            UpdatedBy: 'Admin'
        });

        if (result.success) {
            showToast(result.message, 'success');
            const label = document.getElementById(`toggle-label-${id}`);
            if (label) {
                label.textContent = targetStatus ? 'Satışta' : 'Satış Dışı';
                label.className = `toggle-label ${targetStatus ? 'on' : 'off'}`;
            }
            loadDashboard();
        } else {
            showToast(result.message, 'error');
            const toggleEl = document.getElementById(`toggle-${id}`);
            if (toggleEl) toggleEl.checked = !targetStatus;
        }
    } catch (err) {
        showToast('İşlem başarısız oldu.', 'error');
        const toggleEl = document.getElementById(`toggle-${id}`);
        if (toggleEl) toggleEl.checked = !targetStatus;
    }
}

// ─── Single Item Price Update ─────────────────────────────────────────────────
function openPriceModal(id, name, currentPrice, vatRate) {
    document.getElementById('priceProductId').value = id;
    document.getElementById('priceProductName').textContent = name;
    document.getElementById('newSalePrice').value = currentPrice;
    document.getElementById('newSaleVatRate').value = vatRate || 20;

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('priceModal'));
    modal.show();
}

async function saveSalePrice() {
    const id = parseInt(document.getElementById('priceProductId').value);
    const newPrice = parseFloat(document.getElementById('newSalePrice').value) || 0;
    const vatRate = parseInt(document.getElementById('newSaleVatRate').value) || 20;
    const vatIncluded = document.querySelector('input[name="newVatIncluded"]:checked')?.value === 'true';

    if (newPrice < 0) {
        showToast('Fiyat negatif olamaz.', 'warning');
        return;
    }

    try {
        const result = await efbisAjax.post('/SalesProducts/UpdateSalePrice', {
            ProductId: id,
            SalePrice: newPrice,
            SaleVatRate: vatRate,
            SaleVatIncluded: vatIncluded
        });

        bootstrap.Modal.getInstance(document.getElementById('priceModal'))?.hide();

        if (result.success) {
            showToast(result.message, 'success');
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Fiyat güncellenemedi.', 'error');
    }
}

// ─── Selection & Bulk Actions ─────────────────────────────────────────────────
function toggleRowSelect(id, checked) {
    if (checked) {
        selectedIds.add(id);
        document.getElementById(`row-${id}`)?.classList.add('row-selected');
    } else {
        selectedIds.delete(id);
        document.getElementById(`row-${id}`)?.classList.remove('row-selected');
    }
    updateBulkBar();
}

function toggleSelectAll() {
    const checkAll = document.getElementById('selectAll').checked;
    document.querySelectorAll('.row-checkbox').forEach(cb => {
        cb.checked = checkAll;
        const id = parseInt(cb.value);
        if (checkAll) {
            selectedIds.add(id);
            document.getElementById(`row-${id}`)?.classList.add('row-selected');
        } else {
            selectedIds.delete(id);
            document.getElementById(`row-${id}`)?.classList.remove('row-selected');
        }
    });
    updateBulkBar();
}

function clearSelection() {
    selectedIds.clear();
    document.querySelectorAll('.row-checkbox').forEach(cb => cb.checked = false);
    document.querySelectorAll('.efbis-table tbody tr').forEach(r => r.classList.remove('row-selected'));
    document.getElementById('selectAll').checked = false;
    updateBulkBar();
}

function updateSelectAllState() {
    const checkboxes = document.querySelectorAll('.row-checkbox');
    if (checkboxes.length === 0) return;
    const allChecked = Array.from(checkboxes).every(cb => cb.checked);
    document.getElementById('selectAll').checked = allChecked;
    updateBulkBar();
}

function updateBulkBar() {
    const bar = document.getElementById('bulkActionBar');
    const countSpan = document.getElementById('selectedCount');
    countSpan.textContent = selectedIds.size;

    if (selectedIds.size > 0) {
        bar.classList.remove('d-none');
    } else {
        bar.classList.add('d-none');
    }
}

async function bulkSaleStatus(isAvailable) {
    if (selectedIds.size === 0) return;

    try {
        const result = await efbisAjax.post('/SalesProducts/BulkSaleStatus', {
            ProductIds: Array.from(selectedIds),
            IsAvailableForSale: isAvailable,
            UpdatedBy: 'Admin'
        });

        if (result.success) {
            showToast(result.message, 'success');
            clearSelection();
            refreshAll();
        } else {
            showToast(result.message, 'warning');
        }
    } catch (e) {
        showToast('Toplu işlem başarısız oldu.', 'error');
    }
}

async function bulkActivate() {
    if (selectedIds.size === 0) return;
    try {
        const result = await efbisAjax.post('/SalesProducts/BulkStatus', {
            ProductIds: Array.from(selectedIds),
            Status: 1,
            UpdatedBy: 'Admin'
        });
        if (result.success) {
            showToast(result.message, 'success');
            clearSelection();
            refreshAll();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('İşlem başarısız.', 'error');
    }
}

async function bulkPassivate() {
    if (selectedIds.size === 0) return;
    try {
        const result = await efbisAjax.post('/SalesProducts/BulkStatus', {
            ProductIds: Array.from(selectedIds),
            Status: 2,
            UpdatedBy: 'Admin'
        });
        if (result.success) {
            showToast(result.message + ' (Ürünler satıştan da kaldırıldı)', 'warning');
            clearSelection();
            refreshAll();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('İşlem başarısız.', 'error');
    }
}

function openBulkPriceModal() {
    if (selectedIds.size === 0) return;
    document.getElementById('bulkPriceCount').textContent = selectedIds.size;
    document.getElementById('bulkNewPrice').value = '';
    bootstrap.Modal.getOrCreateInstance(document.getElementById('bulkPriceModal')).show();
}

async function saveBulkPrice() {
    const price = parseFloat(document.getElementById('bulkNewPrice').value) || 0;
    if (price < 0) {
        showToast('Fiyat negatif olamaz.', 'warning');
        return;
    }
    try {
        const result = await efbisAjax.post('/SalesProducts/BulkPrice', {
            ProductIds: Array.from(selectedIds),
            NewPrice: price,
            UpdatedBy: 'Admin'
        });
        bootstrap.Modal.getInstance(document.getElementById('bulkPriceModal'))?.hide();
        if (result.success) {
            showToast(result.message, 'success');
            clearSelection();
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Toplu fiyat güncellenemedi.', 'error');
    }
}

function openBulkCategoryModal() {
    if (selectedIds.size === 0) return;
    document.getElementById('bulkCategoryCount').textContent = selectedIds.size;
    document.getElementById('bulkNewCategory').value = '';
    bootstrap.Modal.getOrCreateInstance(document.getElementById('bulkCategoryModal')).show();
}

async function saveBulkCategory() {
    const catId = parseInt(document.getElementById('bulkNewCategory').value);
    if (!catId) {
        showToast('Kategori seçmelisiniz.', 'warning');
        return;
    }
    try {
        const result = await efbisAjax.post('/SalesProducts/BulkCategory', {
            ProductIds: Array.from(selectedIds),
            CategoryId: catId,
            UpdatedBy: 'Admin'
        });
        bootstrap.Modal.getInstance(document.getElementById('bulkCategoryModal'))?.hide();
        if (result.success) {
            showToast(result.message, 'success');
            clearSelection();
            loadProducts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Kategori güncellenemedi.', 'error');
    }
}

// ─── Detail Modal ─────────────────────────────────────────────────────────────
async function showDetail(id) {
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('detailModal'));
    document.getElementById('detailBody').innerHTML = `<div class="text-center py-4"><div class="spinner-efbis mx-auto"></div></div>`;
    modal.show();

    try {
        const p = await efbisAjax.get(`/SalesProducts/GetDetail/${id}`);

        const name = p.ProductName || p.productName || '';
        const code = p.ProductCode || p.productCode || '';
        const barcode = p.Barcode || p.barcode || '';
        const catName = p.CategoryName || p.categoryName || '';
        const typeDisp = p.ProductTypeDisplay || p.productTypeDisplay || '';
        const unitDisp = p.UnitDisplay || p.unitDisplay || '';
        const pPrice = p.PurchasePrice ?? p.purchasePrice ?? 0;
        const sPrice = p.SalePrice ?? p.salePrice ?? 0;
        const sPriceWithVat = p.SalePriceWithVat ?? p.salePriceWithVat ?? 0;
        const margin = p.ProfitMarginPercent ?? p.profitMarginPercent ?? 0;
        const currentStock = p.CurrentStock ?? p.currentStock ?? 0;
        const minStock = p.MinimumStock ?? p.minimumStock ?? 0;
        const stockStatus = p.StockStatus ?? p.stockStatus ?? 1;
        const isAvail = p.IsAvailableForSale ?? p.isAvailableForSale ?? false;
        const status = p.Status ?? p.status ?? 1;
        const desc = p.Description || p.description;
        const createdDate = p.CreatedDate || p.createdDate;
        const updatedDate = p.UpdatedDate || p.updatedDate;

        document.getElementById('detailBody').innerHTML = `
            <div class="row g-3">
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-info-circle text-primary"></i> Temel Bilgiler</div>
                            ${detailRow('Ürün Adı', escHtml(name))}
                            ${detailRow('Ürün Kodu', `<code style="background:#e2e8f0;padding:2px 7px;border-radius:5px;">${escHtml(code)}</code>`)}
                            ${detailRow('Barkod', barcode ? escHtml(barcode) : '—')}
                            ${detailRow('Kategori', catName ? escHtml(catName) : '—')}
                            ${detailRow('Ürün Tipi', escHtml(typeDisp))}
                            ${detailRow('Birim', escHtml(unitDisp))}
                            ${detailRow('Ürün Durumu', renderProductStatusBadge(status))}
                            ${detailRow('Satış Durumu', isAvail ? '<span class="badge-sale-available">Satışta</span>' : '<span class="badge-sale-unavailable">Satış Dışı</span>')}
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-currency-dollar text-success"></i> Fiyat & Stok</div>
                            ${detailRow('Alış Fiyatı (Salt Okunur)', formatCurrency(pPrice))}
                            ${detailRow('Satış Fiyatı', `<strong>${formatCurrency(sPrice)}</strong>`)}
                            ${detailRow('KDV Dahil Satış', formatCurrency(sPriceWithVat))}
                            ${detailRow('Kâr Marjı', `<span class="profit-badge ${margin >= 0 ? 'positive' : 'negative'}">%${margin}</span>`)}
                            ${detailRow('Güncel Stok', `<span class="stock-number">${formatNumber(currentStock)}</span>`)}
                            ${detailRow('Minimum Stok', formatNumber(minStock))}
                            ${detailRow('Stok Durumu', renderStockBadge(stockStatus))}
                            ${detailRow('Oluşturma', formatDate(createdDate))}
                            ${updatedDate ? detailRow('Son Güncelleme', formatDate(updatedDate)) : ''}
                        </div>
                    </div>
                </div>
                ${desc ? `
                <div class="col-12">
                    <div class="p-3 bg-light rounded" style="font-size:13px;">
                        <strong>Açıklama:</strong> ${escHtml(desc)}
                    </div>
                </div>` : ''}
            </div>`;
    } catch (e) {
        document.getElementById('detailBody').innerHTML = `<div class="text-center text-danger py-4">Ürün detayları yüklenemedi.</div>`;
    }
}

function detailRow(label, value) {
    return `<div class="d-flex justify-content-between align-items-center py-2 border-bottom" style="border-color:#e2e8f0 !important;">
                <span style="font-size:12.5px;color:#64748b;font-weight:500;">${label}</span>
                <span style="font-size:13.5px;font-weight:500;">${value}</span>
            </div>`;
}

// ─── Pagination ────────────────────────────────────────────────────────────────
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

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadProducts(${pageNumber - 1})" aria-label="Önceki"><i class="bi bi-chevron-left"></i></button>`;

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

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadProducts(${pageNumber + 1})" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></button>`;

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

// ─── Filters Reset & Search ────────────────────────────────────────────────────
function debounceSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => loadProducts(1), 400);
}

function resetFilters() {
    document.querySelectorAll('.sales-stat-card').forEach(c => c.classList.remove('active-filter'));
    resetFilterInputs(true);
    loadProducts(1);
}

function resetFilterInputs(reloadDashboard = false) {
    document.getElementById('searchInput').value = '';
    document.getElementById('categoryFilter').value = '';
    document.getElementById('saleStatusFilter').value = '';
    document.getElementById('productStatusFilter').value = '';
    document.getElementById('stockStatusFilter').value = '';
    document.getElementById('belowMinStock').checked = false;
    document.getElementById('outOfStockOnly').checked = false;
    document.getElementById('minPrice').value = '';
    document.getElementById('maxPrice').value = '';
    document.getElementById('pageSizeSelect').value = '10';
    if (reloadDashboard) loadDashboard();
}

// ─── Helpers ───────────────────────────────────────────────────────────────────
function setTableLoading(loading) {
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
    document.getElementById('salesBody').innerHTML = `
        <tr><td colspan="14" class="text-center py-4 text-danger">
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
