// ─── Depo Yönetimi JavaScript Module ─────────────────────────────────────────
// AJAX Listing, CRUD, Filters, Pagination, Export & Print

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 10;
let searchTimeout = null;

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    refreshAll();
});

function refreshAll() {
    loadDashboard();
    loadData();
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const response = await fetch('/Warehouses/GetWarehouses?pageSize=500');
        if (!response.ok) return;
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? items.length;

        let active = 0, passive = 0, defaultName = '—';

        items.forEach(w => {
            const status = w.Status ?? w.status ?? 1;
            const isDef = w.IsDefault ?? w.isDefault ?? false;
            const name = w.Name || w.name;

            if (status === 1) active++;
            if (status === 2) passive++;
            if (isDef) defaultName = name;
        });

        document.getElementById('statTotal').textContent = formatNumber(totalCount);
        document.getElementById('statActive').textContent = formatNumber(active);
        document.getElementById('statPassive').textContent = formatNumber(passive);
        document.getElementById('statDefault').textContent = defaultName;
    } catch (e) {
        console.error('Stats error:', e);
    }
}

// ─── Load Warehouses List ──────────────────────────────────────────────────────
async function loadData(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const searchTerm = document.getElementById('searchInput')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        status: status
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/Warehouses/GetWarehouses?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} depo`;
    } catch (e) {
        console.error(e);
        showTableError('Depolar yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('tableBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8">
                    <div class="empty-state">
                        <i class="bi bi-building"></i>
                        <h6>Depo bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili kriterlere uygun depo bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(w => {
        const id = w.Id ?? w.id;
        const code = w.WarehouseCode || w.warehouseCode || '';
        const name = w.Name || w.name || '';
        const city = w.City || w.city || '';
        const phone = w.Phone || w.phone || '';
        const address = w.Address || w.address || '';
        const isDefault = w.IsDefault ?? w.isDefault ?? false;
        const status = w.Status ?? w.status ?? 1;

        return `
        <tr id="row-wh-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td><strong>${escHtml(name)}</strong></td>
            <td>${city ? escHtml(city) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>${phone ? escHtml(phone) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="font-size:12.5px;color:#64748b;">${address ? escHtml(address) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>
                ${isDefault ? '<span class="badge bg-primary-subtle text-primary fw-600" style="font-size:12px;"><i class="bi bi-star-fill me-1"></i>Varsayılan</span>' : '<span style="color:#cbd5e1;">—</span>'}
            </td>
            <td>
                <span class="badge ${status === 1 ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}" style="font-size:12px;">
                    ${status === 1 ? 'Aktif' : 'Pasif'}
                </span>
            </td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-edit" onclick="openEditModal(${id})" title="Düzenle" aria-label="Düzenle">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action btn-action-delete" onclick="deleteWarehouse(${id}, '${escHtml(name)}')" title="Sil" aria-label="Sil">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

// ─── Modal Actions ────────────────────────────────────────────────────────────
function openCreateModal() {
    document.getElementById('warehouseForm').reset();
    document.getElementById('warehouseId').value = '';
    const randomNum = Math.floor(4 + Math.random() * 90);
    document.getElementById('warehouseCode').value = `DPO-MMP-${randomNum < 10 ? '0' + randomNum : randomNum}`;
    document.getElementById('status').value = '1';
    document.getElementById('isDefault').checked = false;

    document.getElementById('warehouseModalLabel').innerHTML = '<i class="bi bi-building me-2"></i>Yeni Depo Ekle';
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('warehouseModal'));
    modal.show();
}

async function openEditModal(id) {
    try {
        const item = await efbisAjax.get(`/Warehouses/GetDetail/${id}`);

        document.getElementById('warehouseId').value = item.Id ?? item.id;
        document.getElementById('warehouseCode').value = item.WarehouseCode || item.warehouseCode || '';
        document.getElementById('name').value = item.Name || item.name || '';
        document.getElementById('city').value = item.City || item.city || '';
        document.getElementById('phone').value = item.Phone || item.phone || '';
        document.getElementById('address').value = item.Address || item.address || '';
        document.getElementById('description').value = item.Description || item.description || '';
        document.getElementById('isDefault').checked = item.IsDefault ?? item.isDefault ?? false;
        document.getElementById('status').value = (item.Status ?? item.status ?? 1).toString();

        document.getElementById('warehouseModalLabel').innerHTML = '<i class="bi bi-pencil-square me-2"></i>Depo Düzenle';
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('warehouseModal'));
        modal.show();
    } catch (e) {
        showToast('Depo bilgileri yüklenemedi.', 'error');
    }
}

async function saveWarehouse() {
    const idVal = document.getElementById('warehouseId').value.trim();
    const code = document.getElementById('warehouseCode').value.trim();
    const name = document.getElementById('name').value.trim();
    const city = document.getElementById('city').value.trim();
    const phone = document.getElementById('phone').value.trim();
    const address = document.getElementById('address').value.trim();
    const description = document.getElementById('description').value.trim();
    const isDefault = document.getElementById('isDefault').checked;
    const status = parseInt(document.getElementById('status').value) || 1;

    if (!code || !name) {
        showToast('Depo kodu ve depo adı zorunludur.', 'warning');
        return;
    }

    const dto = {
        WarehouseCode: code,
        Name: name,
        City: city || null,
        Phone: phone || null,
        Address: address || null,
        Description: description || null,
        IsDefault: isDefault,
        Status: status
    };

    const isUpdate = idVal !== '' && !isNaN(parseInt(idVal));
    if (isUpdate) dto.Id = parseInt(idVal);

    const url = isUpdate ? `/Warehouses/Update/${idVal}` : '/Warehouses/Create';

    try {
        const result = await efbisAjax.post(url, dto);
        if (result.success || result.Id || result.id) {
            showToast(result.message || 'Depo başarıyla kaydedildi.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('warehouseModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message || 'Depo kaydedilemedi.', 'error');
        }
    } catch (e) {
        console.error(e);
        showToast(e.message || 'Depo kaydedilirken bir hata oluştu.', 'error');
    }
}

async function deleteWarehouse(id, name) {
    if (!confirm(`"${name}" isimli depoyu silmek istediğinize emin misiniz?`)) return;

    try {
        const result = await efbisAjax.post(`/Warehouses/Delete/${id}`, {});
        if (result.success) {
            showToast(result.message || 'Depo başarıyla silindi.', 'success');
            refreshAll();
        } else {
            showToast(result.message || 'Depo silinemedi.', 'error');
        }
    } catch (e) {
        showToast('Depo silinirken bir hata oluştu.', 'error');
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
    document.getElementById('tableBody').innerHTML = `
        <tr><td colspan="8" class="text-center py-4 text-danger">
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

function formatNumber(val) {
    const num = parseFloat(val) || 0;
    return num.toLocaleString('tr-TR');
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
