// ─── Müşteri & Şirket Kurulum Portalı JavaScript Module ──────────────────────

'use strict';

document.addEventListener('DOMContentLoaded', function () {
    loadTenants();
});

async function loadTenants() {
    const tbody = document.getElementById('tenantsTbody');
    if (!tbody) return;

    try {
        const res = await fetch('/TenantSetup/GetTenants');
        if (!res.ok) throw new Error('Sunucu hatası');
        const tenants = await res.json();

        if (!tenants || tenants.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="8" class="text-center py-4 text-muted">
                        <i class="bi bi-building fs-3 d-block mb-2"></i>
                        Henüz kayıtlı müşteri şirketi bulunmuyor. Yeni bir müşteri şirketi tanımlamak için <strong>"Yeni Müşteri Şirket Kaydı Ekle"</strong> butonuna tıklayınız.
                    </td>
                </tr>`;
            return;
        }

        tbody.innerHTML = tenants.map(t => {
            const id = t.Id ?? t.id;
            const code = t.TenantCode || t.tenantCode || `TEN-${id}`;
            const name = t.CompanyName || t.companyName || '';
            const sector = t.Sector || t.sector || 'Genel Ticaret';
            const email = t.Email || t.email || '—';
            const city = t.City || t.city || 'İstanbul';
            const isActive = t.IsActive ?? t.isActive ?? true;
            const dateStr = t.CreatedDate || t.createdDate ? new Date(t.CreatedDate || t.createdDate).toLocaleDateString('tr-TR') : '—';

            return `
                <tr>
                    <td><strong class="text-primary">#${id}</strong></td>
                    <td><code class="bg-light px-2 py-1 rounded border">${escHtml(code)}</code></td>
                    <td><strong>${escHtml(name)}</strong></td>
                    <td><span class="badge bg-secondary-subtle text-secondary">${escHtml(sector)}</span></td>
                    <td>${escHtml(email)}</td>
                    <td>${escHtml(city)}</td>
                    <td>${isActive ? '<span class="badge bg-success">Aktif 🟢</span>' : '<span class="badge bg-danger">Pasif 🔴</span>'}</td>
                    <td class="text-muted small">${dateStr}</td>
                </tr>`;
        }).join('');
    } catch (e) {
        console.error(e);
        tbody.innerHTML = `<tr><td colspan="8" class="text-center py-4 text-danger"><i class="bi bi-exclamation-triangle me-2"></i>Şirket listesi alınamadı.</td></tr>`;
    }
}

function openCreateTenantModal() {
    document.getElementById('createTenantForm')?.reset();
    document.getElementById('tnCity').value = 'İstanbul';
    document.getElementById('tnAdminPassword').value = 'Musteri123!';
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('createTenantModal'));
    modal.show();
}

async function submitCreateTenant() {
    const name = document.getElementById('tnCompanyName')?.value.trim();
    const tradeTitle = document.getElementById('tnTradeTitle')?.value.trim();
    const sector = document.getElementById('tnSector')?.value;
    const taxOffice = document.getElementById('tnTaxOffice')?.value.trim();
    const taxNumber = document.getElementById('tnTaxNumber')?.value.trim();
    const phone = document.getElementById('tnPhone')?.value.trim();
    const city = document.getElementById('tnCity')?.value.trim();
    const address = document.getElementById('tnAddress')?.value.trim();
    const adminName = document.getElementById('tnAdminFullName')?.value.trim();
    const adminEmail = document.getElementById('tnAdminEmail')?.value.trim();
    const adminPassword = document.getElementById('tnAdminPassword')?.value.trim();

    if (!name || !adminName || !adminEmail || !adminPassword) {
        showToast('Lütfen zorunlu alanları doldurunuz (Şirket Adı, Yetkili Adı, E-Posta ve Şifre).', 'warning');
        return;
    }

    const payload = {
        CompanyName: name,
        TradeTitle: tradeTitle,
        Sector: sector,
        TaxOffice: taxOffice,
        TaxNumber: taxNumber,
        Phone: phone,
        City: city,
        Address: address,
        AdminFullName: adminName,
        AdminEmail: adminEmail,
        AdminPassword: adminPassword
    };

    try {
        const res = await efbisAjax.post('/TenantSetup/CreateNewTenant', payload);
        if (res && res.success) {
            showToast(res.message || 'Müşteri şirketi başarıyla kaydedildi!', 'success');
            bootstrap.Modal.getInstance(document.getElementById('createTenantModal'))?.hide();
            loadTenants();
        } else {
            showToast((res && res.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        console.error(e);
        showToast('Müşteri şirketi açılırken bir hata oluştu.', 'error');
    }
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
