// ─── Cari Hesaplar & Tedarikçiler JavaScript Module ──────────────────────────────

'use strict';

let custCurrentPage = 1;
let custPageSize = 10;
let custSortColumn = 'Title';
let custSortAscending = true;
let custSearchTimeout = null;
let editingCustId = null;
let deletingCustId = null;

document.addEventListener('DOMContentLoaded', function () {
    const urlParams = new URLSearchParams(window.location.search);
    const typeParam = urlParams.get('type');
    const viewParam = urlParams.get('view');

    if (viewParam === 'payments') {
        updateHeaderForPaymentsMode();
    } else if (viewParam === 'receivables') {
        updateHeaderForReceivablesMode();
    } else if (typeParam === '2' || typeParam === 'supplier' || typeParam === 'tedarikci') {
        const typeSelect = document.getElementById('custTypeFilter');
        if (typeSelect) typeSelect.value = '2';
        updateHeaderForSupplierMode(true);
    } else if (typeParam === '1' || typeParam === 'customer') {
        const typeSelect = document.getElementById('custTypeFilter');
        if (typeSelect) typeSelect.value = '1';
        updateHeaderForCustomerMode(true);
    }

    loadCustomerDashboard();
    loadCustomers();
});

function updateHeaderForPaymentsMode() {
    const titleEl = document.getElementById('custMainPageTitle');
    const subEl = document.getElementById('custMainPageSubtitle');
    const breadcrumbEl = document.getElementById('custBreadcrumbActive');
    const tableTitleEl = document.getElementById('tableTitleText');

    if (titleEl) titleEl.innerHTML = '<i class="bi bi-cash-coin me-2 text-success"></i>Cari Ödemeler & Tahsilatlar';
    if (subEl) subEl.textContent = 'Müşterilerden alınan tahsilatları ve tedarikçilere yapılan ödemeleri kaydedin. Kasa/Banka ve Gelir/Gider anlık güncellenir.';
    if (tableTitleEl) tableTitleEl.textContent = 'Cari Ödeme Kayıtları';
    if (breadcrumbEl) breadcrumbEl.textContent = 'Cari Ödemeler';
}

function updateHeaderForReceivablesMode() {
    const titleEl = document.getElementById('custMainPageTitle');
    const subEl = document.getElementById('custMainPageSubtitle');
    const breadcrumbEl = document.getElementById('custBreadcrumbActive');
    const tableTitleEl = document.getElementById('tableTitleText');
    const balanceSelect = document.getElementById('custBalanceFilter');

    if (balanceSelect) balanceSelect.value = '2'; // Borçlu (Alacağımız)

    if (titleEl) titleEl.innerHTML = '<i class="bi bi-journal-check me-2 text-info"></i>Cari Alacak & Borç Takip Portalı';
    if (subEl) subEl.textContent = 'Müşteri alacaklarınızı, tedarikçi borçlarınızı ve faturalardan doğan bakiyeleri takip edin.';
    if (tableTitleEl) tableTitleEl.textContent = 'Alacak / Borç Bakiye Listesi';
    if (breadcrumbEl) breadcrumbEl.textContent = 'Cari Alacak';
}

function onTypeFilterChange() {
    const val = document.getElementById('custTypeFilter')?.value;
    if (val === '2') {
        updateHeaderForSupplierMode(false);
    } else if (val === '1') {
        updateHeaderForCustomerMode(false);
    } else {
        resetHeaderToDefault();
    }
    loadCustomers(1);
}

function updateHeaderForSupplierMode(isFromUrl) {
    const titleEl = document.getElementById('custMainPageTitle');
    const subEl = document.getElementById('custMainPageSubtitle');
    const tableTitleEl = document.getElementById('tableTitleText');
    const breadcrumbEl = document.getElementById('custBreadcrumbActive');

    if (titleEl) titleEl.innerHTML = '<i class="bi bi-truck me-2 text-warning"></i>Tedarikçi Yönetimi';
    if (subEl) subEl.textContent = 'Mal ve hizmet satın alınan tedarikçi firma kartlarını, borç bakiyelerini ve iletişim detaylarını yönetin.';
    if (tableTitleEl) tableTitleEl.textContent = 'Tedarikçi Listesi';
    if (breadcrumbEl) breadcrumbEl.textContent = 'Tedarikçiler';

    document.title = 'Tedarikçiler - Efbis Muhasebe';
}

function updateHeaderForCustomerMode(isFromUrl) {
    const titleEl = document.getElementById('custMainPageTitle');
    const subEl = document.getElementById('custMainPageSubtitle');
    const tableTitleEl = document.getElementById('tableTitleText');
    const breadcrumbEl = document.getElementById('custBreadcrumbActive');

    if (titleEl) titleEl.innerHTML = '<i class="bi bi-person-badge me-2 text-primary"></i>Müşteri Yönetimi';
    if (subEl) subEl.textContent = 'Satış yapılan müşteri firma ve şahıs kartlarını, alacak bakiyelerini yönetin.';
    if (tableTitleEl) tableTitleEl.textContent = 'Müşteri Listesi';
    if (breadcrumbEl) breadcrumbEl.textContent = 'Müşteriler';

    document.title = 'Müşteriler - Efbis Muhasebe';
}

function resetHeaderToDefault() {
    const titleEl = document.getElementById('custMainPageTitle');
    const subEl = document.getElementById('custMainPageSubtitle');
    const tableTitleEl = document.getElementById('tableTitleText');
    const breadcrumbEl = document.getElementById('custBreadcrumbActive');

    if (titleEl) titleEl.innerHTML = '<i class="bi bi-people me-2 text-primary"></i>Cari Hesaplar & Tedarikçiler';
    if (subEl) subEl.textContent = 'Müşteri ve tedarikçi cari kartlarını, bakiyelerini ve iletişim bilgilerini yönetin.';
    if (tableTitleEl) tableTitleEl.textContent = 'Cari / Tedarikçi Kart Listesi';
    if (breadcrumbEl) breadcrumbEl.textContent = 'Cari Hesaplar & Tedarikçiler';

    document.title = 'Cari Hesaplar & Tedarikçiler - Efbis Muhasebe';
}

async function loadCustomerDashboard() {
    try {
        const stats = await efbisAjax.get('/Customers/GetDashboard');
        document.getElementById('statTotalCustomers').textContent = stats.TotalCustomers ?? stats.totalCustomers ?? 0;
        document.getElementById('statCustomersOnly').textContent = stats.CustomersOnly ?? stats.customersOnly ?? 0;
        document.getElementById('statSuppliersOnly').textContent = stats.SuppliersOnly ?? stats.suppliersOnly ?? 0;
        document.getElementById('statTotalReceivables').textContent = formatCurrency(stats.TotalReceivables ?? stats.totalReceivables ?? 0);
    } catch (e) {
        console.error('Dashboard istatistikleri yüklenemedi:', e);
    }
}

async function loadCustomers(page = null) {
    if (page !== null) custCurrentPage = page;
    custPageSize = parseInt(document.getElementById('custPageSize')?.value || '10');

    const params = new URLSearchParams({
        pageNumber: custCurrentPage,
        pageSize: custPageSize,
        searchTerm: document.getElementById('custSearchInput')?.value || '',
        customerType: document.getElementById('custTypeFilter')?.value || '',
        status: document.getElementById('custStatusFilter')?.value || '',
        balanceStatus: document.getElementById('custBalanceFilter')?.value || '',
        sortBy: custSortColumn,
        ascending: custSortAscending
    });

    setCustTableLoading(true);

    try {
        const data = await efbisAjax.get(`/Customers/GetCustomers?${params}`);
        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;
        const pageNumber = data.PageNumber ?? data.pageNumber ?? custCurrentPage;
        const pgSize = data.PageSize ?? data.pageSize ?? custPageSize;

        renderCustomersTable(items);
        renderCustPagination(totalCount, pageNumber, pgSize);
        document.getElementById('custTotalBadge').textContent = `${totalCount} kayıt`;
    } catch (err) {
        console.error(err);
        showCustTableError('Cari hesaplar yüklenirken bir hata oluştu.');
    } finally {
        setCustTableLoading(false);
    }
}

function renderCustomersTable(items) {
    const tbody = document.getElementById('customersBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="10">
                    <div class="empty-state">
                        <i class="bi bi-people"></i>
                        <h6>Cari veya tedarikçi kartı bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Arama kriterlerinizi değiştirmeyi veya yeni cari eklemeyi deneyin.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(c => {
        const id = c.Id ?? c.id;
        const code = c.CustomerCode || c.customerCode || '';
        const title = c.Title || c.title || '';
        const taxNumber = c.TaxNumber || c.taxNumber || '';
        const customerType = c.CustomerType ?? c.customerType ?? 1;
        const auth = c.AuthorizedPerson || c.authorizedPerson || '';
        const phone = c.Phone || c.phone || '';
        const city = c.City || c.city || '';
        const balance = c.Balance ?? c.balance ?? 0;
        const balanceStatus = c.BalanceStatus ?? c.balanceStatus ?? 0;
        const status = c.Status ?? c.status ?? 1;

        return `
        <tr id="cust-row-${id}">
            <td>
                <code style="background:#f1f5f9;padding:3px 8px;border-radius:6px;font-size:12px;font-weight:600;">${escHtml(code)}</code>
            </td>
            <td>
                <div class="product-name-cell">
                    <span class="product-name">${escHtml(title)}</span>
                    ${taxNumber ? `<small style="font-size:11px;color:#94a3b8;">VN/TC: ${escHtml(taxNumber)}</small>` : ''}
                </div>
            </td>
            <td>${renderCustTypeBadge(customerType)}</td>
            <td style="font-size:13px;color:#475569;">${auth ? escHtml(auth) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="font-size:13px;">${phone ? escHtml(phone) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td style="font-size:13px;">${city ? escHtml(city) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td class="price-cell">
                <span class="${balance > 0 ? 'text-success' : (balance < 0 ? 'text-danger' : 'text-muted')} fw-600">
                    ${formatCurrency(balance)}
                </span>
            </td>
            <td>${renderBalanceStatusBadge(balanceStatus, balance)}</td>
            <td>${renderCustStatusBadge(status)}</td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-detail" title="Cari Mutabakat Mektubu (Form BA/BS)" onclick="openCustReconciliationModal(${id})">
                        <i class="bi bi-file-earmark-check text-info"></i>
                    </button>
                    <button class="btn-action btn-action-detail" title="Detay" onclick="showCustDetail(${id})">
                        <i class="bi bi-eye"></i>
                    </button>
                    <button class="btn-action btn-action-edit" title="Düzenle" onclick="editCustomer(${id})">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action btn-action-status" title="${status === 1 ? 'Pasife Al' : 'Aktife Al'}" onclick="toggleCustStatus(${id})">
                        <i class="bi bi-arrow-left-right"></i>
                    </button>
                    <button class="btn-action btn-action-delete" title="Sil" onclick="deleteCustomer(${id}, '${escHtml(title)}')">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

function renderCustTypeBadge(type) {
    switch (type) {
        case 1: return '<span class="badge bg-primary px-2.5 py-1" style="font-size:11px;">Müşteri</span>';
        case 2: return '<span class="badge bg-warning text-dark px-2.5 py-1" style="font-size:11px;">Tedarikçi</span>';
        case 3: return '<span class="badge bg-info text-dark px-2.5 py-1" style="font-size:11px;">Müşteri & Tedarikçi</span>';
        default: return '<span class="badge bg-secondary">Cari</span>';
    }
}

function renderBalanceStatusBadge(balanceStatus, balance) {
    if (balance > 0) {
        return '<span class="badge bg-success-subtle text-success border border-success-subtle fw-600 px-2 py-1" style="font-size:11px;">🟢 Borçlu (Alacağımız)</span>';
    } else if (balance < 0) {
        return '<span class="badge bg-danger-subtle text-danger border border-danger-subtle fw-600 px-2 py-1" style="font-size:11px;">🔴 Alacaklı (Borcumuz)</span>';
    } else {
        return '<span class="badge bg-light text-secondary border fw-500 px-2 py-1" style="font-size:11px;">⚪ Bakiyesiz</span>';
    }
}

function renderCustStatusBadge(status) {
    return status === 1
        ? '<span class="badge-status-active">Aktif</span>'
        : '<span class="badge-status-passive">Pasif</span>';
}

function filterByType(typeVal) {
    document.getElementById('custTypeFilter').value = typeVal;
    onTypeFilterChange();
}

function filterByBalance(balVal) {
    document.getElementById('custBalanceFilter').value = balVal;
    loadCustomers(1);
}

function resetCustFilters() {
    document.getElementById('custSearchInput').value = '';
    document.getElementById('custTypeFilter').value = '';
    document.getElementById('custStatusFilter').value = '';
    document.getElementById('custBalanceFilter').value = '';
    document.getElementById('custPageSize').value = '10';
    resetHeaderToDefault();
    loadCustomerDashboard();
    loadCustomers(1);
}

function debounceCustSearch() {
    clearTimeout(custSearchTimeout);
    custSearchTimeout = setTimeout(() => loadCustomers(1), 400);
}

function sortByCust(col) {
    if (custSortColumn === col) {
        custSortAscending = !custSortAscending;
    } else {
        custSortColumn = col;
        custSortAscending = true;
    }
    loadCustomers(1);
}

function onModalTypeChange(typeVal) {
    const codeInput = document.getElementById('custCode');
    if (!codeInput || editingCustId !== null) return;

    if (!codeInput.value || codeInput.value.startsWith('MST-') || codeInput.value.startsWith('TDR-')) {
        if (typeVal === '2') {
            codeInput.value = 'TDR-' + String(Math.floor(100 + Math.random() * 900));
        } else {
            codeInput.value = 'MST-' + String(Math.floor(100 + Math.random() * 900));
        }
    }
}

function openCreateCustomerModal() {
    editingCustId = null;
    document.getElementById('custEditId').value = '0';
    
    const currentFilterType = document.getElementById('custTypeFilter')?.value;
    const defaultType = currentFilterType === '2' ? '2' : '1';
    
    document.getElementById('custType').value = defaultType;
    document.getElementById('custModalTitle').innerHTML = defaultType === '2' 
        ? '<i class="bi bi-truck text-warning me-2"></i>Yeni Tedarikçi Kartı Ekle' 
        : '<i class="bi bi-person-plus me-2"></i>Yeni Müşteri / Cari Ekle';
        
    document.getElementById('btnSaveCustomer').textContent = 'Kaydet';
    document.getElementById('initialBalanceWrapper').style.display = '';

    clearCustForm();
    clearCustErrors();

    document.getElementById('custType').value = defaultType;
    onModalTypeChange(defaultType);

    bootstrap.Modal.getOrCreateInstance(document.getElementById('customerModal')).show();
}

async function editCustomer(id) {
    editingCustId = id;
    document.getElementById('custEditId').value = id;
    document.getElementById('custModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Cari Kartı Düzenle';
    document.getElementById('btnSaveCustomer').textContent = 'Güncelle';
    document.getElementById('initialBalanceWrapper').style.display = 'none';

    clearCustErrors();

    try {
        const c = await efbisAjax.get(`/Customers/GetForEdit/${id}`);
        document.getElementById('custCode').value = c.CustomerCode || c.customerCode || '';
        document.getElementById('custTitle').value = c.Title || c.title || '';
        document.getElementById('custAuthorized').value = c.AuthorizedPerson || c.authorizedPerson || '';
        document.getElementById('custType').value = c.CustomerType ?? c.customerType ?? 1;
        document.getElementById('custTaxOffice').value = c.TaxOffice || c.taxOffice || '';
        document.getElementById('custTaxNumber').value = c.TaxNumber || c.taxNumber || '';
        document.getElementById('custPhone').value = c.Phone || c.phone || '';
        document.getElementById('custEmail').value = c.Email || c.email || '';
        document.getElementById('custCity').value = c.City || c.city || '';
        document.getElementById('custDistrict').value = c.District || c.district || '';
        document.getElementById('custAddress').value = c.Address || c.address || '';
        document.getElementById('custRiskLimit').value = c.RiskLimit ?? c.riskLimit ?? 0;

        bootstrap.Modal.getOrCreateInstance(document.getElementById('customerModal')).show();
    } catch (e) {
        showToast('Cari bilgileri yüklenemedi.', 'error');
    }
}

async function saveCustomer() {
    clearCustErrors();

    const isEdit = editingCustId !== null;
    const code = document.getElementById('custCode').value.trim();
    const title = document.getElementById('custTitle').value.trim();

    let valid = true;
    if (!code) {
        document.getElementById('err-custCode').textContent = 'Cari kodu zorunludur.';
        document.getElementById('custCode').classList.add('is-invalid');
        valid = false;
    }
    if (!title) {
        document.getElementById('err-custTitle').textContent = 'Firma unvanı zorunludur.';
        document.getElementById('custTitle').classList.add('is-invalid');
        valid = false;
    }
    if (!valid) return;

    const dto = {
        CustomerCode: code,
        Title: title,
        AuthorizedPerson: document.getElementById('custAuthorized').value.trim() || null,
        CustomerType: parseInt(document.getElementById('custType').value) || 1,
        TaxOffice: document.getElementById('custTaxOffice').value.trim() || null,
        TaxNumber: document.getElementById('custTaxNumber').value.trim() || null,
        Phone: document.getElementById('custPhone').value.trim() || null,
        Email: document.getElementById('custEmail').value.trim() || null,
        City: document.getElementById('custCity').value.trim() || null,
        District: document.getElementById('custDistrict').value.trim() || null,
        Address: document.getElementById('custAddress').value.trim() || null,
        RiskLimit: parseFloat(document.getElementById('custRiskLimit').value) || 0
    };

    if (!isEdit) {
        dto.InitialBalance = parseFloat(document.getElementById('custInitialBalance').value) || 0;
    } else {
        dto.Id = editingCustId;
    }

    const url = isEdit ? '/Customers/Update' : '/Customers/Create';

    try {
        const res = await efbisAjax.post(url, dto);
        bootstrap.Modal.getInstance(document.getElementById('customerModal'))?.hide();

        if (res.success) {
            showToast(res.message, 'success');
            loadCustomerDashboard();
            loadCustomers();
        } else {
            showToast(res.message, 'error');
        }
    } catch (e) {
        showToast('Kaydetme işlemi başarısız.', 'error');
    }
}

function deleteCustomer(id, title) {
    deletingCustId = id;
    document.getElementById('deleteCustTitle').textContent = title;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteCustomerModal')).show();
}

async function confirmDeleteCustomer() {
    if (!deletingCustId) return;

    try {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const res = await fetch(`/Customers/Delete/${deletingCustId}`, {
            method: 'POST',
            body: formData
        });
        const result = await res.json();

        bootstrap.Modal.getInstance(document.getElementById('deleteCustomerModal'))?.hide();

        if (result.success) {
            showToast(result.message, 'success');
            loadCustomerDashboard();
            loadCustomers();
        } else {
            showToast(result.message, 'warning');
        }
    } catch (e) {
        showToast('Silme işlemi başarısız.', 'error');
    }
}

async function toggleCustStatus(id) {
    try {
        const formData = new FormData();
        formData.append('__RequestVerificationToken', getAntiForgeryToken());

        const res = await fetch(`/Customers/ToggleStatus/${id}`, {
            method: 'POST',
            body: formData
        });
        const result = await res.json();

        if (result.success) {
            showToast(result.message, 'success');
            loadCustomers();
        } else {
            showToast(result.message, 'error');
        }
    } catch (e) {
        showToast('Durum değiştirme başarısız.', 'error');
    }
}

async function showCustDetail(id) {
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('customerDetailModal'));
    document.getElementById('custDetailBody').innerHTML = `<div class="text-center py-4"><div class="spinner-efbis mx-auto"></div></div>`;
    modal.show();

    try {
        const c = await efbisAjax.get(`/Customers/GetById/${id}`);
        const code = c.CustomerCode || c.customerCode || '';
        const title = c.Title || c.title || '';
        const customerType = c.CustomerType ?? c.customerType ?? 1;
        const auth = c.AuthorizedPerson || c.authorizedPerson || '';
        const taxOffice = c.TaxOffice || c.taxOffice || '';
        const taxNumber = c.TaxNumber || c.taxNumber || '';
        const status = c.Status ?? c.status ?? 1;
        const balance = c.Balance ?? c.balance ?? 0;
        const balanceStatus = c.BalanceStatus ?? c.balanceStatus ?? 0;
        const riskLimit = c.RiskLimit ?? c.riskLimit ?? 0;
        const phone = c.Phone || c.phone || '';
        const email = c.Email || c.email || '';
        const city = c.City || c.city || '';
        const district = c.District || c.district || '';
        const address = c.Address || c.address || '';
        const createdDate = c.CreatedDate || c.createdDate || '';

        document.getElementById('custDetailBody').innerHTML = `
            <div class="row g-3">
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-info-circle text-primary"></i> Temel Bilgiler</div>
                            ${detailRow('Cari Kodu', `<code style="background:#e2e8f0;padding:2px 7px;border-radius:5px;">${escHtml(code)}</code>`)}
                            ${detailRow('Unvan', title)}
                            ${detailRow('Cari Türü', renderCustTypeBadge(customerType))}
                            ${detailRow('Yetkili Kişi', auth || '—')}
                            ${detailRow('Vergi Dairesi', taxOffice || '—')}
                            ${detailRow('Vergi / TC No', taxNumber || '—')}
                            ${detailRow('Durum', renderCustStatusBadge(status))}
                        </div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="efbis-card" style="border:none;background:#f8fafc;">
                        <div class="efbis-card-body">
                            <div class="form-section-title"><i class="bi bi-cash-stack text-success"></i> Finans & İletişim</div>
                            ${detailRow('Bakiye', `<strong class="${balance > 0 ? 'text-success' : (balance < 0 ? 'text-danger' : '')}">${formatCurrency(balance)}</strong>`)}
                            ${detailRow('Bakiye Durumu', renderBalanceStatusBadge(balanceStatus, balance))}
                            ${detailRow('Risk Limiti', formatCurrency(riskLimit))}
                            ${detailRow('Telefon', phone || '—')}
                            ${detailRow('E-Posta', email || '—')}
                            ${detailRow('Şehir / İlçe', (city || '—') + ' / ' + (district || '—'))}
                            ${detailRow('Kayıt Tarihi', formatDate(createdDate))}
                        </div>
                    </div>
                </div>
                ${address ? `
                <div class="col-12">
                    <div class="p-3 bg-light rounded" style="font-size:13px;">
                        <strong>Açık Adres:</strong> ${escHtml(address)}
                    </div>
                </div>` : ''}
                <div class="col-12 text-end pt-2">
                    ${customerType === 2 || customerType === 3 ? `
                        <a href="/Invoices?type=Purchase" class="btn btn-sm btn-outline-warning me-1">
                            <i class="bi bi-cart-plus me-1"></i>Alış Faturası Kes
                        </a>` : ''}
                    ${customerType === 1 || customerType === 3 ? `
                        <a href="/Invoices?type=Sales" class="btn btn-sm btn-outline-success me-1">
                            <i class="bi bi-bag-check me-1"></i>Satış Faturası Kes
                        </a>` : ''}
                    <a href="/CashAccounts" class="btn btn-sm btn-outline-info">
                        <i class="bi bi-safe me-1"></i>Tahsilat/Tediye Yap
                    </a>
                </div>
            </div>`;
    } catch (e) {
        document.getElementById('custDetailBody').innerHTML = `<div class="text-center text-danger py-4">Cari detayları yüklenemedi.</div>`;
    }
}

function detailRow(label, val) {
    return `<div class="d-flex justify-content-between align-items-center py-1.5 border-bottom" style="font-size:13px;">
        <span class="text-muted">${label}:</span>
        <span class="fw-500 text-end">${val}</span>
    </div>`;
}

function clearCustForm() {
    ['custCode','custTitle','custAuthorized','custTaxOffice','custTaxNumber','custPhone','custEmail','custCity','custDistrict','custAddress'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.value = '';
    });
    document.getElementById('custType').value = '1';
    document.getElementById('custInitialBalance').value = '0';
    document.getElementById('custRiskLimit').value = '0';
}

function clearCustErrors() {
    ['err-custCode','err-custTitle'].forEach(id => {
        const el = document.getElementById(id);
        if (el) el.textContent = '';
    });
    document.querySelectorAll('#customerForm .is-invalid').forEach(el => el.classList.remove('is-invalid'));
}

function setCustTableLoading(loading) {
    const wrapper = document.getElementById('custTableWrapper');
    if (!wrapper) return;
    wrapper.style.opacity = loading ? '0.6' : '1';
    wrapper.style.pointerEvents = loading ? 'none' : '';
}

function showCustTableError(msg) {
    document.getElementById('customersBody').innerHTML = `
        <tr><td colspan="10" class="text-center py-4 text-danger">
            <i class="bi bi-exclamation-triangle me-2"></i>${msg}
        </td></tr>`;
}

function renderCustPagination(totalCount, pageNumber, pgSize) {
    const totalPages = Math.ceil(totalCount / pgSize);
    const info = document.getElementById('custPaginationInfo');
    const buttons = document.getElementById('custPaginationButtons');

    const from = totalCount === 0 ? 0 : ((pageNumber - 1) * pgSize + 1);
    const to = Math.min(pageNumber * pgSize, totalCount);
    info.textContent = totalCount === 0 ? 'Kayıt bulunamadı' : `${from}–${to} / ${totalCount} kayıt`;

    if (totalPages <= 1) {
        buttons.innerHTML = '';
        return;
    }

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadCustomers(${pageNumber - 1})"><i class="bi bi-chevron-left"></i></button>`;

    const startPage = Math.max(1, pageNumber - 2);
    const endPage = Math.min(totalPages, pageNumber + 2);

    for (let i = startPage; i <= endPage; i++) {
        html += `<button class="page-btn ${i === pageNumber ? 'active' : ''}" onclick="loadCustomers(${i})">${i}</button>`;
    }

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadCustomers(${pageNumber + 1})"><i class="bi bi-chevron-right"></i></button>`;

    buttons.innerHTML = html;
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
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

// ─── CARİ ÖDEME / TAHSİLAT MODAL FUNCTIONS ─────────────────────────────────────
async function openCustPaymentModal() {
    document.getElementById('custPaymentForm')?.reset();
    document.getElementById('payTransactionDate').value = new Date().toISOString().slice(0, 16);

    try {
        // Load Customers Dropdown
        const cRes = await fetch('/Customers/GetCustomers?pageSize=1000');
        if (cRes.ok) {
            const data = await cRes.json();
            const items = data.Items || data.items || [];
            const cSelect = document.getElementById('payCustomerId');
            cSelect.innerHTML = '<option value="">-- Cari Seçiniz --</option>';
            items.forEach(c => {
                const id = c.Id ?? c.id;
                const title = c.Title || c.title;
                const code = c.CustomerCode || c.customerCode;
                const bal = c.Balance ?? c.balance ?? 0;
                cSelect.innerHTML += `<option value="${id}">${escHtml(title)} (${escHtml(code)}) — Bakiye: ${formatCurrency(bal)}</option>`;
            });
        }

        // Load Cash Accounts Dropdown
        const caRes = await fetch('/CashAccounts/GetActive');
        if (caRes.ok) {
            const accounts = await caRes.json();
            const caSelect = document.getElementById('payCashAccountId');
            caSelect.innerHTML = '<option value="">-- Kasa / Banka Hesabı Seçiniz --</option>';
            accounts.forEach(a => {
                const id = a.Id ?? a.id;
                const name = a.AccountName || a.accountName;
                const code = a.AccountCode || a.accountCode;
                const bal = a.Balance ?? a.balance ?? 0;
                caSelect.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)}) — Kasa Bakiyesi: ${formatCurrency(bal)}</option>`;
            });
        }
    } catch (e) {
        console.error('Payment dropdown error:', e);
    }

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('custPaymentModal'));
    modal.show();
}

async function saveCustomerPayment() {
    const custId = parseInt(document.getElementById('payCustomerId').value);
    const txType = parseInt(document.getElementById('payTransactionType').value);
    const cashAccId = parseInt(document.getElementById('payCashAccountId').value);
    const amount = parseFloat(document.getElementById('payAmount').value) || 0;
    const dateVal = document.getElementById('payTransactionDate').value;
    const desc = document.getElementById('payDescription').value.trim();

    if (!custId || !cashAccId || amount <= 0) {
        showToast('Lütfen Cari Hesap, Kasa Hesabı ve 0\'dan büyük Ödeme Tutarı giriniz.', 'warning');
        return;
    }

    const payload = {
        CustomerId: custId,
        TransactionType: txType, // 1: Collection (Tahsilat), 2: Payment (Tediye)
        CashAccountId: cashAccId,
        Amount: amount,
        TransactionDate: dateVal ? new Date(dateVal).toISOString() : new Date().toISOString(),
        Description: desc || (txType === 1 ? 'Cari Tahsilat' : 'Cari Ödeme / Tediye')
    };

    try {
        const res = await efbisAjax.post('/CashAccounts/CreateTransaction', payload);
        if (res && res.success !== false) {
            showToast(res.message || 'Cari ödeme / tahsilat kaydı başarıyla oluşturuldu! Kasa, Cari ve Gelir/Gider güncellendi.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('custPaymentModal'))?.hide();
            loadCustomerDashboard();
            loadCustomers();
        } else {
            showToast((res && res.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        console.error(e);
        showToast('Ödeme kaydedilirken bir hata oluştu.', 'error');
    }
}

// ─── Cari Hesap Mutabakat Mektubu (Form BA/BS Uyumlu) ─────────────────────
async function openCustReconciliationModal(id) {
    try {
        const c = await efbisAjax.get(`/Customers/GetById/${id}`);
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('reconciliationLetterModal'));

        const title = c.Title || c.title || '';
        const code = c.CustomerCode || c.customerCode || '';
        const taxOffice = c.TaxOffice || c.taxOffice || 'Bakırköy';
        const taxNumber = c.TaxNumber || c.taxNumber || '1234567890';
        const balance = c.Balance ?? c.balance ?? 0;
        const balanceStatusText = balance > 0 ? 'BORÇLU (ALACAĞIMIZ)' : (balance < 0 ? 'ALACAKLI (BORCUMUZ)' : 'BAKİYESİZ');

        const nowStr = new Date().toLocaleDateString('tr-TR', { day: '2-digit', month: 'long', year: 'numeric' });

        const html = `
        <div style="background:#fff;padding:35px;border-radius:12px;border:1px solid #cbd5e1;font-family:'Segoe UI',Roboto,sans-serif;color:#0f172a;max-width:850px;margin:0 auto;">
            <!-- Header -->
            <div class="d-flex justify-content-between align-items-start pb-3 mb-4 border-bottom border-2">
                <div>
                    <h4 class="fw-bold text-dark mb-1">${escHtml(window.EFBIS_COMPANY_NAME || 'Şirketiniz')}</h4>
                    <p class="text-muted small mb-0">Bakırköy Vergi Dairesi | VKN: 1472583690 | Tel: (0212) 444 00 00</p>
                </div>
                <div class="text-end">
                    <span class="badge bg-dark fs-6 px-3 py-2 fw-bold font-monospace">FORM BA/BS MUTABAKAT</span>
                    <div class="text-muted small mt-1 font-monospace">Tarih: ${nowStr}</div>
                </div>
            </div>

            <!-- Subject & Addressee -->
            <div class="mb-4">
                <div class="fw-bold text-uppercase fs-6 text-primary mb-1">SAYIN: ${escHtml(title)}</div>
                <div class="small text-muted font-monospace">${escHtml(taxOffice)} Vergi Dairesi | VKN/TCKN: ${escHtml(taxNumber)} | Cari Kod: ${escHtml(code)}</div>
            </div>

            <!-- Main Body -->
            <div class="p-3 bg-light rounded border mb-4" style="line-height:1.7;font-size:14px;">
                <p class="mb-2">Şirketimiz ile firmanız arasındaki cari hesap hareketlerinin kontrolü amacıyla hazırlanan mutabakat mektubudur.</p>
                <p class="mb-2"><strong>${nowStr}</strong> tarihi itibariyle cari hesabınızda yapılan inceleme neticesinde tarafınızdaki bakiye durumu aşağıdaki gibidir:</p>
                
                <div class="d-flex justify-content-between align-items-center p-3 my-3 bg-white border rounded shadow-sm font-monospace fs-5">
                    <span>CARİ HESAP BAKİYESİ:</span>
                    <strong class="${balance > 0 ? 'text-success' : (balance < 0 ? 'text-danger' : 'text-dark')}">${formatCurrency(Math.abs(balance))} (${balanceStatusText})</strong>
                </div>

                <p class="mb-0">Bakiyede mutabıksanız işbu mektubun alt kısmını imzalayıp kaşeleyerek tarafımıza e-posta veya faks yoluyla iletmenizi rica ederiz.</p>
            </div>

            <!-- Signatures Section -->
            <div class="row g-4 pt-3 border-top">
                <div class="col-6 text-center">
                    <div class="fw-bold text-dark mb-1">FİRMAMIZ (DÜZENLEYEN)</div>
                    <div class="text-muted small mb-4">${escHtml(window.EFBIS_COMPANY_NAME || 'Şirketiniz')} Muhasebe Servisi</div>
                    <div style="height:50px;border-bottom:1px dashed #cbd5e1;width:70%;margin:0 auto;"></div>
                    <small class="text-muted d-block mt-1">İmza & Kaşe</small>
                </div>
                <div class="col-6 text-center">
                    <div class="fw-bold text-dark mb-1">MUTABIKIZ / MUTABIK DEĞİLİZ</div>
                    <div class="text-muted small mb-4">${escHtml(title)}</div>
                    <div style="height:50px;border-bottom:1px dashed #cbd5e1;width:70%;margin:0 auto;"></div>
                    <small class="text-muted d-block mt-1">Yetkili İmza & Kaşe</small>
                </div>
            </div>
        </div>`;

        document.getElementById('reconciliationPrintArea').innerHTML = html;
        modal.show();
    } catch (e) {
        showToast('Cari mutabakat mektubu hazırlanamadı', 'error');
    }
}

function printReconciliationLetter() {
    const content = document.getElementById('reconciliationPrintArea')?.innerHTML;
    if (!content) return;

    const printWin = window.open('', '_blank', 'width=900,height=750');
    printWin.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>Resmi Cari Hesap Mutabakat Mektubu</title>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
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
