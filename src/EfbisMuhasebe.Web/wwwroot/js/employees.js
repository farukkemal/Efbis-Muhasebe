// ─── Personel Yönetimi JavaScript Module ─────────────────────────────────────
// AJAX Listing, CRUD, Filters, Pagination, Export & Print

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 25;
let searchTimeout = null;
let warehouses = [];

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    refreshAll();
    loadWarehousesDropdown();
});

function refreshAll() {
    loadDashboard();
    loadData();
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const response = await fetch('/Employees/GetDashboard');
        if (!response.ok) return;
        const data = await response.json();

        const total = data.TotalEmployees ?? data.totalEmployees ?? 0;
        const active = data.ActiveEmployees ?? data.activeEmployees ?? 0;
        const salary = data.TotalMonthlySalary ?? data.totalMonthlySalary ?? 0;
        const whCount = data.WarehouseStaffCount ?? data.warehouseStaffCount ?? 0;
        const cashierCount = data.CashierStaffCount ?? data.cashierStaffCount ?? 0;
        const salesCount = data.SalesStaffCount ?? data.salesStaffCount ?? 0;
        const consultantCount = data.ConsultantStaffCount ?? data.consultantStaffCount ?? 0;

        document.getElementById('statTotal').textContent = formatNumber(total);
        document.getElementById('statActive').textContent = formatNumber(active);
        document.getElementById('statMonthlySalary').textContent = formatCurrency(salary);
        document.getElementById('statDepartmentStats').textContent = `${whCount} Depo | ${cashierCount} Kasiyer | ${salesCount} Reyon | ${consultantCount} Danışman`;
    } catch (e) {
        console.error('Stats error:', e);
    }
}

// ─── Load Employees List ──────────────────────────────────────────────────────
async function loadData(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '25');

    const searchTerm = document.getElementById('searchInput')?.value || '';
    const department = document.getElementById('departmentFilter')?.value || '';
    const status = document.getElementById('statusFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        department: department,
        status: status
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/Employees/GetEmployees?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 25;

        renderTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} personel`;
    } catch (e) {
        console.error(e);
        showTableError('Personel listesi yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('tableBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="10">
                    <div class="empty-state">
                        <i class="bi bi-people"></i>
                        <h6>Personel bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili filtrelere uygun çalışan kaydı bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const code = p.EmployeeCode || p.employeeCode || '';
        const fullName = p.FullName || p.fullName || `${p.FirstName || p.firstName} ${p.LastName || p.lastName}`;
        const dept = p.Department ?? p.department ?? 1;
        const deptText = p.DepartmentText || p.departmentText || getDepartmentText(dept);
        const title = p.Title || p.title || '';
        const phone = p.Phone || p.phone || '';
        const email = p.Email || p.email || '';
        const whName = p.WarehouseName || p.warehouseName || '';
        const salary = p.Salary ?? p.salary ?? 0;
        const hireDate = p.FormattedHireDate || p.formattedHireDate || formatDate(p.HireDate || p.hireDate);
        const status = p.Status ?? p.status ?? 1;
        const statusText = p.StatusText || p.statusText || getStatusText(status);

        return `
        <tr id="row-emp-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td><strong>${escHtml(fullName)}</strong></td>
            <td>
                <span class="badge ${getDepartmentBadge(dept)}" style="font-size:12px;padding:4px 8px;">
                    ${escHtml(deptText)}
                </span>
            </td>
            <td style="font-size:13px;color:#334155;"><strong>${escHtml(title)}</strong></td>
            <td style="font-size:12.5px;color:#64748b;">
                ${phone ? `<div><i class="bi bi-telephone text-muted me-1"></i>${escHtml(phone)}</div>` : ''}
                ${email ? `<div><i class="bi bi-envelope text-muted me-1"></i>${escHtml(email)}</div>` : ''}
            </td>
            <td style="font-size:12.5px;">${whName ? `<span class="badge bg-light text-dark border"><i class="bi bi-building me-1 text-primary"></i>${escHtml(whName)}</span>` : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="text-align:right;"><strong style="color:#0f172a;font-size:13.5px;">${formatCurrency(salary)}</strong></td>
            <td style="font-size:12.5px;color:#64748b;">${hireDate}</td>
            <td>
                <span class="badge ${getStatusBadge(status)}" style="font-size:12px;">
                    ${escHtml(statusText)}
                </span>
            </td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-edit" onclick="openEditModal(${id})" title="Düzenle" aria-label="Düzenle">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action btn-action-delete" onclick="deleteEmployee(${id}, '${escHtml(fullName)}')" title="Sil" aria-label="Sil">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

function getDepartmentBadge(dept) {
    switch (dept) {
        case 1: return 'bg-primary';            // Depo & Lojistik
        case 2: return 'bg-success';            // Kasa Birimi
        case 3: return 'bg-warning text-dark';  // Reyon & Satış Uzmanı
        case 4: return 'bg-info text-dark';     // Müşteri Danışmanı
        case 5: return 'bg-dark';               // Yönetim
        default: return 'bg-secondary';
    }
}

function getDepartmentText(dept) {
    switch (dept) {
        case 1: return 'Depo & Lojistik';
        case 2: return 'Kasa Birimi';
        case 3: return 'Reyon & Satış';
        case 4: return 'Müşteri Danışmanı';
        case 5: return 'Yönetim & İdari';
        default: return 'Bilinmiyor';
    }
}

function getStatusBadge(status) {
    switch (status) {
        case 1: return 'bg-success-subtle text-success';   // Aktif
        case 2: return 'bg-warning-subtle text-warning';   // İzinli
        case 3: return 'bg-secondary-subtle text-secondary'; // Ayrıldı
        default: return 'bg-dark';
    }
}

function getStatusText(status) {
    switch (status) {
        case 1: return 'Aktif';
        case 2: return 'İzinli';
        case 3: return 'Ayrıldı';
        default: return 'Bilinmiyor';
    }
}

// ─── Dropdowns & Modal ────────────────────────────────────────────────────────
async function loadWarehousesDropdown() {
    try {
        const res = await fetch('/Employees/GetWarehouses');
        if (!res.ok) return;
        warehouses = await res.json();
        const select = document.getElementById('warehouseId');
        select.innerHTML = '<option value="">-- Depo Yok / Bağımsız --</option>';
        warehouses.forEach(w => {
            const id = w.Id ?? w.id;
            const name = w.Name || w.name;
            const code = w.WarehouseCode || w.warehouseCode;
            select.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)})</option>`;
        });
    } catch (e) {
        console.error('Warehouse dropdown error:', e);
    }
}

function openCreateModal() {
    document.getElementById('employeeForm').reset();
    document.getElementById('employeeId').value = '';
    const randomNum = Math.floor(100 + Math.random() * 900);
    document.getElementById('employeeCode').value = `PRS-MMP-${randomNum}`;
    document.getElementById('city').value = 'İstanbul';
    document.getElementById('hireDate').value = new Date().toISOString().slice(0, 10);

    document.getElementById('employeeModalLabel').innerHTML = '<i class="bi bi-person-plus me-2"></i>Yeni Personel Ekle';
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('employeeModal'));
    modal.show();
}

async function openEditModal(id) {
    try {
        const item = await efbisAjax.get(`/Employees/GetDetail/${id}`);

        document.getElementById('employeeId').value = item.Id ?? item.id;
        document.getElementById('employeeCode').value = item.EmployeeCode || item.employeeCode || '';
        document.getElementById('firstName').value = item.FirstName || item.firstName || '';
        document.getElementById('lastName').value = item.LastName || item.lastName || '';
        document.getElementById('tckn').value = item.TCKN || item.tckn || '';
        document.getElementById('department').value = (item.Department ?? item.department ?? 1).toString();
        document.getElementById('title').value = item.Title || item.title || '';
        document.getElementById('phone').value = item.Phone || item.phone || '';
        document.getElementById('email').value = item.Email || item.email || '';
        document.getElementById('city').value = item.City || item.city || 'İstanbul';
        document.getElementById('salary').value = item.Salary ?? item.salary ?? 0;
        document.getElementById('status').value = (item.Status ?? item.status ?? 1).toString();
        document.getElementById('warehouseId').value = (item.WarehouseId ?? item.warehouseId ?? '').toString();

        if (item.HireDate || item.hireDate) {
            document.getElementById('hireDate').value = new Date(item.HireDate || item.hireDate).toISOString().slice(0, 10);
        }

        document.getElementById('employeeModalLabel').innerHTML = '<i class="bi bi-pencil-square me-2"></i>Personel Bilgilerini Düzenle';
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('employeeModal'));
        modal.show();
    } catch (e) {
        showToast('Personel bilgileri yüklenemedi.', 'error');
    }
}

async function saveEmployee() {
    const idVal = document.getElementById('employeeId').value.trim();
    const code = document.getElementById('employeeCode').value.trim();
    const firstName = document.getElementById('firstName').value.trim();
    const lastName = document.getElementById('lastName').value.trim();
    const tckn = document.getElementById('tckn').value.trim();
    const department = parseInt(document.getElementById('department').value) || 1;
    const title = document.getElementById('title').value.trim();
    const phone = document.getElementById('phone').value.trim();
    const email = document.getElementById('email').value.trim();
    const city = document.getElementById('city').value.trim();
    const salary = parseFloat(document.getElementById('salary').value) || 0;
    const hireDateVal = document.getElementById('hireDate').value;
    const status = parseInt(document.getElementById('status').value) || 1;
    const warehouseIdVal = document.getElementById('warehouseId').value;

    if (!code || !firstName || !lastName || !title) {
        showToast('Lütfen zorunlu tüm alanları doldurunuz (Kod, Ad, Soyad, Unvan).', 'warning');
        return;
    }

    const dto = {
        EmployeeCode: code,
        FirstName: firstName,
        LastName: lastName,
        TCKN: tckn || null,
        Department: department,
        Title: title,
        Phone: phone || null,
        Email: email || null,
        City: city || 'İstanbul',
        Salary: salary,
        HireDate: hireDateVal ? new Date(hireDateVal).toISOString() : new Date().toISOString(),
        Status: status,
        WarehouseId: warehouseIdVal ? parseInt(warehouseIdVal) : null
    };

    const isUpdate = idVal !== '' && !isNaN(parseInt(idVal));
    if (isUpdate) dto.Id = parseInt(idVal);

    const url = isUpdate ? `/Employees/Update/${idVal}` : '/Employees/Create';

    try {
        const result = await efbisAjax.post(url, dto);
        if (result.success || result.Id || result.id) {
            showToast(result.message || 'Personel kaydı başarıyla oluşturuldu.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('employeeModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message || 'Personel kaydı oluşturulamadı.', 'error');
        }
    } catch (e) {
        console.error(e);
        showToast(e.message || 'Personel kaydedilirken bir hata oluştu.', 'error');
    }
}

async function deleteEmployee(id, name) {
    if (!confirm(`"${name}" isimli personelin kaydını silmek istediğinize emin misiniz?`)) return;

    try {
        const result = await efbisAjax.post(`/Employees/Delete/${id}`, {});
        if (result.success) {
            showToast(result.message || 'Personel kaydı silindi.', 'success');
            refreshAll();
        } else {
            showToast(result.message || 'Personel kaydı silinemedi.', 'error');
        }
    } catch (e) {
        showToast('Personel silinirken bir hata oluştu.', 'error');
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

function formatDate(dateStr) {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
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
