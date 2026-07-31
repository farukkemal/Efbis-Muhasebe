// ─── SuperAdmin SaaS Command Center JavaScript Module ──────────────────────
// Multi-tenant switching, Tenant inspection, User CRUD, System Stats, Audit Trail

'use strict';

let currentUsersPage = 1;
let usersPageSize = 10;
let userModal;
let resetPasswordModal;

$(document).ready(function () {
    initAdmin();
});

function initAdmin() {
    userModal = new bootstrap.Modal(document.getElementById('userModal'));
    resetPasswordModal = new bootstrap.Modal(document.getElementById('resetPasswordModal'));

    loadDashboardStats();
    loadTenantsData();
    loadUsersData(1);

    $('#btnNewUser').click(openCreateUserModal);
    $('#btnRefreshAdmin').click(() => { loadDashboardStats(); loadTenantsData(); loadUsersData(currentUsersPage); });
    $('#btnSaveUser').click(saveUser);

    let searchTimeout;
    $('#filterUserSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => loadUsersData(1), 400);
    });

    $('#filterUserRole, #filterUserStatus').change(() => loadUsersData(1));
}

function loadDashboardStats() {
    efbisAjax.get('/Admin/GetStats', {}, function (response) {
        if (response.success && response.data) {
            const d = response.data;
            $('#statTotalTenants').text(d.tenantCount ?? d.TenantCount ?? 0);
            $('#statTotalUsers').text(d.totalUsers ?? d.TotalUsers ?? 0);
            $('#statDbServerNode').html((d.dbServer ?? '(localdb)\\mssqllocaldb 🟢'));

            const rev = (d.totalInvoicesAmount ?? d.TotalInvoicesAmount ?? 0);
            $('#statPlatformRevenue').text(rev.toLocaleString('tr-TR', { minimumFractionDigits: 2 }) + ' ₺');

            const activeTenantId = d.activeTenantId ?? d.ActiveTenantId ?? 0;
            const currentTenantName = d.currentTenantName ?? d.CurrentTenantName ?? 'Ortak Görünüm';

            if (activeTenantId > 0) {
                $('#currentTenantBadge').text('Aktif İnceleme: ' + currentTenantName).removeClass('bg-purple').addClass('bg-success');
                $('#currentTenantSubtext').text(`Şu an "${currentTenantName}" şirketinin özel verilerini inceliyorsunuz. Şirket değişimi için tablodan seçim yapabilirsiniz.`);
            } else {
                $('#currentTenantBadge').text('Ortak Görünüm').removeClass('bg-success').addClass('bg-purple');
                $('#currentTenantSubtext').text('Tüm kayıtlı şirketlerin ortak finansal verilerini ve platform parametrelerini yönetiyorsunuz.');
            }
        }
    });
}

function loadTenantsData() {
    efbisAjax.get('/Admin/GetTenants', {}, function (response) {
        if (response.success && response.data) {
            renderTenantsTable(response.data);
        }
    });
}

function renderTenantsTable(tenants) {
    const tbody = $('#tenantsTableBody');
    tbody.empty();

    if (!tenants || tenants.length === 0) {
        tbody.append(`<tr><td colspan="8" class="text-center py-4 text-muted">Kayıtlı şirket bulunamadı.</td></tr>`);
        return;
    }

    tenants.forEach(t => {
        const id = t.id || t.Id;
        const code = t.tenantCode || t.TenantCode || '—';
        const companyName = t.companyName || t.CompanyName || '—';
        const email = t.email || t.Email || '—';
        const city = t.city || t.City || 'İstanbul';
        const sector = t.sector || t.Sector || 'Genel';
        const created = t.createdDateStr || t.CreatedDateStr || '—';
        const prodCount = t.productCount ?? t.ProductCount ?? 0;
        const invCount = t.invoiceCount ?? t.InvoiceCount ?? 0;
        const isActive = t.isActive ?? t.IsActive ?? true;

        const statusBadge = isActive
            ? '<span class="badge bg-success-subtle text-success"><i class="bi bi-check-circle me-1"></i>Aktif Şirket</span>'
            : '<span class="badge bg-danger-subtle text-danger"><i class="bi bi-x-circle me-1"></i>Pasif</span>';

        tbody.append(`
            <tr>
                <td><span class="badge bg-secondary font-monospace">${escHtml(code)}</span></td>
                <td><strong style="color:#0f172a;font-size:14px;">${escHtml(companyName)}</strong></td>
                <td style="font-size:13px;color:#2563eb;"><i class="bi bi-envelope me-1"></i>${escHtml(email)}</td>
                <td style="font-size:12.5px;color:#475569;">${escHtml(city)} <span class="text-muted">(${escHtml(sector)})</span></td>
                <td>
                    <span class="badge bg-info-subtle text-info me-1" title="Stok Ürünü"><i class="bi bi-box-seam me-1"></i>${prodCount} Ürün</span>
                    <span class="badge bg-primary-subtle text-primary" title="Resmi Fatura"><i class="bi bi-receipt me-1"></i>${invCount} Fatura</span>
                </td>
                <td style="font-size:12px;color:#64748b;">${escHtml(created)}</td>
                <td>${statusBadge}</td>
                <td style="text-align:center;">
                    <button class="btn btn-sm btn-success fw-bold me-1" onclick="inspectTenantData(${id}, '${escHtml(companyName)}')" title="Bu Şirketin Verilerini İncele">
                        <i class="bi bi-eye-fill me-1"></i>Verilerini İncele
                    </button>
                </td>
            </tr>
        `);
    });
}

window.inspectTenantData = function (tenantId, companyName) {
    efbisAjax.post('/Admin/SwitchActiveTenant', { tenantId: tenantId }, function (res) {
        if (res.success) {
            showToast(`"${companyName}" şirket görünümüne geçildi! Veriler yükleniyor...`, 'success');
            loadDashboardStats();
            setTimeout(() => {
                window.location.href = '/Invoices';
            }, 800);
        } else {
            showToast(res.message || 'Hata oluştu.', 'error');
        }
    });
};

window.resetToGlobalView = function () {
    efbisAjax.post('/Admin/SwitchActiveTenant', { tenantId: 0 }, function (res) {
        if (res.success) {
            showToast('Tüm Şirketler Ortak Görünümüne geçildi.', 'info');
            loadDashboardStats();
            loadTenantsData();
            loadUsersData(1);
        }
    });
};

function loadUsersData(page) {
    currentUsersPage = page;

    const filter = {
        pageNumber: page,
        pageSize: usersPageSize,
        searchTerm: $('#filterUserSearch').val(),
        role: $('#filterUserRole').val(),
        isActive: $('#filterUserStatus').val()
    };

    efbisAjax.get('/Admin/GetUsers', filter, function (response) {
        if (response.success && response.data) {
            const items = response.data.items || response.data.Items || [];
            renderUsersTable(items);
            renderUsersPagination(response.data);
        }
    });
}

function renderUsersTable(items) {
    const tbody = $('#usersTableBody');
    tbody.empty();

    if (!items || items.length === 0) {
        tbody.append(`<tr><td colspan="9" class="text-center py-4 text-muted">Kayıtlı kullanıcı bulunamadı.</td></tr>`);
        return;
    }

    items.forEach(user => {
        const id = user.id || user.Id;
        const email = user.email || user.Email || '';
        const name = user.fullName || user.FullName || '';
        const title = user.title || user.Title || '—';
        const roleText = user.roleText || user.RoleText || 'Kullanıcı';
        const badgeClass = user.roleBadgeClass || user.RoleBadgeClass || 'bg-secondary';
        const phone = user.phoneNumber || user.PhoneNumber || '—';
        const initials = user.initials || user.Initials || 'US';
        const lastLogin = user.formattedLastLogin || user.FormattedLastLogin || 'Henüz Giriş Yapmadı';
        const isActive = user.isActive ?? user.IsActive ?? true;

        const switchChecked = isActive ? 'checked' : '';

        tbody.append(`
            <tr>
                <td>
                    <div class="rounded-circle bg-primary text-white d-flex align-items-center justify-content-center fw-bold" style="width:36px;height:36px;font-size:12.5px;">
                        ${escHtml(initials)}
                    </div>
                </td>
                <td><strong style="color:#0f172a;">${escHtml(email)}</strong></td>
                <td>${escHtml(name)}</td>
                <td style="font-size:12.5px;color:#64748b;">${escHtml(title)}</td>
                <td><span class="badge ${badgeClass}" style="font-size:11.5px;">${escHtml(roleText)}</span></td>
                <td style="font-size:12px;">${escHtml(phone)}</td>
                <td style="font-size:12px;color:#475569;">${escHtml(lastLogin)}</td>
                <td>
                    <div class="form-check form-switch m-0" title="Hesap Durumunu Değiştir">
                        <input class="form-check-input" type="checkbox" ${switchChecked} onchange="toggleUserStatus(${id})">
                    </div>
                </td>
                <td style="text-align:center;">
                    <div class="d-flex gap-1 justify-content-center">
                        <button class="btn-action btn-action-edit" onclick="openEditUserModal(${id})" title="Düzenle"><i class="bi bi-pencil"></i></button>
                        <button class="btn-action text-warning" style="background:#fef3c7;border-color:#fde047;" onclick="openResetPasswordModal(${id})" title="Şifre Sıfırla"><i class="bi bi-key"></i></button>
                        <button class="btn-action btn-action-delete" onclick="deleteUser(${id})" title="Sil"><i class="bi bi-trash"></i></button>
                    </div>
                </td>
            </tr>
        `);
    });
}

function openCreateUserModal() {
    document.getElementById('userForm').reset();
    $('#userId').val('0');
    $('.password-field').removeClass('d-none');
    $('#userPassword').prop('required', true);
    $('#userModalTitle').html('<i class="bi bi-person-plus me-2"></i>Yeni Kullanıcı Hesabı');
    userModal.show();
}

window.openEditUserModal = function (id) {
    efbisAjax.get(`/Admin/GetUserDetail/${id}`, {}, function (res) {
        if (res.success && res.data) {
            const user = res.data;
            document.getElementById('userForm').reset();
            $('#userId').val(id);

            $('#userEmail').val(user.email || user.Email);
            $('#userFullName').val(user.fullName || user.FullName);
            $('#userRole').val(user.role || user.Role);
            $('#userTitleInput').val(user.title || user.Title);
            $('#userPhone').val(user.phoneNumber || user.PhoneNumber);
            $('#userIsActive').prop('checked', user.isActive ?? user.IsActive);

            $('.password-field').addClass('d-none');
            $('#userPassword').prop('required', false);

            $('#userModalTitle').html('<i class="bi bi-pencil-square me-2"></i>Kullanıcı Hesabı Düzenle');
            userModal.show();
        }
    });
};

function saveUser() {
    const form = document.getElementById('userForm');
    const id = $('#userId').val();

    if (id === '0' && !$('#userPassword').val()) {
        showToast('Yeni kullanıcı için şifre zorunludur.', 'warning');
        return;
    }

    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const isNew = id === '0';
    const url = isNew ? '/Admin/CreateUser' : `/Admin/UpdateUser/${id}`;

    const dto = {
        id: parseInt(id),
        email: $('#userEmail').val(),
        password: $('#userPassword').val(),
        fullName: $('#userFullName').val(),
        role: parseInt($('#userRole').val()),
        title: $('#userTitleInput').val(),
        phoneNumber: $('#userPhone').val(),
        isActive: $('#userIsActive').is(':checked')
    };

    efbisAjax.post(url, dto, function (res) {
        if (res.success) {
            showToast(res.message || 'Kullanıcı başarıyla kaydedildi.', 'success');
            userModal.hide();
            loadUsersData(currentUsersPage);
            loadDashboardStats();
        } else {
            showToast(res.message || 'Hata oluştu.', 'error');
        }
    });
}

window.toggleUserStatus = function (id) {
    efbisAjax.post(`/Admin/ToggleUserStatus/${id}`, {}, function (res) {
        if (res.success) {
            showToast(res.message || 'Kullanıcı durumu güncellendi.', 'success');
            loadUsersData(currentUsersPage);
            loadDashboardStats();
        } else {
            showToast(res.message || 'İşlem başarısız.', 'error');
        }
    });
};

window.openResetPasswordModal = function (id) {
    $('#resetUserId').val(id);
    $('#resetNewPassword').val('');
    resetPasswordModal.show();
};

window.confirmResetPassword = function () {
    const userId = $('#resetUserId').val();
    const newPassword = $('#resetNewPassword').val();

    if (!newPassword || newPassword.length < 6) {
        showToast('Yeni şifre en az 6 karakter olmalıdır.', 'warning');
        return;
    }

    efbisAjax.post('/Admin/ResetUserPassword', { userId: parseInt(userId), newPassword: newPassword }, function (res) {
        if (res.success) {
            showToast(res.message || 'Şifre başarıyla sıfırlandı.', 'success');
            resetPasswordModal.hide();
        } else {
            showToast(res.message || 'Hata oluştu.', 'error');
        }
    });
};

window.deleteUser = function (id) {
    if (!confirm('Bu kullanıcı hesabını silmek istediğinize emin misiniz?')) return;

    efbisAjax.post(`/Admin/DeleteUser/${id}`, {}, function (res) {
        if (res.success) {
            showToast(res.message || 'Kullanıcı silindi.', 'success');
            loadUsersData(currentUsersPage);
            loadDashboardStats();
        } else {
            showToast(res.message || 'Silinemedi.', 'error');
        }
    });
};

// ─── Audit Trail Logs ─────────────────────────────────────────────────────────
function loadAuditLogs() {
    efbisAjax.get('/Admin/GetAuditLogs', {}, function (res) {
        if (res.success && res.data) {
            const tbody = $('#auditLogsBody');
            tbody.empty();

            res.data.forEach(log => {
                let levelBadge = '<span class="badge bg-info">INFO</span>';
                if (log.level === 'WARN') levelBadge = '<span class="badge bg-warning text-dark">WARN</span>';
                else if (log.level === 'SECURITY') levelBadge = '<span class="badge bg-danger">SECURITY</span>';

                tbody.append(`
                    <tr>
                        <td style="font-size:12px;font-weight:600;color:#334155;">${escHtml(log.timestamp)}</td>
                        <td><strong style="color:#0f172a;">${escHtml(log.user)}</strong></td>
                        <td>${levelBadge}</td>
                        <td><span class="badge bg-secondary">${escHtml(log.module)}</span></td>
                        <td>${escHtml(log.action)}</td>
                        <td><code style="font-size:11.5px;">${escHtml(log.ipAddress)}</code></td>
                    </tr>
                `);
            });
        }
    });
}

function clearUserFilters() {
    $('#filterUserSearch').val('');
    $('#filterUserRole').val('');
    $('#filterUserStatus').val('');
    loadUsersData(1);
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

function renderUsersPagination(data) {
    const container = $('#usersPagination');
    container.empty();

    const totalPages = data.totalPages || Math.ceil((data.totalCount || 0) / usersPageSize) || 1;
    const totalCount = data.totalCount || 0;

    const start = totalCount === 0 ? 0 : ((currentUsersPage - 1) * usersPageSize) + 1;
    const end = Math.min(currentUsersPage * usersPageSize, totalCount);

    let html = `
        <div class="text-muted small">
            ${totalCount === 0 ? 'Kullanıcı bulunamadı' : `Toplam <strong>${totalCount}</strong> kullanıcıdan <strong>${start}–${end}</strong> arası gösteriliyor`}
        </div>
        <ul class="pagination efbis-pagination mb-0">
            <li class="page-item ${currentUsersPage === 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadUsersData(${currentUsersPage - 1})"><i class="bi bi-chevron-left"></i></a>
            </li>
    `;

    if (totalPages > 1) {
        for (let i = 1; i <= totalPages; i++) {
            if (i === 1 || i === totalPages || (i >= currentUsersPage - 1 && i <= currentUsersPage + 1)) {
                html += `
                    <li class="page-item ${i === currentUsersPage ? 'active' : ''}">
                        <a class="page-link page-btn" href="javascript:void(0)" onclick="loadUsersData(${i})">${i}</a>
                    </li>
                `;
            }
        }
    }

    html += `
            <li class="page-item ${currentUsersPage === totalPages || totalPages <= 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadUsersData(${currentUsersPage + 1})"><i class="bi bi-chevron-right"></i></a>
            </li>
        </ul>
    `;

    container.html(html);
}

window.openTenantDbModal = function (tenantId) {
    const modal = new bootstrap.Modal(document.getElementById('tenantDbModal'));
    $('#tenantDbImpersonateId').val(tenantId);

    efbisAjax.get('/Admin/GetTenantDatabaseDetails', { tenantId: tenantId }, function (res) {
        if (res.success && res.tenant) {
            const t = res.tenant;
            const tbl = res.tables;

            $('#tenantDbModalTitle').html(`<i class="bi bi-database-fill text-info me-2"></i>Veritabanı: <strong>${escHtml(t.companyName)}</strong> (${escHtml(t.tenantCode)})`);
            $('#tenantDbInfo').html(`
                <strong>Sunucu:</strong> <code>${escHtml(t.dbServer)}</code> | 
                <strong>Katalog:</strong> <code>${escHtml(t.dbName)}</code> | 
                <strong>Filtre:</strong> <code>${escHtml(t.schemaScope)}</code>
            `);

            // Products
            const pBody = $('#dbProductsTableBody').empty();
            if (tbl.products && tbl.products.length > 0) {
                tbl.products.forEach(p => pBody.append(`<tr><td>${p.id}</td><td><code>${escHtml(p.productCode)}</code></td><td><strong>${escHtml(p.productName)}</strong></td><td>${p.salePrice} ₺</td><td>${p.currentStock}</td><td>${p.createdDateStr}</td></tr>`));
            } else pBody.append('<tr><td colspan="6" class="text-center text-muted">Ürün tablosunda kayıt yok.</td></tr>');

            // Invoices
            const iBody = $('#dbInvoicesTableBody').empty();
            if (tbl.invoices && tbl.invoices.length > 0) {
                tbl.invoices.forEach(inv => iBody.append(`<tr><td>${inv.id}</td><td><code>${escHtml(inv.invoiceNumber)}</code></td><td><strong>${inv.grandTotal} ₺</strong></td><td>${inv.invoiceDateStr}</td><td><span class="badge bg-primary">${escHtml(inv.statusStr)}</span></td></tr>`));
            } else iBody.append('<tr><td colspan="5" class="text-center text-muted">Fatura tablosunda kayıt yok.</td></tr>');

            // Customers
            const cBody = $('#dbCustomersTableBody').empty();
            if (tbl.customers && tbl.customers.length > 0) {
                tbl.customers.forEach(c => cBody.append(`<tr><td>${c.id}</td><td><code>${escHtml(c.customerCode)}</code></td><td><strong>${escHtml(c.title)}</strong></td><td>${c.balance} ₺</td><td>${escHtml(c.phone || '—')}</td></tr>`));
            } else cBody.append('<tr><td colspan="5" class="text-center text-muted">Cari tablosunda kayıt yok.</td></tr>');

            // Cash Accounts
            const caBody = $('#dbCashTableBody').empty();
            if (tbl.cashAccounts && tbl.cashAccounts.length > 0) {
                tbl.cashAccounts.forEach(ca => caBody.append(`<tr><td>${ca.id}</td><td><code>${escHtml(ca.accountCode)}</code></td><td><strong>${escHtml(ca.accountName)}</strong></td><td>${ca.balance} ₺</td><td>${escHtml(ca.currency)}</td></tr>`));
            } else caBody.append('<tr><td colspan="5" class="text-center text-muted">Kasa tablosunda kayıt yok.</td></tr>');

            // Employees
            const eBody = $('#dbEmployeesTableBody').empty();
            if (tbl.employees && tbl.employees.length > 0) {
                tbl.employees.forEach(e => eBody.append(`<tr><td>${e.id}</td><td><strong>${escHtml(e.fullName)}</strong></td><td>${escHtml(e.title || '—')}</td><td>${escHtml(e.department || '—')}</td><td>${escHtml(e.phone || '—')}</td></tr>`));
            } else eBody.append('<tr><td colspan="5" class="text-center text-muted">Personel tablosunda kayıt yok.</td></tr>');

            modal.show();
        } else {
            showToast(res.message || 'Veritabanı detayları alınamadı.', 'error');
        }
    });
};
