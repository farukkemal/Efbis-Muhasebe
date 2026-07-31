// ─── Gelir & Gider Yönetimi JavaScript Module ─────────────────────────────
// AJAX CRUD, Filtreleme, Sayfalama, Modal, Export & Print

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
    loadRecords();
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const response = await fetch('/IncomeExpenses/GetDashboard');
        if (!response.ok) return;
        const data = await response.json();

        const totalInc = data.TotalIncome ?? data.totalIncome ?? 0;
        const totalExp = data.TotalExpense ?? data.totalExpense ?? 0;
        const netProf = data.NetProfit ?? data.netProfit ?? 0;
        const txCount = data.TransactionCount ?? data.transactionCount ?? 0;

        document.getElementById('totalIncome').textContent = formatCurrency(totalInc);
        document.getElementById('totalExpense').textContent = formatCurrency(totalExp);
        
        const netProfitEl = document.getElementById('netProfit');
        netProfitEl.textContent = formatCurrency(netProf);

        if (netProf < 0) {
            netProfitEl.className = 'stat-value text-danger';
        } else {
            netProfitEl.className = 'stat-value text-success';
        }

        document.getElementById('transactionCount').textContent = txCount;
    } catch (e) {
        console.error('Dashboard yüklenemedi:', e);
    }
}

// ─── Load Records ─────────────────────────────────────────────────────────────
async function loadRecords(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: document.getElementById('searchInput')?.value || '',
        type: document.getElementById('typeFilter')?.value || '',
        categoryName: document.getElementById('categoryFilter')?.value || ''
    });

    setTableLoading(true);

    try {
        const response = await fetch(`/IncomeExpenses/GetRecords?${params}`);
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
        showTableError('Gelir ve gider kayıtları yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('recordsBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="9">
                    <div class="empty-state">
                        <i class="bi bi-receipt-cutoff"></i>
                        <h6>Kayıt bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili kriterlere uygun gelir veya gider kaydı bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const code = p.TransactionCode || p.transactionCode || '';
        const type = p.Type ?? p.type ?? 1;
        const categoryName = p.CategoryName || p.categoryName || '';
        const amount = p.Amount ?? p.amount ?? 0;
        const accountName = p.AccountName || p.accountName || '';
        const customerTitle = p.CustomerTitle || p.customerTitle || '';
        const date = p.TransactionDate || p.transactionDate;
        const desc = p.Description || p.description || '';

        return `
        <tr id="row-rec-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td>
                <span class="badge ${type === 1 ? 'bg-success' : 'bg-danger'}" style="font-size:12px;padding:4px 8px;">
                    ${type === 1 ? '🟢 Gelir' : '🔴 Gider'}
                </span>
            </td>
            <td><strong>${escHtml(categoryName)}</strong></td>
            <td>
                <strong style="font-size:14px;color:${type === 1 ? '#059669' : '#dc2626'};">
                    ${type === 1 ? '+' : '-'}${formatCurrency(amount)}
                </strong>
            </td>
            <td>${accountName ? escHtml(accountName) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>${customerTitle ? escHtml(customerTitle) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="font-size:13px;color:#64748b;">${formatDate(date)}</td>
            <td style="font-size:13px;color:#64748b;">${desc ? escHtml(desc) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-delete" title="Kaydı Sil" onclick="deleteRecord(${id}, '${escHtml(code)}')" aria-label="Sil">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

// ─── Modal Actions ────────────────────────────────────────────────────────────
async function showCreateModal() {
    try {
        // Load Cash/Bank Accounts
        const accRes = await fetch('/CashAccounts/GetActive');
        if (accRes.ok) {
            const accounts = await accRes.json();
            const accSelect = document.getElementById('cashAccountSelect');
            accSelect.innerHTML = '<option value="">-- Kasa/Banka Seçilmeyebilir --</option>';
            accounts.forEach(acc => {
                const accId = acc.Id ?? acc.id;
                const accName = acc.AccountName || acc.accountName;
                const bal = acc.Balance ?? acc.balance ?? 0;
                accSelect.innerHTML += `<option value="${accId}">${escHtml(accName)} (${formatCurrency(bal)})</option>`;
            });
        }

        // Load Customers
        const custRes = await fetch('/IncomeExpenses/GetCustomers');
        if (custRes.ok) {
            const customers = await custRes.json();
            const custSelect = document.getElementById('customerSelect');
            custSelect.innerHTML = '<option value="">-- Cari Seçilmeyebilir --</option>';
            customers.forEach(c => {
                const cId = c.Id ?? c.id;
                const title = c.Title || c.title;
                const code = c.CustomerCode || c.customerCode;
                custSelect.innerHTML += `<option value="${cId}">${escHtml(title)} (${escHtml(code)})</option>`;
            });
        }

        document.getElementById('createForm').reset();
        document.getElementById('recTransactionDate').value = new Date().toISOString().slice(0, 16);

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createModal'));
        modal.show();
    } catch (e) {
        showToast('Form verileri yüklenirken hata oluştu.', 'error');
    }
}

async function saveRecord() {
    const type = parseInt(document.getElementById('recType').value);
    const categoryName = document.getElementById('recCategoryName').value.trim();
    const cashAccountId = parseInt(document.getElementById('cashAccountSelect').value) || null;
    const customerId = parseInt(document.getElementById('customerSelect').value) || null;
    const amount = parseFloat(document.getElementById('recAmount').value) || 0;
    const dateVal = document.getElementById('recTransactionDate').value;
    const description = document.getElementById('recDescription').value.trim();

    if (!categoryName || amount <= 0) {
        showToast('Lütfen kategori seçin ve 0\'dan büyük bir tutar girin.', 'warning');
        return;
    }

    const dto = {
        Type: type,
        CategoryName: categoryName,
        CashAccountId: cashAccountId,
        CustomerId: customerId,
        Amount: amount,
        TransactionDate: dateVal ? new Date(dateVal).toISOString() : new Date().toISOString(),
        Description: description || null
    };

    try {
        const result = await efbisAjax.post('/IncomeExpenses/Create', dto);
        if (result.success) {
            showToast(result.message, 'success');
            bootstrap.Modal.getInstance(document.getElementById('createModal'))?.hide();
            refreshAll();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Gelir/Gider kaydı kaydedilirken hata oluştu.', 'error');
    }
}

async function deleteRecord(id, code) {
    if (!confirm(`"${code}" kodlu işlemi silmek istediğinize emin misiniz?`)) return;

    try {
        const result = await efbisAjax.post(`/IncomeExpenses/Delete/${id}`, {});
        if (result.success) {
            showToast(result.message, 'success');
            refreshAll();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Kayıt silinirken hata oluştu.', 'error');
    }
}

// ─── Helpers ───────────────────────────────────────────────────────────────────
function debounceSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => loadRecords(1), 400);
}

function setTableLoading(loading) {
    const wrapper = document.getElementById('recordsTableWrapper');
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
    document.getElementById('recordsBody').innerHTML = `
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

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadRecords(${pageNumber - 1})" aria-label="Önceki"><i class="bi bi-chevron-left"></i></button>`;

    const startPage = Math.max(1, pageNumber - 2);
    const endPage = Math.min(totalPages, pageNumber + 2);

    if (startPage > 1) {
        html += `<button class="page-btn" onclick="loadRecords(1)">1</button>`;
        if (startPage > 2) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === pageNumber ? 'active' : ''}" onclick="loadRecords(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
        html += `<button class="page-btn" onclick="loadRecords(${totalPages})">${totalPages}</button>`;
    }

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadRecords(${pageNumber + 1})" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></button>`;

    buttons.innerHTML = html;
}
