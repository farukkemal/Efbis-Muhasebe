// ─── Categories Module JS ──────────────────────────────────────────────────────

'use strict';

let allCategories = [];
let deletingCatId = null;

document.addEventListener('DOMContentLoaded', function () {
    loadCategories();
});

async function loadCategories() {
    try {
        const categories = await efbisAjax.get('/Categories/GetCategories');
        allCategories = categories || [];
        renderCategoryTable(allCategories);
        updateCategoryStats(allCategories);
        populateParentDropdown(allCategories);
    } catch (err) {
        console.error(err);
        showToast('Kategoriler yüklenemedi.', 'error');
    }
}

function renderCategoryTable(categories) {
    const tbody = document.getElementById('categoriesBody');
    document.getElementById('categoryCountBadge').textContent = `${categories.length} kategori`;

    if (!categories || categories.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="6" class="text-center py-4 text-muted">
                    <i class="bi bi-tags d-block mb-2" style="font-size:32px;opacity:0.4;"></i>
                    Kategori bulunamadı.
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = categories.map(c => {
        const id = c.id ?? c.Id;
        const name = c.name ?? c.Name ?? '';
        const description = c.description ?? c.Description;
        const parentName = c.parentName ?? c.ParentName;
        const productCount = c.productCount ?? c.ProductCount ?? 0;
        const createdDate = c.createdDate ?? c.CreatedDate;

        return `
        <tr>
            <td>
                <span class="fw-600">${escHtml(name)}</span>
            </td>
            <td style="font-size:13px;color:#64748b;">${description ? escHtml(description) : '<span style="color:#cbd5e1;">—</span>'}</td>
            <td>
                ${parentName ? `<span class="badge bg-light text-dark border">${escHtml(parentName)}</span>` : '<span class="badge bg-secondary">Ana Kategori</span>'}
            </td>
            <td style="text-align:center;">
                <span class="badge ${productCount > 0 ? 'bg-primary' : 'bg-light text-dark border'}">
                    ${productCount} ürün
                </span>
            </td>
            <td style="font-size:12px;color:#94a3b8;">${formatDate(createdDate)}</td>
            <td>
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-edit" title="Düzenle" onclick="editCategory(${id})">
                        <i class="bi bi-pencil"></i>
                    </button>
                    <button class="btn-action btn-action-delete" title="Sil" onclick="deleteCategory(${id}, '${escHtml(name)}')">
                        <i class="bi bi-trash3"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

function updateCategoryStats(categories) {
    const total = categories.length;
    const parentCount = categories.filter(c => !(c.parentId ?? c.ParentId)).length;
    const totalProducts = categories.reduce((sum, c) => sum + (c.productCount ?? c.ProductCount ?? 0), 0);

    document.getElementById('statCategoryCount').textContent = total;
    document.getElementById('statParentCount').textContent = parentCount;
    document.getElementById('statTotalCategoryProducts').textContent = totalProducts;
}

function populateParentDropdown(categories, excludeId = null) {
    const select = document.getElementById('catParentId');
    const parents = categories.filter(c => {
        const id = c.id ?? c.Id;
        return !excludeId || id !== excludeId;
    });

    select.innerHTML = '<option value="">-- Ana Kategori (Üst Kategori Yok) --</option>' +
        parents.map(c => {
            const id = c.id ?? c.Id;
            const name = c.name ?? c.Name;
            return `<option value="${id}">${escHtml(name)}</option>`;
        }).join('');
}

function filterCategoryTable() {
    const term = document.getElementById('catSearchInput')?.value?.toLowerCase() || '';
    const filtered = allCategories.filter(c => {
        const name = (c.name ?? c.Name ?? '').toLowerCase();
        const description = (c.description ?? c.Description ?? '').toLowerCase();
        const parentName = (c.parentName ?? c.ParentName ?? '').toLowerCase();
        return name.includes(term) || description.includes(term) || parentName.includes(term);
    });
    renderCategoryTable(filtered);
}

function openCreateCategoryModal() {
    document.getElementById('catId').value = '0';
    document.getElementById('catName').value = '';
    document.getElementById('catDescription').value = '';
    document.getElementById('catParentId').value = '';
    document.getElementById('catModalTitle').innerHTML = '<i class="bi bi-tags me-2"></i>Yeni Kategori Ekle';
    document.getElementById('err-catName').textContent = '';
    populateParentDropdown(allCategories);

    bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryModal')).show();
}

async function editCategory(id) {
    try {
        const cat = await efbisAjax.get(`/Categories/GetById/${id}`);
        const catId = cat.id ?? cat.Id;
        const name = cat.name ?? cat.Name;
        const description = cat.description ?? cat.Description;
        const parentId = cat.parentId ?? cat.ParentId;

        document.getElementById('catId').value = catId;
        document.getElementById('catName').value = name;
        document.getElementById('catDescription').value = description || '';
        document.getElementById('catModalTitle').innerHTML = '<i class="bi bi-pencil me-2"></i>Kategoriyi Düzenle';
        document.getElementById('err-catName').textContent = '';

        populateParentDropdown(allCategories, catId);
        document.getElementById('catParentId').value = parentId || '';

        bootstrap.Modal.getOrCreateInstance(document.getElementById('categoryModal')).show();
    } catch (e) {
        showToast('Kategori yüklenemedi.', 'error');
    }
}

async function saveCategory() {
    const id = parseInt(document.getElementById('catId').value) || 0;
    const name = document.getElementById('catName').value.trim();
    const description = document.getElementById('catDescription').value.trim();
    const parentId = parseInt(document.getElementById('catParentId').value) || null;

    if (!name) {
        document.getElementById('err-catName').textContent = 'Kategori adı zorunludur.';
        return;
    }

    const dto = { id: id, name: name, description: description, parentId: parentId };
    const url = id > 0 ? '/Categories/Update' : '/Categories/Create';

    try {
        const res = await efbisAjax.post(url, dto);
        bootstrap.Modal.getInstance(document.getElementById('categoryModal'))?.hide();

        if (res.success) {
            showToast(res.message || 'Kategori kaydedildi.', 'success');
            loadCategories();
        } else {
            showToast(res.message || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('İşlem başarısız.', 'error');
    }
}

function deleteCategory(id, name) {
    deletingCatId = id;
    document.getElementById('deleteCatName').textContent = name;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('deleteCategoryModal')).show();
}

async function confirmDeleteCategory() {
    if (!deletingCatId) return;

    try {
        const res = await efbisAjax.post(`/Categories/Delete/${deletingCatId}`, {});
        bootstrap.Modal.getInstance(document.getElementById('deleteCategoryModal'))?.hide();

        if (res.success) {
            showToast(res.message || 'Kategori silindi.', 'success');
            loadCategories();
        } else {
            showToast(res.message || 'Silme işlemi başarısız.', 'warning');
        }
    } catch (e) {
        showToast('Silme işlemi başarısız.', 'error');
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

function formatDate(dateStr) {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '—';
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}
