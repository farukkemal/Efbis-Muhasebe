// ─── Maaş Ödemeleri & Bordro Yönetimi JavaScript Module ─────────────────────
// AJAX Listing, Chart.js Analytics, Pay Slip Modal, Payroll Generator, Bank Export

'use strict';

// ─── State ────────────────────────────────────────────────────────────────────
let currentPage = 1;
let pageSize = 25;
let searchTimeout = null;
let deptChart = null;
let activeEmployees = [];
let cashAccounts = [];
let currentPaymentsData = [];

// ─── Init ──────────────────────────────────────────────────────────────────────
document.addEventListener('DOMContentLoaded', function () {
    const d = new Date();
    if (!document.getElementById('filterYear').value) {
        document.getElementById('filterYear').value = d.getFullYear();
    }
    if (!document.getElementById('filterMonth').value) {
        document.getElementById('filterMonth').value = d.getMonth() + 1;
    }

    refreshData();
    loadEmployeesDropdown();
    loadCashAccountsDropdown();
});

function refreshData() {
    loadDashboard();
    loadData(1);
}

// ─── Dashboard & Chart ────────────────────────────────────────────────────────
async function loadDashboard() {
    const year = document.getElementById('filterYear')?.value || '';
    const month = document.getElementById('filterMonth')?.value || '';

    const params = new URLSearchParams();
    if (year) params.append('year', year);
    if (month) params.append('month', month);

    try {
        const res = await fetch(`/SalaryPayments/GetDashboard?${params}`);
        if (!res.ok) return;
        const data = await res.json();

        const totalPaid = data.TotalPaidAmount ?? data.totalPaidAmount ?? 0;
        const totalPending = data.TotalPendingAmount ?? data.totalPendingAmount ?? 0;
        const paidCount = data.PaidCount ?? data.paidCount ?? 0;
        const pendingCount = data.PendingCount ?? data.pendingCount ?? 0;
        const avgSalary = data.AverageSalary ?? data.averageSalary ?? 0;

        document.getElementById('statTotalAmount').textContent = formatCurrency(totalPaid + totalPending);
        document.getElementById('statPaidCount').textContent = `${paidCount} kayıt`;
        document.getElementById('statPaidAmountLabel').textContent = `Tamamlanan Ödemeler: ${formatCurrency(totalPaid)}`;
        document.getElementById('statPendingCount').textContent = `${pendingCount} kayıt`;
        document.getElementById('statPendingAmountLabel').textContent = `Ödeme Bekleyen: ${formatCurrency(totalPending)}`;
        document.getElementById('statAvgSalary').textContent = formatCurrency(avgSalary);
    } catch (e) {
        console.error('Dashboard load error:', e);
    }
}

// ─── Load Salary Payments List ────────────────────────────────────────────────
async function loadData(page = null) {
    if (page !== null) currentPage = page;

    const year = document.getElementById('filterYear')?.value || '';
    const month = document.getElementById('filterMonth')?.value || '';
    const dept = document.getElementById('filterDepartment')?.value || '';
    const status = document.getElementById('filterStatus')?.value || '';
    const search = document.getElementById('filterSearch')?.value || '';

    const params = new URLSearchParams({
        pageNumber: currentPage,
        pageSize: pageSize,
        searchTerm: search,
        year: year,
        month: month,
        department: dept,
        status: status
    });

    setTableLoading(true);

    try {
        const res = await fetch(`/SalaryPayments/GetPayments?${params}`);
        if (!res.ok) throw new Error('Sunucu hatası');
        const data = await res.json();

        const items = data.Items || data.items || [];
        const totalCount = data.TotalCount ?? data.totalCount ?? 0;

        currentPaymentsData = items;
        renderTable(items);
        renderPagination(totalCount, currentPage, pageSize);
        renderDepartmentChart(items);
        updateDeductionSummary(items);

        document.getElementById('totalBadge').textContent = `${totalCount} kayıt`;
    } catch (e) {
        console.error('Data load error:', e);
        showTableError('Maaş ödemeleri yüklenirken bir hata oluştu.');
    } finally {
        setTableLoading(false);
    }
}

function renderTable(items) {
    const tbody = document.getElementById('tableBody');

    if (!items || items.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="12">
                    <div class="empty-state">
                        <i class="bi bi-wallet-2 fs-1 text-muted mb-2"></i>
                        <h6>Maaş Ödeme Kaydı Bulunamadı</h6>
                        <p class="text-muted" style="font-size:13px;">Seçili yıl, ay veya departman filtrelerine uygun maaş kaydı bulunmamaktadır.</p>
                    </div>
                </td>
            </tr>`;
        return;
    }

    tbody.innerHTML = items.map(item => {
        const id = item.Id ?? item.id;
        const code = item.PaymentCode || item.paymentCode || '';
        const name = item.EmployeeName || item.employeeName || '';
        const empCode = item.EmployeeCode || item.employeeCode || '';
        const deptText = item.DepartmentText || item.departmentText || '';
        const periodText = item.PeriodText || item.periodText || '';
        const gross = item.GrossSalary ?? item.grossSalary ?? 0;
        const tax = item.TaxDeduction ?? item.taxDeduction ?? 0;
        const sgk = item.SgkDeduction ?? item.sgkDeduction ?? 0;
        const other = item.OtherDeductions ?? item.otherDeductions ?? 0;
        const totalDeductions = tax + sgk + other;
        const net = item.NetSalary ?? item.netSalary ?? 0;
        const bonus = item.BonusAmount ?? item.bonusAmount ?? 0;
        const totalPayment = item.TotalPayment ?? item.totalPayment ?? (net + bonus);
        const paymentDate = item.FormattedPaymentDate || item.formattedPaymentDate || formatDate(item.PaymentDate || item.paymentDate);
        const status = item.Status ?? item.status ?? 1;

        return `
        <tr>
            <td><code style="background:#f1f5f9;padding:2px 6px;border-radius:4px;font-size:12px;">${escHtml(code)}</code></td>
            <td>
                <strong style="color:#0f172a;">${escHtml(name)}</strong>
                <div style="font-size:11.5px;color:#64748b;">${escHtml(empCode)}</div>
            </td>
            <td>
                <span class="badge ${getDeptBadgeClass(deptText)}" style="font-size:11.5px;">
                    ${escHtml(deptText)}
                </span>
            </td>
            <td style="font-size:12.5px;font-weight:600;color:#334155;">${escHtml(periodText)}</td>
            <td style="text-align:right;font-size:12.5px;color:#475569;">${formatCurrency(gross)}</td>
            <td style="text-align:right;font-size:12.5px;color:#dc2626;" title="Vergi: ${formatCurrency(tax)} | SGK: ${formatCurrency(sgk)}">
                -${formatCurrency(totalDeductions)}
            </td>
            <td style="text-align:right;font-size:13px;font-weight:600;color:#0f172a;">${formatCurrency(net)}</td>
            <td style="text-align:right;font-size:12.5px;color:${bonus > 0 ? '#16a34a' : '#94a3b8'};font-weight:${bonus > 0 ? '700' : '400'};">
                ${bonus > 0 ? '+' + formatCurrency(bonus) : '—'}
            </td>
            <td style="text-align:right;"><strong style="color:#0d9488;font-size:13.5px;">${formatCurrency(totalPayment)}</strong></td>
            <td style="font-size:12px;color:#64748b;">${paymentDate}</td>
            <td>${getStatusBadge(status)}</td>
            <td style="text-align:center;">
                <div class="d-flex gap-1 justify-content-center">
                    <button class="btn-action btn-action-edit" onclick="openPaySlipModal(${id})" title="Bordro Zarfını İncele / Yazdır">
                        <i class="bi bi-file-earmark-person"></i>
                    </button>
                    <button class="btn-action" style="background:#fef3c7;color:#d97706;border-color:#fde68a;" onclick="openEditPayrollModal(${id})" title="Bordro Düzenle">
                        <i class="bi bi-pencil-square"></i>
                    </button>
                    ${status === 1 ? `
                        <button class="btn-action" style="background:#ecfdf5;color:#10b981;border-color:#a7f3d0;" onclick="markAsPaid(${id})" title="Ödendi İşaretle">
                            <i class="bi bi-check-lg"></i>
                        </button>
                        <button class="btn-action btn-action-delete" onclick="cancelPayment(${id})" title="İptal Et">
                            <i class="bi bi-x-lg"></i>
                        </button>
                    ` : ''}
                    <button class="btn-action btn-action-delete" onclick="deletePayrollRecord(${id})" title="Kayıt Sil">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </td>
        </tr>`;
    }).join('');
}

function getStatusBadge(status) {
    switch (status) {
        case 1:
            return `<span class="badge bg-warning-subtle text-warning" style="font-size:12px;"><i class="bi bi-clock me-1"></i>Beklemede</span>`;
        case 2:
            return `<span class="badge bg-success-subtle text-success" style="font-size:12px;"><i class="bi bi-check-circle me-1"></i>Ödendi</span>`;
        case 3:
            return `<span class="badge bg-secondary-subtle text-secondary" style="font-size:12px;"><i class="bi bi-x-circle me-1"></i>İptal</span>`;
        default:
            return `<span class="badge bg-light text-dark">Bilinmiyor</span>`;
    }
}

function getDeptBadgeClass(deptText) {
    if (deptText.includes('Depo')) return 'bg-primary';
    if (deptText.includes('Kasa')) return 'bg-success';
    if (deptText.includes('Reyon')) return 'bg-warning text-dark';
    if (deptText.includes('Danışman')) return 'bg-info text-dark';
    return 'bg-dark';
}

function updateDeductionSummary(items) {
    let sgkSum = 0, taxSum = 0, bonusSum = 0, netSum = 0;
    items.forEach(i => {
        sgkSum += i.SgkDeduction ?? i.sgkDeduction ?? 0;
        taxSum += i.TaxDeduction ?? i.taxDeduction ?? 0;
        bonusSum += i.BonusAmount ?? i.bonusAmount ?? 0;
        netSum += i.TotalPayment ?? i.totalPayment ?? 0;
    });

    document.getElementById('statSgkSum').textContent = formatCurrency(sgkSum);
    document.getElementById('statTaxSum').textContent = formatCurrency(taxSum);
    document.getElementById('statBonusSum').textContent = '+' + formatCurrency(bonusSum);
    document.getElementById('statNetSum').textContent = formatCurrency(netSum);
}

// ─── Chart.js Analytics ───────────────────────────────────────────────────────
function renderDepartmentChart(items) {
    const ctx = document.getElementById('salaryDeptChart');
    if (!ctx) return;

    const deptTotals = {};
    items.forEach(i => {
        const dept = i.DepartmentText || i.departmentText || 'Diğer';
        const amt = i.TotalPayment ?? i.totalPayment ?? 0;
        deptTotals[dept] = (deptTotals[dept] || 0) + amt;
    });

    const labels = Object.keys(deptTotals);
    const dataValues = Object.values(deptTotals);

    if (deptChart) {
        deptChart.destroy();
    }

    deptChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Toplam Ödenen Net Maaş (₺)',
                data: dataValues,
                backgroundColor: ['#6366f1', '#10b981', '#f59e0b', '#0891b2', '#8b5cf6'],
                borderRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        callback: function (value) {
                            return (value / 1000) + ' Bin ₺';
                        }
                    }
                }
            }
        }
    });
}

// ─── Bordro Zarfı (Pay Slip Modal) ─────────────────────────────────────────────
async function openPaySlipModal(id) {
    try {
        const res = await fetch(`/SalaryPayments/GetDetail/${id}`);
        if (!res.ok) throw new Error('Bordro detayı alınamadı');
        const p = await res.json();

        const name = p.EmployeeName || p.employeeName || '';
        const empCode = p.EmployeeCode || p.employeeCode || '';
        const dept = p.DepartmentText || p.departmentText || '';
        const period = p.PeriodText || p.periodText || '';
        const code = p.PaymentCode || p.paymentCode || '';
        const gross = p.GrossSalary ?? p.grossSalary ?? 0;
        const tax = p.TaxDeduction ?? p.taxDeduction ?? 0;
        const sgk = p.SgkDeduction ?? p.sgkDeduction ?? 0;
        const other = p.OtherDeductions ?? p.otherDeductions ?? 0;
        const bonus = p.BonusAmount ?? p.bonusAmount ?? 0;
        const net = p.NetSalary ?? p.netSalary ?? 0;
        const total = p.TotalPayment ?? p.totalPayment ?? (net + bonus);
        const paymentDate = p.FormattedPaymentDate || p.formattedPaymentDate || 'Ödenmedi';

        const html = `
        <div class="border rounded p-4 bg-white shadow-sm" id="printablePaySlip">
            <div class="d-flex justify-content-between align-items-center border-bottom pb-3 mb-3">
                <div>
                    <h5 class="fw-bold text-primary mb-1">EFBİS KURUMSAL PERSONEL BORDRO ZARFI</h5>
                    <div class="text-muted small">Resmi Personel Ücret Bordro Zarfı — ${escHtml(period)}</div>
                </div>
                <div class="text-end">
                    <span class="badge bg-dark fs-6 mb-1">${escHtml(code)}</span>
                    <div class="text-muted small">Tarih: ${paymentDate}</div>
                </div>
            </div>

            <!-- Personel Bilgileri Tablosu -->
            <div class="row g-3 mb-4">
                <div class="col-md-6">
                    <table class="table table-sm table-borderless mb-0">
                        <tr><td class="text-muted" style="width:130px;">Personel Kodu:</td><td><strong>${escHtml(empCode)}</strong></td></tr>
                        <tr><td class="text-muted">Adı Soyadı:</td><td><strong>${escHtml(name)}</strong></td></tr>
                        <tr><td class="text-muted">Departman:</td><td><span class="badge bg-secondary">${escHtml(dept)}</span></td></tr>
                    </table>
                </div>
                <div class="col-md-6">
                    <table class="table table-sm table-borderless mb-0">
                        <tr><td class="text-muted" style="width:130px;">İş Yeri:</td><td>Genel Merkez / Şube</td></tr>
                        <tr><td class="text-muted">Bordro Dönemi:</td><td><strong>${escHtml(period)}</strong></td></tr>
                        <tr><td class="text-muted">Ödeme Durumu:</td><td>${getStatusBadge(p.Status ?? p.status ?? 1)}</td></tr>
                    </table>
                </div>
            </div>

            <!-- Kazanç ve Kesintiler Hesap Cetveli -->
            <div class="row g-3 mb-4">
                <div class="col-md-6">
                    <div class="card h-100 border-success border-2">
                        <div class="card-header bg-success text-white fw-bold py-2">
                            <i class="bi bi-plus-circle me-1"></i>HAK EDİŞLER & HESAPLAMALAR
                        </div>
                        <div class="card-body p-3">
                            <div class="d-flex justify-content-between mb-2">
                                <span>Aylık Brüt Ücret:</span>
                                <strong>${formatCurrency(gross)}</strong>
                            </div>
                            <div class="d-flex justify-content-between mb-2">
                                <span>Aylık Net Maaş Hakedişi:</span>
                                <strong>${formatCurrency(net)}</strong>
                            </div>
                            <div class="d-flex justify-content-between mb-2 text-success">
                                <span>Ek Prim / İkramiye:</span>
                                <strong>+${formatCurrency(bonus)}</strong>
                            </div>
                            <hr class="my-2" />
                            <div class="d-flex justify-content-between fw-bold text-dark fs-6">
                                <span>Toplam Brüt Hak Ediş:</span>
                                <span>${formatCurrency(gross + bonus)}</span>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="col-md-6">
                    <div class="card h-100 border-danger border-2">
                        <div class="card-header bg-danger text-white fw-bold py-2">
                            <i class="bi bi-dash-circle me-1"></i>YASAL KESİNTİLER (DEDUCTIONS)
                        </div>
                        <div class="card-body p-3">
                            <div class="d-flex justify-content-between mb-2 text-danger">
                                <span>SGK İşçi Payı (%14):</span>
                                <span>-${formatCurrency(sgk)}</span>
                            </div>
                            <div class="d-flex justify-content-between mb-2 text-danger">
                                <span>Gelir Vergisi (%15):</span>
                                <span>-${formatCurrency(tax)}</span>
                            </div>
                            <div class="d-flex justify-content-between mb-2 text-danger">
                                <span>Diğer Kesintiler / Avans:</span>
                                <span>-${formatCurrency(other)}</span>
                            </div>
                            <hr class="my-2" />
                            <div class="d-flex justify-content-between fw-bold text-danger fs-6">
                                <span>Toplam Yasal Kesinti:</span>
                                <span>-${formatCurrency(tax + sgk + other)}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Ele Geçen Net Tutar Vurgusu -->
            <div class="p-3 rounded text-white d-flex justify-content-between align-items-center" style="background: linear-gradient(135deg, #0d9488, #0f766e);">
                <div>
                    <div class="text-white-50 small">PERSONELE ÖDENEN NET TUTAR</div>
                    <div class="fs-6 fw-bold">Banka Hesabına Aktarılan Net Bakiye</div>
                </div>
                <div class="fs-2 fw-bold">${formatCurrency(total)}</div>
            </div>
        </div>`;

        document.getElementById('paySlipContent').innerHTML = html;
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('paySlipModal'));
        modal.show();
    } catch (e) {
        showToast('Bordro zarfı yüklenirken hata oluştu.', 'error');
    }
}

function printPaySlip() {
    const content = document.getElementById('printablePaySlip').innerHTML;
    const printWin = window.open('', '', 'width=900,height=700');
    printWin.document.write(`
        <html>
            <head>
                <title>Personel Bordro Zarfı</title>
                <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" />
            </head>
            <body class="p-4" onload="window.print(); window.close();">
                ${content}
            </body>
        </html>
    `);
    printWin.document.close();
}

// ─── Modal Actions & CRUD ─────────────────────────────────────────────────────
function generatePayrollModal() {
    bootstrap.Modal.getOrCreateInstance(document.getElementById('generatePayrollModal')).show();
}

async function confirmGeneratePayroll() {
    const year = parseInt(document.getElementById('genYear').value);
    const month = parseInt(document.getElementById('genMonth').value);

    try {
        const result = await efbisAjax.post('/SalaryPayments/GeneratePayroll', { Year: year, Month: month });
        if (result.success || result.generatedCount >= 0) {
            showToast(`${result.generatedCount || 0} personel için aylık bordro başarıyla oluşturuldu.`, 'success');
            bootstrap.Modal.getInstance(document.getElementById('generatePayrollModal'))?.hide();
            refreshData();
        } else {
            showToast('Bordro oluşturulurken hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('Bordro oluşturulamadı.', 'error');
    }
}

function bulkPayModal() {
    const year = document.getElementById('filterYear').value;
    const month = document.getElementById('filterMonth').value;

    if (!year || !month) {
        showToast('Lütfen toplu ödeme yapmak için Yıl ve Ay filtrelerini seçiniz.', 'warning');
        return;
    }

    if (!confirm(`${year} yılı ${month}. ayındaki bekleyen tüm maaş ödemelerini "Ödendi" olarak tamamlamak istediğinize emin misiniz?`)) return;

    efbisAjax.post('/SalaryPayments/BulkPay', { Year: parseInt(year), Month: parseInt(month) })
        .then(result => {
            showToast(`${result.paidCount || 0} adet maaş ödemesi başarıyla tamamlandı.`, 'success');
            refreshData();
        })
        .catch(() => showToast('Toplu ödeme sırasında bir hata oluştu.', 'error'));
}

async function markAsPaid(id) {
    if (!confirm('Bu maaş ödemesini "Ödendi" olarak işaretlemek istiyor musunuz?')) return;

    try {
        const result = await efbisAjax.post(`/SalaryPayments/MarkAsPaid/${id}`, {});
        if (result.success) {
            showToast('Maaş ödemesi başarıyla tamamlandı.', 'success');
            refreshData();
        } else {
            showToast('Ödeme tamamlanamadı.', 'error');
        }
    } catch (e) {
        showToast('İşlem sırasında hata oluştu.', 'error');
    }
}

async function cancelPayment(id) {
    if (!confirm('Bu maaş ödeme kaydını iptal etmek istediğinize emin misiniz?')) return;

    try {
        const result = await efbisAjax.post(`/SalaryPayments/Cancel/${id}`, {});
        if (result.success) {
            showToast('Maaş ödeme kaydı iptal edildi.', 'info');
            refreshData();
        } else {
            showToast('İptal işlemi başarısız.', 'error');
        }
    } catch (e) {
        showToast('İşlem sırasında hata oluştu.', 'error');
    }
}

// ─── Custom Payment Modal ─────────────────────────────────────────────────────
async function loadEmployeesDropdown() {
    try {
        const res = await fetch('/SalaryPayments/GetEmployees');
        if (!res.ok) return;
        activeEmployees = await res.json();
        const select = document.getElementById('newEmpId');
        if (!select) return;
        select.innerHTML = '<option value="">-- Personel Seçiniz --</option>';
        activeEmployees.forEach(emp => {
            const id = emp.Id ?? emp.id;
            const name = emp.FullName || emp.fullName || `${emp.FirstName} ${emp.LastName}`;
            const code = emp.EmployeeCode || emp.employeeCode;
            select.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)})</option>`;
        });
    } catch (e) {
        console.error('Employees dropdown error:', e);
    }
}

async function loadCashAccountsDropdown() {
    try {
        const res = await fetch('/SalaryPayments/GetCashAccounts');
        if (!res.ok) return;
        cashAccounts = await res.json();
        const select = document.getElementById('newCashAccountId');
        if (!select) return;
        select.innerHTML = '<option value="">-- Ödeme Anında Seçilecek --</option>';
        cashAccounts.forEach(acc => {
            const id = acc.Id ?? acc.id;
            const name = acc.AccountName || acc.accountName;
            const code = acc.AccountCode || acc.accountCode;
            select.innerHTML += `<option value="${id}">${escHtml(name)} (${escHtml(code)})</option>`;
        });
    } catch (e) {
        console.error('Cash accounts dropdown error:', e);
    }
}

function openCreatePaymentModal() {
    document.getElementById('createPaymentForm').reset();
    recalcDeductions();
    bootstrap.Modal.getOrCreateInstance(document.getElementById('createPaymentModal')).show();
}

function autoFillSalary() {
    const empId = parseInt(document.getElementById('newEmpId').value);
    if (!empId) return;

    const emp = activeEmployees.find(e => (e.Id ?? e.id) === empId);
    if (emp && (emp.Salary || emp.salary)) {
        document.getElementById('newNetSalary').value = emp.Salary || emp.salary;
        recalcDeductions();
    }
}

function recalcDeductions() {
    const net = parseFloat(document.getElementById('newNetSalary').value) || 0;
    const bonus = parseFloat(document.getElementById('newBonus').value) || 0;

    const gross = Math.round(net * 1.42);
    const tax = Math.round(net * 0.15);
    const sgk = Math.round(net * 0.14);

    document.getElementById('newGrossSalary').value = gross;
    document.getElementById('newTax').value = tax;
    document.getElementById('newSgk').value = sgk;

    const total = net + bonus;
    document.getElementById('newTotalPaymentDisplay').textContent = formatCurrency(total);
}

async function saveCustomPayment() {
    const empId = parseInt(document.getElementById('newEmpId').value);
    const year = parseInt(document.getElementById('newYear').value);
    const month = parseInt(document.getElementById('newMonth').value);
    const net = parseFloat(document.getElementById('newNetSalary').value) || 0;
    const gross = parseFloat(document.getElementById('newGrossSalary').value) || 0;
    const bonus = parseFloat(document.getElementById('newBonus').value) || 0;
    const tax = parseFloat(document.getElementById('newTax').value) || 0;
    const sgk = parseFloat(document.getElementById('newSgk').value) || 0;
    const other = parseFloat(document.getElementById('newOtherDeductions').value) || 0;
    const desc = document.getElementById('newDescription').value.trim();
    const cashAccIdVal = document.getElementById('newCashAccountId').value;

    if (!empId || !net) {
        showToast('Lütfen personel seçiniz ve net maaş giriniz.', 'warning');
        return;
    }

    const dto = {
        EmployeeId: empId,
        Year: year,
        Month: month,
        GrossSalary: gross,
        NetSalary: net,
        TaxDeduction: tax,
        SgkDeduction: sgk,
        OtherDeductions: other,
        BonusAmount: bonus,
        Description: desc || null,
        CashAccountId: cashAccIdVal ? parseInt(cashAccIdVal) : null
    };

    try {
        const res = await efbisAjax.post('/SalaryPayments/Create', dto);
        if (res.success || res.data) {
            showToast('Özel maaş / prim kaydı oluşturuldu.', 'success');
            bootstrap.Modal.getInstance(document.getElementById('createPaymentModal'))?.hide();
            refreshData();
        } else {
            showToast('Maaş kaydı oluşturulamadı.', 'error');
        }
    } catch (e) {
        showToast('Maaş kaydı oluşturulurken hata meydana geldi.', 'error');
    }
}

// ─── Bank EFT/Havale Export ───────────────────────────────────────────────────
function downloadBankTransferFile() {
    if (!currentPaymentsData || currentPaymentsData.length === 0) {
        showToast('İndirilecek maaş kaydı bulunmamaktadır.', 'warning');
        return;
    }

    let csvContent = "data:text/csv;charset=utf-8,";
    csvContent += "Banka_Transfer_Kodu;Personel_Kodu;Personel_Adi;Departman;Donem;Net_Odenecek_Tutar;Tarih\n";

    currentPaymentsData.forEach(p => {
        const code = p.PaymentCode || p.paymentCode || '';
        const empCode = p.EmployeeCode || p.employeeCode || '';
        const name = p.EmployeeName || p.employeeName || '';
        const dept = p.DepartmentText || p.departmentText || '';
        const period = p.PeriodText || p.periodText || '';
        const amt = (p.TotalPayment ?? p.totalPayment ?? 0).toFixed(2);
        const date = p.FormattedPaymentDate || p.formattedPaymentDate || '';

        csvContent += `${code};${empCode};"${name}";${dept};${period};${amt};${date}\n`;
    });

    const encodedUri = encodeURI(csvContent);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `Banka_Maas_Transfer_Listesi_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showToast('Banka transfer listesi (CSV) bilgisayarınıza indirildi.', 'success');
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
        <tr><td colspan="12" class="text-center py-4 text-danger">
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
    return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

function renderPagination(totalCount, pageNumber, pgSize) {
    const totalPages = Math.ceil(totalCount / pgSize);
    const info = document.getElementById('paginationInfo');
    const buttons = document.getElementById('paginationControls');

    const from = totalCount === 0 ? 0 : ((pageNumber - 1) * pgSize + 1);
    const to = Math.min(pageNumber * pgSize, totalCount);
    info.textContent = totalCount === 0 ? 'Kayıt bulunamadı' : `${from}–${to} / ${totalCount} kayıt gösteriliyor`;

    if (totalPages <= 1) {
        buttons.innerHTML = '';
        return;
    }

    let html = `<button class="page-btn" ${pageNumber <= 1 ? 'disabled' : ''} onclick="loadData(${pageNumber - 1})"><i class="bi bi-chevron-left"></i></button>`;

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

    html += `<button class="page-btn" ${pageNumber >= totalPages ? 'disabled' : ''} onclick="loadData(${pageNumber + 1})"><i class="bi bi-chevron-right"></i></button>`;

    buttons.innerHTML = html;
}

// ─── Edit & Delete Payroll Modal Logic ─────────────────────────────────────
async function openEditPayrollModal(id) {
    const item = currentPaymentsData.find(x => (x.id ?? x.Id) === id);
    if (!item) {
        showToast('Bordro kaydı bulunamadı', 'warning');
        return;
    }

    document.getElementById('editId').value = item.id ?? item.Id;
    document.getElementById('editEmployeeName').value = (item.employeeName || item.EmployeeName || '') + ' (' + (item.employeeCode || item.EmployeeCode || '') + ')';
    document.getElementById('editYear').value = item.year ?? item.Year;
    document.getElementById('editMonth').value = item.month ?? item.Month;

    const net = item.netSalary ?? item.NetSalary ?? 0;
    const gross = item.grossSalary ?? item.GrossSalary ?? Math.round(net * 1.42);
    const bonus = item.bonusAmount ?? item.BonusAmount ?? 0;
    const tax = item.taxDeduction ?? item.TaxDeduction ?? Math.round(gross * 0.15);
    const sgk = item.sgkDeduction ?? item.SgkDeduction ?? Math.round(gross * 0.14);
    const other = item.otherDeductions ?? item.OtherDeductions ?? 0;
    const status = item.status ?? item.Status ?? 1;
    const desc = item.description || item.Description || '';
    const cashAccId = item.cashAccountId ?? item.CashAccountId ?? '';

    document.getElementById('editNetSalary').value = net;
    document.getElementById('editGrossSalary').value = gross;
    document.getElementById('editBonus').value = bonus;
    document.getElementById('editTax').value = tax;
    document.getElementById('editSgk').value = sgk;
    document.getElementById('editOtherDeductions').value = other;
    document.getElementById('editStatus').value = status;
    document.getElementById('editDescription').value = desc;

    // Populate Cash Accounts dropdown
    const cashSelect = document.getElementById('editCashAccountId');
    if (cashSelect) {
        cashSelect.innerHTML = '<option value="">-- Kasa Hesabı Seçilmedi --</option>' +
            cashAccounts.map(c => `<option value="${c.id || c.Id}" ${c.id == cashAccId ? 'selected' : ''}>${escHtml(c.accountName || c.AccountName)} (${formatCurrency(c.balance || c.Balance)})</option>`).join('');
    }

    recalcEditDeductions();
    new bootstrap.Modal(document.getElementById('editPayrollModal')).show();
}

function recalcEditDeductions() {
    const net = parseFloat(document.getElementById('editNetSalary').value) || 0;
    const bonus = parseFloat(document.getElementById('editBonus').value) || 0;
    const gross = parseFloat(document.getElementById('editGrossSalary').value) || Math.round(net * 1.42);
    const tax = parseFloat(document.getElementById('editTax').value) || Math.round(gross * 0.15);
    const sgk = parseFloat(document.getElementById('editSgk').value) || Math.round(gross * 0.14);

    const total = net + bonus;
    const disp = document.getElementById('editTotalPaymentDisplay');
    if (disp) disp.textContent = formatCurrency(total);
}

async function submitEditPayroll() {
    const id = parseInt(document.getElementById('editId').value, 10);
    const net = parseFloat(document.getElementById('editNetSalary').value) || 0;
    const gross = parseFloat(document.getElementById('editGrossSalary').value) || 0;
    const bonus = parseFloat(document.getElementById('editBonus').value) || 0;
    const tax = parseFloat(document.getElementById('editTax').value) || 0;
    const sgk = parseFloat(document.getElementById('editSgk').value) || 0;
    const other = parseFloat(document.getElementById('editOtherDeductions').value) || 0;
    const status = parseInt(document.getElementById('editStatus').value, 10);
    const desc = document.getElementById('editDescription').value.trim();
    const cashId = document.getElementById('editCashAccountId').value;

    const dto = {
        id: id,
        netSalary: net,
        grossSalary: gross,
        bonusAmount: bonus,
        taxDeduction: tax,
        sgkDeduction: sgk,
        otherDeductions: other,
        status: status,
        description: desc,
        cashAccountId: cashId ? parseInt(cashId, 10) : null
    };

    try {
        const res = await fetch('/SalaryPayments/Update', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(dto)
        });
        const d = await res.json();
        if (d && d.success) {
            showToast('Maaş bordrosu başarıyla güncellendi', 'success');
            bootstrap.Modal.getInstance(document.getElementById('editPayrollModal'))?.hide();
            refreshData();
        } else {
            showToast((d && d.message) || 'Güncelleme hatası', 'error');
        }
    } catch (e) {
        showToast('Sunucu hatası', 'error');
    }
}

async function deletePayrollRecord(id) {
    if (!confirm('Bu maaş bordro kaydını silmek istediğinize emin misiniz?')) return;
    try {
        const res = await fetch(`/SalaryPayments/Delete?id=${id}`, { method: 'POST' });
        const d = await res.json();
        if (d && d.success) {
            showToast('Bordro kaydı silindi', 'success');
            refreshData();
        } else {
            showToast((d && d.message) || 'Silme hatası', 'error');
        }
    } catch (e) {
        showToast('Sunucu hatası', 'error');
    }
}
