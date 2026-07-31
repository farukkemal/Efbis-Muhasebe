// ─── Sidebar Toggle (Mobile & Desktop) ──────────────────────────────────────────
function initSidebarToggle() {
    const toggleBtn = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    if (!toggleBtn || !sidebar) return;

    // Mobile Backdrop elementi oluştur
    let backdrop = document.getElementById('sidebarBackdrop');
    if (!backdrop) {
        backdrop = document.createElement('div');
        backdrop.id = 'sidebarBackdrop';
        backdrop.className = 'sidebar-backdrop';
        document.body.appendChild(backdrop);
    }

    toggleBtn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();

        if (window.innerWidth < 992) {
            // Mobil görünümde menüyü aç/kapat
            const isOpen = document.body.classList.toggle('sidebar-mobile-open');
            sidebar.classList.toggle('show', isOpen);
        } else {
            // Masaüstü görünümde daralt/genişlet
            const isCollapsed = document.body.classList.toggle('sidebar-collapsed');
            localStorage.setItem('efbis_sidebar_collapsed', isCollapsed);
        }
    });

    // Backdrop tıklandığında mobilde sidebar'ı kapat
    backdrop.addEventListener('click', function () {
        document.body.classList.remove('sidebar-mobile-open');
        sidebar.classList.remove('show');
    });

    // Masaüstünde kullanıcının önceden seçtiği daraltma tercihini hatırla
    if (window.innerWidth >= 992) {
        const isCollapsed = localStorage.getItem('efbis_sidebar_collapsed') === 'true';
        if (isCollapsed) {
            document.body.classList.add('sidebar-collapsed');
        }
    }

    initSidebarScrollMemory();
}

// ─── Sidebar Scroll Memory & Auto-Scroll to Active Item ────────────────────
function initSidebarScrollMemory() {
    const sidebar = document.getElementById('sidebar');
    if (!sidebar) return;

    // 1. Restore scroll position from sessionStorage or scroll active item into view
    const savedScroll = sessionStorage.getItem('efbis_sidebar_scroll');
    const activeItem = sidebar.querySelector('.nav-item.active');

    if (savedScroll !== null) {
        sidebar.scrollTop = parseInt(savedScroll, 10);
    } else if (activeItem) {
        setTimeout(() => {
            activeItem.scrollIntoView({ block: 'center', behavior: 'instant' });
        }, 50);
    }

    // 2. Save scroll position on scroll
    sidebar.addEventListener('scroll', () => {
        sessionStorage.setItem('efbis_sidebar_scroll', sidebar.scrollTop);
    });

    // 3. Save scroll position when any sidebar link is clicked
    sidebar.querySelectorAll('a.nav-item').forEach(link => {
        link.addEventListener('click', () => {
            sessionStorage.setItem('efbis_sidebar_scroll', sidebar.scrollTop);
        });
    });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initSidebarToggle);
} else {
    initSidebarToggle();
}

// ─── Command Palette (Ctrl+K) ──────────────────────────────────────────────────
let cmdModalInstance = null;

function openCommandPalette() {
    const el = document.getElementById('commandPaletteModal');
    if (!el) return;

    if (el.parentElement !== document.body) {
        document.body.appendChild(el);
    }

    cmdModalInstance = bootstrap.Modal.getOrCreateInstance(el);
    cmdModalInstance.show();
    
    setTimeout(() => {
        const input = document.getElementById('cmdInput');
        if (input) {
            input.value = '';
            input.focus();
            filterCmdList('');
        }
    }, 150);
}

document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
        e.preventDefault();
        openCommandPalette();
    }
});

document.addEventListener('input', (e) => {
    if (e.target && e.target.id === 'cmdInput') {
        filterCmdList(e.target.value.toLowerCase().trim());
    }
});

function filterCmdList(query) {
    const items = document.querySelectorAll('#cmdList a');
    items.forEach(item => {
        const text = item.textContent.toLowerCase();
        if (!query || text.includes(query)) {
            item.style.display = 'flex';
        } else {
            item.style.display = 'none';
        }
    });
}

// ─── Export & Print Table Helpers ──────────────────────────────────────────────
/**
 * Tabloyu Şık ve Türkçe Karakter Uyumlu Excel (.xls/.xlsx) Olarak İndirir
 */
function exportTableToExcel(tableId, filename = 'rapor.xls') {
    const table = document.getElementById(tableId);
    if (!table) {
        showToast('İndirilecek tablo bulunamadı.', 'warning');
        return;
    }

    if (!filename.endsWith('.xls') && !filename.endsWith('.xlsx')) {
        filename = filename.replace(/\.[^/.]+$/, '') + '.xls';
    }

    // Tablonun bir kopyasını oluşturup işlem/buton sütunlarını temizle
    const clone = table.cloneNode(true);

    // Header ve body satırlarındaki "İşlemler" / "Actions" sütunlarını ve buton içeren hücreleri kaldır
    clone.querySelectorAll('tr').forEach(row => {
        const cells = Array.from(row.querySelectorAll('th, td'));
        cells.forEach(cell => {
            const text = cell.innerText.trim().toLowerCase();
            const hasButtonsOnly = cell.querySelectorAll('button, a.btn, .btn-action').length > 0 && cell.innerText.trim().length < 5;
            const isActionHeader = text === 'işlemler' || text === 'işlem' || text === 'actions' || text === 'action';
            
            if (isActionHeader || hasButtonsOnly) {
                cell.remove();
            }
        });
    });

    // Excel HTML XML Şablonu (UTF-8 BOM + Excel Stilleri)
    const excelTemplate = `
    <html xmlns:o="urn:schemas-microsoft-company:office:office" xmlns:x="urn:schemas-microsoft-company:office:excel" xmlns="http://www.w3.org/TR/REC-html40">
    <head>
        <meta http-equiv="Content-Type" content="text/html; charset=utf-8">
        <!--[if gte mso 9]>
        <xml>
            <x:ExcelWorkbook>
                <x:ExcelWorksheets>
                    <x:ExcelWorksheet>
                        <x:Name>Efbis Muhasebe Raporu</x:Name>
                        <x:WorksheetOptions>
                            <x:DisplayGridlines/>
                        </x:WorksheetOptions>
                    </x:ExcelWorksheet>
                </x:ExcelWorksheets>
            </x:ExcelWorkbook>
        </xml>
        <![endif]-->
        <style>
            body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; }
            table { border-collapse: collapse; width: 100%; }
            th { background-color: #0f172a; color: #ffffff; font-weight: bold; border: 1px solid #334155; padding: 10px 14px; text-align: left; font-size: 13px; }
            td { border: 1px solid #cbd5e1; padding: 8px 12px; font-size: 12px; color: #0f172a; }
            tr:nth-child(even) td { background-color: #f8fafc; }
            .text-end, th.text-end, td.text-end { text-align: right; }
            .text-center, th.text-center, td.text-center { text-align: center; }
            .badge { font-weight: bold; padding: 3px 8px; border-radius: 4px; }
        </style>
    </head>
    <body>
        <h2>Efbis Muhasebe — ${filename.replace(/\.[^/.]+$/, '')}</h2>
        ${clone.outerHTML}
    </body>
    </html>`;

    const blob = new Blob(['\uFEFF' + excelTemplate], { type: 'application/vnd.ms-excel;charset=utf-8' });
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = filename;
    link.style.display = 'none';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showToast('Excel dosyası başarıyla indirildi: ' + filename, 'success');
}

/**
 * Tabloyu CSV / Excel formatında indirir (Geriye Dönük Uyumlu)
 */
function exportTableToCSV(tableId, filename = 'rapor.csv') {
    exportTableToExcel(tableId, filename.replace('.csv', '.xls'));
}

/**
 * Tabloyu yazdırır
 */
function printTable(tableId, title = 'Rapor') {
    const table = document.getElementById(tableId);
    if (!table) return;

    const printWin = window.open('', '', 'width=900,height=700');
    printWin.document.write(`
        <!DOCTYPE html>
        <html>
        <head>
            <title>${title}</title>
            <style>
                body { font-family: Arial, sans-serif; padding: 20px; color: #333; }
                h2 { text-align: center; margin-bottom: 20px; }
                table { width: 100%; border-collapse: collapse; margin-top: 10px; }
                th, td { border: 1px solid #ddd; padding: 8px 12px; font-size: 12px; text-align: left; }
                th { background-color: #f4f5f7; font-weight: bold; }
                .text-end { text-align: right; }
                .text-center { text-align: center; }
            </style>
        </head>
        <body>
            <h2>Efbis Muhasebe — ${title}</h2>
            <div>${table.outerHTML}</div>
            <div style="margin-top:20px;font-size:11px;color:#777;text-align:right;">
                Tarih: ${new Date().toLocaleString('tr-TR')}
            </div>
        </body>
        </html>
    `);
    printWin.document.close();
    printWin.focus();
    setTimeout(() => {
        printWin.print();
        printWin.close();
    }, 250);
}

// ─── Debounce Helper ───────────────────────────────────────────────────────────
function debounce(func, wait = 300) {
    let timeout;
    return function (...args) {
        clearTimeout(timeout);
        timeout = setTimeout(() => func.apply(this, args), wait);
    };
}

// ─── Toast Notification System ────────────────────────────────────────────────
function showToast(message, type = 'info') {
    const toastEl = document.getElementById('mainToast');
    const toastBody = document.getElementById('toastBody');

    if (!toastEl || !toastBody) return;

    toastEl.className = `toast align-items-center border-0 text-bg-${type === 'error' ? 'danger' : type === 'info' ? 'primary' : type}`;
    toastBody.textContent = message;

    const toast = bootstrap.Toast.getOrCreateInstance(toastEl, { delay: 4000 });
    toast.show();
}

// ─── Format Helpers ───────────────────────────────────────────────────────────
function formatCurrency(value) {
    return new Intl.NumberFormat('tr-TR', {
        style: 'decimal',
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(value || 0) + ' ₺';
}

function formatNumber(value) {
    return new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 0,
        maximumFractionDigits: 2
    }).format(value || 0);
}

function formatDate(dateStr) {
    if (!dateStr) return '-';
    return new Date(dateStr).toLocaleDateString('tr-TR', {
        day: '2-digit', month: '2-digit', year: 'numeric'
    });
}

function getAntiForgeryToken() {
    return document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
}

window.efbisAjax = {
    post: async function(url, data, callback) {
        try {
            if (typeof data === 'function') {
                callback = data;
                data = {};
            }
            const response = await fetch(url, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': getAntiForgeryToken()
                },
                body: JSON.stringify(data || {})
            });
            const json = await response.json();
            if (typeof callback === 'function') callback(json);
            return json;
        } catch (err) {
            console.error('efbisAjax.post error:', err);
        }
    },
    get: async function(url, params, callback) {
        try {
            if (typeof params === 'function') {
                callback = params;
                params = null;
            }
            if (params && typeof params === 'object' && Object.keys(params).length > 0) {
                const query = new URLSearchParams(params).toString();
                if (query) url += (url.includes('?') ? '&' : '?') + query;
            }
            const response = await fetch(url);
            const json = await response.json();
            if (typeof callback === 'function') callback(json);
            return json;
        } catch (err) {
            console.error('efbisAjax.get error:', err);
        }
    }
};
