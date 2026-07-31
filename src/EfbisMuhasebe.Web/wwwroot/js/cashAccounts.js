// ─── Cash & Bank Accounts JavaScript Module ──────────────────────────────────
// AJAX CRUD, Tab Switching, Dashboard, Virman/Tahsilat/Tediye, Export & Print

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentTab = 'accounts'; // 'accounts' or 'transactions'
let currentPage = 1;
let pageSize = 10;
let searchTimeout = null;

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    refreshAll();

    const accountsTabEl = document.getElementById('accounts-tab');
    const transactionsTabEl = document.getElementById('transactions-tab');

    if (accountsTabEl) {
        accountsTabEl.addEventListener('shown.bs.tab', function () {
            currentTab = 'accounts';
            currentPage = 1;
            document.getElementById('typeFilterCol')?.classList.remove('d-none');
            document.getElementById('txTypeFilterCol')?.classList.add('d-none');
            loadAccounts();
        });
    }

    if (transactionsTabEl) {
        transactionsTabEl.addEventListener('shown.bs.tab', function () {
            currentTab = 'transactions';
            currentPage = 1;
            document.getElementById('typeFilterCol')?.classList.add('d-none');
            document.getElementById('txTypeFilterCol')?.classList.remove('d-none');
            loadTransactions();
        });
    }
});

function refreshAll() {
    loadDashboard();
    if (currentTab === 'accounts') {
        loadAccounts();
    } else {
        loadTransactions();
    }
}

function refreshCurrentTab() {
    if (currentTab === 'accounts') loadAccounts(1);
    else loadTransactions(1);
}

// ─── Dashboard Stats ──────────────────────────────────────────────────────────
async function loadDashboard() {
    try {
        const response = await fetch('/CashAccounts/GetDashboard');
        if (!response.ok) return;
        const stats = await response.json();

        const cashBal = stats.TotalCashBalance ?? stats.totalCashBalance ?? 0;
        const bankBal = stats.TotalBankBalance ?? stats.totalBankBalance ?? 0;
        const posBal = stats.TotalPosBalance ?? stats.totalPosBalance ?? 0;
        const todayColl = stats.TodayCollections ?? stats.todayCollections ?? 0;
        const todayPay = stats.TodayPayments ?? stats.todayPayments ?? 0;

        if (document.getElementById('totalCashBalance')) document.getElementById('totalCashBalance').textContent = formatCurrency(cashBal);
        if (document.getElementById('totalBankBalance')) document.getElementById('totalBankBalance').textContent = formatCurrency(bankBal);
        if (document.getElementById('totalPosBalance')) document.getElementById('totalPosBalance').textContent = formatCurrency(posBal);
        if (document.getElementById('todayCollections')) document.getElementById('todayCollections').textContent = formatCurrency(todayColl);
        if (document.getElementById('todayPayments')) document.getElementById('todayPayments').textContent = formatCurrency(todayPay);
    } catch (e) {
        console.error('Dashboard istatistikleri yüklenemedi:', e);
    }
}

// ─── Accounts List ────────────────────────────────────────────────────────────
async function loadAccounts(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');
    const searchTerm = document.getElementById('searchInput')?.value || '';
    const type = document.getElementById('accountTypeFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        type: type
    });

    setTableLoading('accountsTableWrapper', true);

    try {
        const response = await fetch(`/CashAccounts/GetAccounts?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderAccountsTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} hesap`;
    } catch (e) {
        console.error(e);
        showTableError('accountsBody', 8, 'Kasa ve banka hesapları yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading('accountsTableWrapper', false);
    }
}

function renderAccountsTable(items) {
    const tbody = document.getElementById('accountsBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8">
                    <div class="empty-state">
                        <i class="bi bi-wallet2"></i>
                        <h6>Hesap bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Arama kriterlerinize uygun hesap bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const id = p.Id ?? p.id;
        const code = p.AccountCode || p.accountCode || '';
        const name = p.AccountName || p.accountName || '';
        const type = p.AccountType ?? p.accountType ?? 1;
        const bankName = p.BankName || p.bankName || '';
        const iban = p.Iban || p.iban || '';
        const balance = p.Balance ?? p.balance ?? 0;
        const status = p.Status ?? p.status ?? 1;

        let typeBadgeHtml = '';
        if (type === 1) {
            typeBadgeHtml = '<span class="badge bg-success fw-normal" style="font-size:12px;padding:4px 8px;">💵 Nakit Kasa</span>';
        } else if (type === 2) {
            typeBadgeHtml = '<span class="badge bg-primary fw-normal" style="font-size:12px;padding:4px 8px;">🏦 Banka Hesabı</span>';
        } else if (type === 3) {
            typeBadgeHtml = '<span class="badge bg-info text-dark fw-normal" style="font-size:12px;padding:4px 8px;">💳 POS Kasası</span>';
        } else if (type === 4) {
            typeBadgeHtml = '<span class="badge text-white fw-normal" style="background:#8b5cf6;font-size:12px;padding:4px 8px;">📱 Sanal POS / Kredi Kartı</span>';
        } else {
            typeBadgeHtml = '<span class="badge bg-secondary fw-normal" style="font-size:12px;padding:4px 8px;">Hesap</span>';
        }

        return `
        <tr id="row-acc-${id}">
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td><strong>${escHtml(name)}</strong></td>
            <td>${typeBadgeHtml}</td>
            <td>${bankName ? escHtml(bankName) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="font-size:12px;color:#64748b;">${iban ? escHtml(iban) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>
                <strong style="font-size:14px;color:${balance >= 0 ? '#059669' : '#dc2626'};">
                    ${formatCurrency(balance)}
                </strong>
            </td>
            <td>
                <span class="badge ${status === 1 ? 'bg-success-subtle text-success' : 'bg-secondary-subtle text-secondary'}" style="font-size:12px;">
                    ${status === 1 ? 'Aktif' : 'Pasif'}
                </span>
            </td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-delete" title="Hesabı Sil" onclick="deleteAccount(${id}, '${escHtml(name)}')" aria-label="Sil">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

// ─── Transactions List ────────────────────────────────────────────────────────
async function loadTransactions(page = null) {
    if (page !== null) currentPage = page;
    pageSize = parseInt(document.getElementById('pageSizeSelect')?.value || '10');
    const searchTerm = document.getElementById('searchInput')?.value || '';
    const txType = document.getElementById('txTypeFilter')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: searchTerm,
        transactionType: txType
    });

    setTableLoading('transactionsTableWrapper', true);

    try {
        const response = await fetch(`/CashAccounts/GetTransactions?${params}`);
        if (!response.ok) throw new Error('Sunucu hatası');
        const data = await response.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? 1;
        const pgSize = data.PageSize ?? data.pageSize ?? 10;

        renderTransactionsTable(items);
        renderPagination(totalCount, pageNumber, pgSize);
        document.getElementById('totalBadge').textContent = `${totalCount} işlem`;
    } catch (e) {
        console.error(e);
        showTableError('transactionsBody', 7, 'Hareketler yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading('transactionsTableWrapper', false);
    }
}

function renderTransactionsTable(items) {
    const tbody = document.getElementById('transactionsBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="7">
                    <div class="empty-state">
                        <i class="bi bi-arrow-left-right"></i>
                        <h6>İşlem bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili kriterlere uygun işlem hareketi bulunamadı.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(p => {
        const code = p.TransactionCode || p.transactionCode || '';
        const txType = p.TransactionType ?? p.transactionType ?? 1;
        const typeText = p.TypeText || p.typeText || (txType === 1 ? 'Tahsilat' : txType === 2 ? 'Tediye' : 'Virman');
        const accName = p.AccountName || p.accountName || '';
        const custTitle = p.CustomerTitle || p.customerTitle || '';
        const amount = p.Amount ?? p.amount ?? 0;
        const date = p.TransactionDate || p.transactionDate;
        const desc = p.Description || p.description || '';

        let badgeClass = 'bg-success';
        if (txType === 2) badgeClass = 'bg-danger';
        else if (txType === 3) badgeClass = 'bg-info text-dark';

        return `
        <tr>
            <td><code style="background:#f1f5f9;padding:2px 7px;border-radius:5px;font-size:12px;">${escHtml(code)}</code></td>
            <td><span class="badge ${badgeClass}" style="font-size:12px;padding:4px 8px;">${escHtml(typeText)}</span></td>
            <td><strong>${escHtml(accName)}</strong></td>
            <td>${custTitle ? escHtml(custTitle) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>
                <strong style="font-size:14px;color:${txType === 1 ? '#059669' : txType === 2 ? '#dc2626' : '#2563eb'};">
                    ${txType === 1 ? '+' : txType === 2 ? '-' : ''}${formatCurrency(amount)}
                </strong>
            </td>
            <td style="font-size:13px;color:#64748b;">${formatDate(date)}</td>
            <td style="font-size:13px;color:#64748b;">${desc ? escHtml(desc) : '<span style="color:#cbd5e1;">—</span>'}</td>
        </tr>`;
    }).join('');
}

// ─── Modal Actions ────────────────────────────────────────────────────────────
function showCreateAccountModal() {
    document.getElementById('createAccountForm').reset();
    document.getElementById('accAccountType').value = '1';
    toggleBankFields();

    // Auto-code generation hint
    document.getElementById('accAccountCode').value = `KSA-${Math.floor(100 + Math.random() * 900)}`;

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createAccountModal'));
    modal.show();
}

function toggleBankFields() {
    const type = document.getElementById('accAccountType').value;
    const bankFields = document.getElementById('bankFields');
    const codeInput = document.getElementById('accAccountCode');

    if (type === '2') {
        bankFields.classList.remove('d-none');
        if (codeInput.value.startsWith('KSA-')) {
            codeInput.value = `BNK-${Math.floor(100 + Math.random() * 900)}`;
        }
    } else {
        bankFields.classList.add('d-none');
        if (codeInput.value.startsWith('BNK-')) {
            codeInput.value = `KSA-${Math.floor(100 + Math.random() * 900)}`;
        }
    }
}

async function saveAccount() {
    const code = document.getElementById('accAccountCode').value.trim();
    const name = document.getElementById('accAccountName').value.trim();
    const type = parseInt(document.getElementById('accAccountType').value);
    const bankName = document.getElementById('accBankName').value.trim();
    const iban = document.getElementById('accIban').value.trim();
    const initialBalance = parseFloat(document.getElementById('accInitialBalance').value) || 0;
    const description = document.getElementById('accDescription').value.trim();

    if (!code || !name) {
        showToast('Hesap kodu ve hesap adı zorunludur.', 'warning');
        return;
    }

    const dto = {
        AccountCode: code,
        AccountName: name,
        AccountType: type,
        BankName: type === 2 ? bankName : null,
        Iban: type === 2 ? iban : null,
        InitialBalance: initialBalance,
        Currency: 'TRY',
        Description: description || null
    };

    try {
        const result = await efbisAjax.post('/CashAccounts/Create', dto);
        if (result.success) {
            showToast(result.message, 'success');
            bootstrap.Modal.getInstance(document.getElementById('createAccountModal'))?.hide();
            loadDashboard();
            loadAccounts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Hesap kaydedilirken hata oluştu.', 'error');
    }
}

async function showCreateTransactionModal() {
    try {
        // Load Active Accounts
        const accRes = await fetch('/CashAccounts/GetActive');
        const accounts = await accRes.json();

        const srcSelect = document.getElementById('txCashAccountId');
        const targetSelect = document.getElementById('txTargetAccountId');

        srcSelect.innerHTML = '<option value="">-- Hesap Seçiniz --</option>';
        targetSelect.innerHTML = '<option value="">-- Hedef Hesap Seçiniz --</option>';

        accounts.forEach(acc => {
            const accId = acc.Id ?? acc.id;
            const accName = acc.AccountName || acc.accountName;
            const bal = acc.Balance ?? acc.balance ?? 0;
            const optionText = `${accName} (${formatCurrency(bal)})`;
            srcSelect.innerHTML += `<option value="${accId}">${escHtml(optionText)}</option>`;
            targetSelect.innerHTML += `<option value="${accId}">${escHtml(optionText)}</option>`;
        });

        // Load Customers
        const custRes = await fetch('/CashAccounts/GetCustomers');
        const customers = await custRes.json();

        const custSelect = document.getElementById('txCustomerId');
        custSelect.innerHTML = '<option value="">-- Cari Seçilmeyebilir --</option>';
        customers.forEach(c => {
            const cId = c.Id ?? c.id;
            const title = c.Title || c.title;
            const code = c.CustomerCode || c.customerCode;
            custSelect.innerHTML += `<option value="${cId}">${escHtml(title)} (${escHtml(code)})</option>`;
        });

        document.getElementById('createTransactionForm').reset();
        document.getElementById('txTransactionDate').value = new Date().toISOString().slice(0, 16);
        onTransactionTypeChange();

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createTransactionModal'));
        modal.show();
    } catch (e) {
        showToast('Form verileri yüklenirken hata oluştu.', 'error');
    }
}

function onTransactionTypeChange() {
    const type = document.getElementById('txTransactionType').value;
    const targetGroup = document.getElementById('targetAccountGroup');
    const customerGroup = document.getElementById('customerGroup');
    const lblCashAccount = document.getElementById('lblCashAccount');

    if (type === '3') { // Virman / Transfer
        targetGroup.classList.remove('d-none');
        customerGroup.classList.add('d-none');
        lblCashAccount.innerHTML = 'Kaynak Hesap (Para Çıkacak) <span class="required">*</span>';
    } else {
        targetGroup.classList.add('d-none');
        customerGroup.classList.remove('d-none');
        lblCashAccount.innerHTML = 'Kasa / Banka Hesabı <span class="required">*</span>';
    }
}

async function saveTransaction() {
    const txType = parseInt(document.getElementById('txTransactionType').value);
    const cashAccountId = parseInt(document.getElementById('txCashAccountId').value);
    const targetAccountId = parseInt(document.getElementById('txTargetAccountId').value) || null;
    const customerId = parseInt(document.getElementById('txCustomerId').value) || null;
    const amount = parseFloat(document.getElementById('txAmount').value) || 0;
    const dateVal = document.getElementById('txTransactionDate').value;
    const description = document.getElementById('txDescription').value.trim();

    if (!cashAccountId || amount <= 0) {
        showToast('Lütfen geçerli bir hesap ve 0\'dan büyük tutar giriniz.', 'warning');
        return;
    }

    if (txType === 3 && !targetAccountId) {
        showToast('Virman işlemi için hedef hesap seçimi zorunludur.', 'warning');
        return;
    }

    if (txType === 3 && cashAccountId === targetAccountId) {
        showToast('Kaynak hesap ile hedef hesap aynı olamaz.', 'warning');
        return;
    }

    const dto = {
        TransactionType: txType,
        CashAccountId: cashAccountId,
        TargetAccountId: txType === 3 ? targetAccountId : null,
        CustomerId: txType !== 3 ? customerId : null,
        Amount: amount,
        TransactionDate: dateVal ? new Date(dateVal).toISOString() : new Date().toISOString(),
        Description: description || null
    };

    try {
        const result = await efbisAjax.post('/CashAccounts/CreateTransaction', dto);
        if (result.success) {
            showToast(result.message, 'success');
            bootstrap.Modal.getInstance(document.getElementById('createTransactionModal'))?.hide();
            loadDashboard();
            refreshCurrentTab();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('İşlem kaydedilirken hata oluştu.', 'error');
    }
}

async function deleteAccount(id, name) {
    if (!confirm(`"${name}" isimli hesabı silmek istediğinize emin misiniz?\n\nNot: Bakiyesi 0 olmayan hesaplar silinemez.`)) return;

    try {
        const result = await efbisAjax.post(`/CashAccounts/Delete/${id}`, {});
        if (result.success) {
            showToast(result.message, 'success');
            loadDashboard();
            loadAccounts();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Hesap silinirken hata oluştu.', 'error');
    }
}

// ─── Export & Print ────────────────────────────────────────────────────────────
function exportCurrentTabToCSV() {
    if (currentTab === 'accounts') {
        exportTableToCSV('accountsTable', 'kasa_banka_hesaplari.csv');
    } else {
        exportTableToCSV('transactionsTable', 'kasa_banka_hareketleri.csv');
    }
}

function printCurrentTab() {
    if (currentTab === 'accounts') {
        printTable('accountsTable', 'Kasa & Banka Hesapları Listesi');
    } else {
        printTable('transactionsTable', 'Kasa & Banka Hareketleri Listesi');
    }
}

// ─── Filters Reset & Search ────────────────────────────────────────────────────
function debounceSearch() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => refreshCurrentTab(), 400);
}

// ─── Helpers ───────────────────────────────────────────────────────────────────
function setTableLoading(wrapperId, loading) {
    const wrapper = document.getElementById(wrapperId);
    if (!wrapper) return;
    if (loading) {
        wrapper.style.opacity = '0.6';
        wrapper.style.pointerEvents = 'none';
    } else {
        wrapper.style.opacity = '1';
        wrapper.style.pointerEvents = '';
    }
}

function showTableError(tbodyId, colspan, msg) {
    document.getElementById(tbodyId).innerHTML = `
        <tr><td colspan="${colspan}" class="text-center py-4 text-danger">
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

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="${currentTab === 'accounts' ? 'loadAccounts' : 'loadTransactions'}(${pageNumber - 1})" aria-label="Önceki"><i class="bi bi-chevron-left"></i></button>`;

    const startPage = Math.max(1, pageNumber - 2);
    const endPage = Math.min(totalPages, pageNumber + 2);

    if (startPage > 1) {
        html += `<button class="page-btn" onclick="${currentTab === 'accounts' ? 'loadAccounts' : 'loadTransactions'}(1)">1</button>`;
        if (startPage > 2) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
    }

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === pageNumber ? 'active' : ''}" onclick="${currentTab === 'accounts' ? 'loadAccounts' : 'loadTransactions'}(${i})">${i}</button>`;
    }

    if (endPage < totalPages) {
        if (endPage < totalPages - 1) html += `<span style="padding:0 4px;color:#94a3b8;">…</span>`;
        html += `<button class="page-btn" onclick="${currentTab === 'accounts' ? 'loadAccounts' : 'loadTransactions'}(${totalPages})">${totalPages}</button>`;
    }

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="${currentTab === 'accounts' ? 'loadAccounts' : 'loadTransactions'}(${pageNumber + 1})" aria-label="Sonraki"><i class="bi bi-chevron-right"></i></button>`;

    buttons.innerHTML = html;
}
