// ─── Veritabanı Sunucusu & Server Explorer JS Module ──────────────────────────

'use strict';

let currentTable = 'Users';
let currentPage = 1;
let pageSize = 15;
let allTableSummaries = [];

$(document).ready(function () {
    init();
});

async function init() {
    $('#btnRefreshDb').click(refreshAll);
    $('#btnRunSeed').click(runSeed);
    $('#dbSearchInput').on('input', debounceSearch);

    await loadServerStats();
    await loadTableSummary();
    await loadTableData(currentTable, 1);
}

async function refreshAll() {
    await loadServerStats();
    await loadTableSummary();
    await loadTableData(currentTable, 1);
    showToast('Veritabanı sunucusu verileri güncellendi.', 'success');
}

async function loadServerStats() {
    try {
        const res = await efbisAjax.get('/Database/GetServerStats');
        if (res && res.success && res.data) {
            const d = res.data;
            $('#dbStatus').text(d.status);
            $('#dbProvider').text(d.providerName + ' (' + d.databaseName + ')');
            $('#dbTableCount').text(d.tableCount + ' Tablo');
            $('#dbTotalRecords').text((d.totalRecords || 0).toLocaleString('tr-TR') + ' Satır');
        }
    } catch (e) {
        console.error('loadServerStats error:', e);
    }
}

async function loadTableSummary() {
    try {
        const res = await efbisAjax.get('/Database/GetTableSummary');
        if (res && res.success && res.data) {
            allTableSummaries = res.data;
            renderTablesListGroup(res.data);
        }
    } catch (e) {
        console.error('loadTableSummary error:', e);
    }
}

function renderTablesListGroup(tables) {
    const container = $('#tablesListGroup');
    container.empty();

    tables.forEach(t => {
        const isSelected = t.name === currentTable;
        const activeClass = isSelected ? 'active text-white' : 'text-dark';
        const badgeClass = isSelected ? 'bg-light text-primary fw-bold' : 'bg-primary';

        container.append(`
            <button type="button" class="list-group-item list-group-item-action d-flex justify-content-between align-items-center py-2.5 px-3 ${activeClass}" onclick="switchTable('${t.name}')">
                <div>
                    <div class="fw-bold" style="font-size:13px;">${escHtml(t.name)}</div>
                    <div style="font-size:11px;opacity:0.8;">${escHtml(t.category)} • ${t.columnCount} Sütun</div>
                </div>
                <span class="badge ${badgeClass} rounded-pill">${t.recordCount}</span>
            </button>
        `);
    });
}

async function switchTable(tableName) {
    currentTable = tableName;
    renderTablesListGroup(allTableSummaries);
    await loadTableData(tableName, 1);
}

async function loadTableData(tableName, page = 1) {
    currentPage = page;

    const summary = allTableSummaries.find(t => t.name === tableName);
    $('#currentTableName').text(summary ? summary.displayName : tableName);

    const tbody = $('#dbTableBody');
    const thead = $('#dbTableHeader');

    tbody.html('<tr><td colspan="15" class="text-center py-5"><div class="spinner-efbis mx-auto mb-2"></div>Veriler getiriliyor...</td></tr>');

    try {
        const res = await efbisAjax.get(`/Database/GetTableData?tableName=${tableName}&page=${page}&pageSize=${pageSize}`);
        if (res && res.success && res.data) {
            const data = res.data;
            const items = data.items || [];
            const totalCount = data.totalCount || 0;

            $('#currentTableBadge').text(`${totalCount} kayıt`);

            if (items.length === 0) {
                thead.empty();
                tbody.html('<tr><td colspan="15" class="text-center py-4 text-muted">Bu tabloda gösterilecek kayıt bulunamadı.</td></tr>');
                $('#dbPagination').empty();
                return;
            }

            renderDynamicTable(items);
            renderDbPagination(data);
        }
    } catch (e) {
        console.error('loadTableData error:', e);
        tbody.html('<tr><td colspan="15" class="text-center text-danger py-4">Veriler yüklenirken hata oluştu.</td></tr>');
    }
}

function renderDynamicTable(items) {
    const thead = $('#dbTableHeader');
    const tbody = $('#dbTableBody');

    const firstItem = items[0];
    const columns = Object.keys(firstItem);

    // Build Header
    let headerHtml = '<tr>';
    columns.forEach(col => {
        headerHtml += `<th style="font-size:12px;text-transform:uppercase;">${escHtml(col)}</th>`;
    });
    headerHtml += '</tr>';
    thead.html(headerHtml);

    // Filter local rows if search input has value
    const searchTerm = $('#dbSearchInput').val()?.toLowerCase() || '';
    let filteredItems = items;
    if (searchTerm) {
        filteredItems = items.filter(item => {
            return Object.values(item).some(val => val != null && String(val).toLowerCase().includes(searchTerm));
        });
    }

    if (filteredItems.length === 0) {
        tbody.html(`<tr><td colspan="${columns.length}" class="text-center py-4 text-muted">Arama kriterinize uygun satır bulunamadı.</td></tr>`);
        return;
    }

    // Build Rows
    let bodyHtml = '';
    filteredItems.forEach(item => {
        bodyHtml += '<tr>';
        columns.forEach(col => {
            const val = item[col];
            bodyHtml += `<td>${formatCellValue(col, val)}</td>`;
        });
        bodyHtml += '</tr>';
    });

    tbody.html(bodyHtml);
}

function formatCellValue(col, val) {
    if (val == null) return '<span style="color:#cbd5e1;">NULL</span>';

    if (typeof val === 'boolean') {
        return val
            ? '<span class="badge bg-success-subtle text-success" style="font-size:11px;">True</span>'
            : '<span class="badge bg-secondary-subtle text-secondary" style="font-size:11px;">False</span>';
    }

    if (typeof val === 'number') {
        if (col.toLowerCase().includes('price') || col.toLowerCase().includes('total') || col.toLowerCase().includes('balance') || col.toLowerCase().includes('salary') || col.toLowerCase().includes('amount')) {
            return `<strong style="color:#0f172a;">${val.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ₺</strong>`;
        }
        return val.toLocaleString('tr-TR');
    }

    const strVal = String(val);
    if (strVal.includes('T00:00:00') || (strVal.length >= 10 && !isNaN(Date.parse(strVal)) && strVal.includes('-'))) {
        const d = new Date(strVal);
        if (!isNaN(d.getTime())) {
            return `<span style="font-size:12px;color:#64748b;">${d.toLocaleDateString('tr-TR')}</span>`;
        }
    }

    if (col.toLowerCase() === 'status' || col.toLowerCase() === 'role' || col.toLowerCase() === 'type') {
        return `<span class="badge bg-light text-dark border" style="font-size:11.5px;">${escHtml(strVal)}</span>`;
    }

    return `<span style="font-size:12.5px;">${escHtml(strVal)}</span>`;
}

function renderDbPagination(data) {
    const container = $('#dbPagination');
    const totalPages = data.totalPages || 1;
    const totalCount = data.totalCount || 0;

    const from = totalCount === 0 ? 0 : ((currentPage - 1) * pageSize + 1);
    const to = Math.min(currentPage * pageSize, totalCount);

    let html = `
        <div class="text-muted small">
            ${totalCount === 0 ? 'Kayıt bulunamadı' : `${from}–${to} / ${totalCount} satır gösteriliyor`}
        </div>
        <ul class="pagination efbis-pagination mb-0">
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadTableData('${currentTable}', ${currentPage - 1})"><i class="bi bi-chevron-left"></i></a>
            </li>
    `;

    for (let i = 1; i <= totalPages; i++) {
        if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
            html += `
                <li class="page-item ${i === currentPage ? 'active' : ''}">
                    <a class="page-link page-btn" href="javascript:void(0)" onclick="loadTableData('${currentTable}', ${i})">${i}</a>
                </li>
            `;
        } else if (i === currentPage - 2 || i === currentPage + 2) {
            html += `<li class="page-item disabled"><span class="page-link border-0">...</span></li>`;
        }
    }

    html += `
            <li class="page-item ${currentPage === totalPages || totalPages <= 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadTableData('${currentTable}', ${currentPage + 1})"><i class="bi bi-chevron-right"></i></a>
            </li>
        </ul>
    `;

    container.html(html);
}

async function runSeed() {
    if (!confirm('Veritabanı eksik verileri yeniden kontrol edilecek ve varsayılan kayıtlar güncellenecek. Onaylıyor musunuz?')) return;

    try {
        const res = await efbisAjax.post('/Database/RunSeed', {});
        if (res && res.success) {
            showToast(res.message || 'Seed işlemi tamamlandı.', 'success');
            refreshAll();
        } else {
            showToast((res && res.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('Seed işlemi sırasında hata meydana geldi.', 'error');
    }
}

let searchTimer;
function debounceSearch() {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(() => {
        loadTableData(currentTable, 1);
    }, 300);
}

function escHtml(unsafe) {
    if (!unsafe) return '';
    return String(unsafe)
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}
