// ─── Vardiya Takip & Planlama JavaScript Module ──────────────────────────────
// List & Weekly Roster Views, Chart.js Analytics, Check-In/Check-Out, Auto Schedule

'use strict';

let currentPage = 1;
let pageSize = 10;
let shiftModal;
let autoScheduleModal;
let shiftTypeChart = null;
let currentShifts = [];
let rosterShifts = [];
let allEmployees = [];
let rosterOffset = 0; // week offset for roster grid

$(document).ready(function () {
    init();
});

async function init() {
    shiftModal = new bootstrap.Modal(document.getElementById('shiftModal'));
    autoScheduleModal = new bootstrap.Modal(document.getElementById('autoScheduleModal'));

    // Default date range for list view: today - 6 days to today + 1 day
    const today = new Date();
    const startDate = new Date();
    startDate.setDate(today.getDate() - 6);

    document.getElementById('filterStartDate').value = startDate.toISOString().slice(0, 10);
    document.getElementById('filterEndDate').value = new Date(today.getTime() + 86400000).toISOString().slice(0, 10);

    await loadEmployeesDropdown();
    await loadDashboard();
    await loadData(1);

    if ($('#roster-tab').hasClass('active')) {
        renderWeeklyRosterGrid();
    }

    // Event Listeners
    $('#btnNewShift').click(openCreateModal);
    $('#btnRefresh').click(async () => {
        await loadDashboard();
        await loadData(currentPage);
        if ($('#roster-tab').hasClass('active')) {
            renderWeeklyRosterGrid();
        }
    });
    $('#btnSave').click(saveShift);

    let searchTimeout;
    $('#filterSearch').on('input', function () {
        clearTimeout(searchTimeout);
        searchTimeout = setTimeout(() => loadData(1), 400);
    });

    $('.filter-select, #filterDepartment').change(() => {
        loadData(1);
        if ($('#roster-tab').hasClass('active')) {
            renderWeeklyRosterGrid();
        }
    });

    $('#shiftType').change(function () {
        setDefaultTimes($(this).val());
    });
}

async function loadDashboard() {
    try {
        const response = await efbisAjax.get('/Shifts/GetDashboard');
        if (response && response.success && response.data) {
            const data = response.data;
            $('#statTodayShifts').text(data.todayShifts ?? data.TodayShifts ?? 0);
            $('#statActiveNow').text(data.activeNow ?? data.ActiveNow ?? 0);
            $('#statCompletedToday').text(data.completedToday ?? data.CompletedToday ?? 0);
            $('#statAbsentToday').text(data.absentToday ?? data.AbsentToday ?? 0);

            const overtime = data.totalOvertimeHours ?? data.TotalOvertimeHours ?? 0;
            $('#statTotalOvertimeHours').text(`${overtime} Sa`);
        }
    } catch (e) {
        console.error('loadDashboard error:', e);
    }
}

function populateEmployeeDropdownSelect() {
    const select = $('#employeeId');
    if (!select.length) return;
    select.empty().append('<option value="">-- Personel Seçiniz --</option>');
    if (allEmployees && allEmployees.length > 0) {
        allEmployees.forEach(emp => {
            const id = emp.id ?? emp.Id;
            const name = emp.fullName ?? emp.FullName ?? `${emp.firstName ?? emp.FirstName} ${emp.lastName ?? emp.LastName}`;
            const dept = emp.departmentText ?? emp.DepartmentText ?? emp.department ?? emp.Department;
            select.append(`<option value="${id}">${escHtml(name)} (${escHtml(dept)})</option>`);
        });
    }
}

async function loadEmployeesDropdown() {
    try {
        const response = await efbisAjax.get('/Shifts/GetEmployees');
        if (response && response.success && response.data) {
            allEmployees = response.data;
            populateEmployeeDropdownSelect();
        }
    } catch (e) {
        console.error('loadEmployeesDropdown error:', e);
    }
}

async function loadData(page) {
    currentPage = page;

    const filter = {
        pageNumber: page,
        pageSize: pageSize,
        searchTerm: $('#filterSearch').val(),
        department: $('#filterDepartment').val(),
        shiftType: $('#filterShiftType').val(),
        status: $('#filterStatus').val(),
        startDate: $('#filterStartDate').val(),
        endDate: $('#filterEndDate').val()
    };

    const query = new URLSearchParams(filter).toString();
    try {
        const response = await efbisAjax.get(`/Shifts/GetShifts?${query}`);
        if (response && response.success && response.data) {
            const items = response.data.items || response.data.Items || [];
            currentShifts = items;
            renderTable(items);
            renderPagination(response.data);
            renderShiftChart(items);
        }
    } catch (e) {
        console.error('loadData error:', e);
    }
}

function renderTable(items) {
    const tbody = $('#shiftsTableBody');
    tbody.empty();

    if (!items || items.length === 0) {
        $('#shiftsTable').addClass('d-none');
        $('#emptyState').removeClass('d-none');
        return;
    }

    $('#shiftsTable').removeClass('d-none');
    $('#emptyState').addClass('d-none');

    items.forEach(item => {
        const id = item.id ?? item.Id;
        const code = item.shiftCode ?? item.ShiftCode ?? '';
        const name = item.employeeName ?? item.EmployeeName ?? '';
        const dept = item.departmentText ?? item.DepartmentText ?? '';
        const date = item.formattedDate ?? item.FormattedDate ?? '';
        const typeText = item.shiftTypeText ?? item.ShiftTypeText ?? '';
        const planned = `${item.formattedStartTime ?? item.FormattedStartTime ?? ''} - ${item.formattedEndTime ?? item.FormattedEndTime ?? ''}`;

        const actualStart = item.formattedActualStart ?? item.FormattedActualStart ?? '—';
        const actualEnd = item.formattedActualEnd ?? item.FormattedActualEnd ?? '—';
        const actual = `${actualStart} / ${actualEnd}`;

        const overtime = item.overtimeHours ?? item.OvertimeHours ?? 0;
        const overtimeText = overtime > 0 ? `<span class="text-danger fw-bold">+${overtime} sa</span>` : '<span class="text-muted">—</span>';
        const statusBadge = getStatusBadge(item.status ?? item.Status);
        const actions = getActionButtons(item);

        tbody.append(`
            <tr>
                <td><code style="background:#f1f5f9;padding:2px 6px;border-radius:4px;font-size:12px;">${escHtml(code)}</code></td>
                <td><strong style="color:#0f172a;">${escHtml(name)}</strong></td>
                <td><span class="badge ${getDeptBadgeClass(dept)}" style="font-size:11.5px;">${escHtml(dept)}</span></td>
                <td style="font-size:12.5px;font-weight:600;color:#334155;">${date}</td>
                <td><span class="badge bg-secondary" style="font-size:11.5px;">${escHtml(typeText)}</span></td>
                <td style="font-size:12.5px;">${planned}</td>
                <td style="font-size:12.5px;">${actual}</td>
                <td style="font-size:12.5px;">${overtimeText}</td>
                <td>${statusBadge}</td>
                <td style="text-align:center;">${actions}</td>
            </tr>
        `);
    });
}

function getStatusBadge(status) {
    switch (status) {
        case 1: return '<span class="badge bg-info-subtle text-info" style="font-size:12px;"><i class="bi bi-clock me-1"></i>Planlandı</span>';
        case 2: return '<span class="badge bg-primary" style="font-size:12px;"><i class="bi bi-play-circle me-1"></i>Aktif</span>';
        case 3: return '<span class="badge bg-success-subtle text-success" style="font-size:12px;"><i class="bi bi-check-circle me-1"></i>Tamamlandı</span>';
        case 4: return '<span class="badge bg-danger-subtle text-danger" style="font-size:12px;"><i class="bi bi-x-circle me-1"></i>Devamsız</span>';
        case 5: return '<span class="badge bg-secondary-subtle text-secondary" style="font-size:12px;">İptal</span>';
        default: return '<span class="badge bg-light text-dark">Bilinmiyor</span>';
    }
}

function getDeptBadgeClass(deptText) {
    if (!deptText) return 'bg-dark';
    if (deptText.includes('Depo')) return 'bg-primary';
    if (deptText.includes('Kasa')) return 'bg-success';
    if (deptText.includes('Reyon')) return 'bg-warning text-dark';
    if (deptText.includes('Danışman')) return 'bg-info text-dark';
    return 'bg-dark';
}

function getActionButtons(item) {
    const id = item.id ?? item.Id;
    const status = item.status ?? item.Status;
    let btns = '<div class="d-flex gap-1 justify-content-center">';

    if (status === 1) { // Planned
        btns += `<button class="btn-action text-primary" style="background:#eff6ff;border-color:#bfdbfe;" onclick="checkIn(${id})" title="Giriş Yap"><i class="bi bi-box-arrow-in-right"></i></button>`;
        btns += `<button class="btn-action text-danger" style="background:#fef2f2;border-color:#fecaca;" onclick="markAbsent(${id})" title="Devamsız Yaz"><i class="bi bi-person-x"></i></button>`;
    } else if (status === 2) { // Active
        btns += `<button class="btn-action text-success fw-bold" style="background:#ecfdf5;border-color:#a7f3d0;" onclick="checkOut(${id})" title="Çıkış Yap"><i class="bi bi-box-arrow-right"></i></button>`;
    }

    btns += `
        <button class="btn-action btn-action-edit" onclick="openEditModal(${id})" title="Düzenle"><i class="bi bi-pencil"></i></button>
        <button class="btn-action btn-action-delete" onclick="deleteShift(${id})" title="Sil"><i class="bi bi-trash"></i></button>
    </div>`;
    return btns;
}

// ─── Quick Date Filters ────────────────────────────────────────────────────────
function setQuickDate(type) {
    const today = new Date();
    let start = new Date(today);
    let end = new Date(today);

    if (type === 'today') {
        // start and end are today
    } else if (type === 'thisWeek') {
        const dayOfWeek = today.getDay() === 0 ? 7 : today.getDay();
        start.setDate(today.getDate() - dayOfWeek + 1);
        end.setDate(start.getDate() + 6);
    } else if (type === 'nextWeek') {
        const dayOfWeek = today.getDay() === 0 ? 7 : today.getDay();
        start.setDate(today.getDate() - dayOfWeek + 8);
        end.setDate(start.getDate() + 6);
    } else if (type === 'thisMonth') {
        start = new Date(today.getFullYear(), today.getMonth(), 1);
        end = new Date(today.getFullYear(), today.getMonth() + 1, 0);
    }

    document.getElementById('filterStartDate').value = start.toISOString().slice(0, 10);
    document.getElementById('filterEndDate').value = end.toISOString().slice(0, 10);

    loadData(1);
}

function clearShiftFilters() {
    $('#filterSearch').val('');
    $('#filterDepartment').val('');
    $('#filterShiftType').val('');
    $('#filterStatus').val('');

    const today = new Date();
    const startDate = new Date();
    startDate.setDate(today.getDate() - 6);

    document.getElementById('filterStartDate').value = startDate.toISOString().slice(0, 10);
    document.getElementById('filterEndDate').value = new Date(today.getTime() + 86400000).toISOString().slice(0, 10);

    loadData(1);
}

// ─── Chart.js Analytics ───────────────────────────────────────────────────────
function renderShiftChart(items) {
    const ctx = document.getElementById('shiftTypeChart');
    if (!ctx) return;

    const counts = { 'Sabah': 0, 'Öğle': 0, 'Akşam': 0, 'Tam Gün': 0, 'Yarım Gün': 0 };

    items.forEach(i => {
        const text = i.shiftTypeText ?? i.ShiftTypeText ?? '';
        if (text.includes('Sabah')) counts['Sabah']++;
        else if (text.includes('Öğle')) counts['Öğle']++;
        else if (text.includes('Akşam')) counts['Akşam']++;
        else if (text.includes('Tam')) counts['Tam Gün']++;
        else if (text.includes('Yarım')) counts['Yarım Gün']++;
    });

    if (shiftTypeChart) {
        shiftTypeChart.destroy();
    }

    shiftTypeChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: Object.keys(counts),
            datasets: [{
                data: Object.values(counts),
                backgroundColor: ['#2563eb', '#10b981', '#f59e0b', '#0891b2', '#64748b']
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { position: 'right' }
            }
        }
    });
}

// ─── Weekly Roster Grid View & Navigation ─────────────────────────────────────
function navigateRosterWeek(offsetChange) {
    if (offsetChange === 0) rosterOffset = 0;
    else rosterOffset += offsetChange;

    renderWeeklyRosterGrid();
}

async function renderWeeklyRosterGrid() {
    const container = document.getElementById('rosterGridContainer');
    if (!container) return;

    if (!allEmployees || allEmployees.length === 0) {
        await loadEmployeesDropdown();
    }

    const today = new Date();
    const dayOfWeek = today.getDay() === 0 ? 7 : today.getDay();
    const monday = new Date(today);
    monday.setDate(today.getDate() - dayOfWeek + 1 + (rosterOffset * 7));

    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);

    const mondayStr = monday.toISOString().slice(0, 10);
    const sundayStr = sunday.toISOString().slice(0, 10);

    const weekTitleEl = document.getElementById('rosterWeekTitle');
    if (weekTitleEl) {
        const formatOpts = { day: '2-digit', month: 'long', year: 'numeric' };
        weekTitleEl.textContent = `${monday.toLocaleDateString('tr-TR', formatOpts)} – ${sunday.toLocaleDateString('tr-TR', formatOpts)}`;
    }

    const filter = {
        pageNumber: 1,
        pageSize: 1000,
        startDate: mondayStr,
        endDate: sundayStr,
        department: $('#filterDepartment').val() || ''
    };

    container.innerHTML = `
        <div class="text-center py-5">
            <div class="spinner-efbis mx-auto d-block mb-2"></div>
            <span class="text-muted">Haftalık çalışma çizelgesi yükleniyor...</span>
        </div>`;

    try {
        const query = new URLSearchParams(filter).toString();
        const response = await efbisAjax.get(`/Shifts/GetShifts?${query}`);
        if (response && response.success && response.data) {
            rosterShifts = response.data.items || response.data.Items || [];
        } else {
            rosterShifts = [];
        }
    } catch (e) {
        rosterShifts = [];
    }

    buildRosterGridTable(container, monday, today);
}

function buildRosterGridTable(container, monday, today) {
    const days = [];
    for (let i = 0; i < 7; i++) {
        const d = new Date(monday);
        d.setDate(monday.getDate() + i);
        days.push(d);
    }

    const selectedDept = $('#filterDepartment').val() || '';
    let filteredEmps = allEmployees;
    if (selectedDept) {
        filteredEmps = allEmployees.filter(e => {
            const d = e.department ?? e.Department;
            return String(d) === selectedDept;
        });
    }

    const daysHeader = days.map(d => `
        <th style="text-align:center;min-width:130px;" class="${d.toDateString() === today.toDateString() ? 'table-primary fw-bold' : ''}">
            ${d.toLocaleDateString('tr-TR', { weekday: 'short' })}<br />
            <span class="fw-normal text-muted" style="font-size:11px;">${d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' })}</span>
        </th>
    `).join('');

    let html = `
    <table class="efbis-table table-bordered mb-0">
        <thead class="bg-light">
            <tr>
                <th style="width:220px;">Personel</th>
                ${daysHeader}
            </tr>
        </thead>
        <tbody>`;

    if (filteredEmps.length === 0) {
        html += `<tr><td colspan="8" class="text-center py-4 text-muted">Personel kaydı bulunamadı.</td></tr>`;
    } else {
        filteredEmps.forEach(emp => {
            const empId = emp.id ?? emp.Id;
            const name = emp.fullName ?? emp.FullName ?? `${emp.firstName ?? emp.FirstName} ${emp.lastName ?? emp.LastName}`;
            const dept = emp.departmentText ?? emp.DepartmentText ?? '';

            html += `<tr>
                <td style="vertical-align:middle;background:#fafbfc;">
                    <strong style="font-size:13px;color:#0f172a;">${escHtml(name)}</strong>
                    <div style="font-size:11px;color:#64748b;"><span class="badge ${getDeptBadgeClass(dept)}" style="font-size:10px;">${escHtml(dept)}</span></div>
                </td>`;

            days.forEach(day => {
                const dateStr = day.toISOString().slice(0, 10);
                const shift = rosterShifts.find(s => {
                    const sDate = (s.shiftDate ?? s.ShiftDate ?? '').substring(0, 10);
                    return (s.employeeId ?? s.EmployeeId) === empId && sDate === dateStr;
                });

                if (shift) {
                    const shiftId = shift.id ?? shift.Id;
                    const typeText = shift.shiftTypeText ?? shift.ShiftTypeText ?? 'Vardiya';
                    const status = shift.status ?? shift.Status;
                    const shiftType = shift.shiftType ?? shift.ShiftType;

                    let bgStyle = 'background:#f1f5f9;color:#334155;border:1px solid #cbd5e1;';
                    if (shiftType === 1) bgStyle = 'background:#dbeafe;color:#1e40af;border:1px solid #93c5fd;'; // Sabah
                    else if (shiftType === 2) bgStyle = 'background:#d1fae5;color:#065f46;border:1px solid #6ee7b7;'; // Öğle
                    else if (shiftType === 3) bgStyle = 'background:#fef3c7;color:#92400e;border:1px solid #fcd34d;'; // Akşam
                    else if (shiftType === 4) bgStyle = 'background:#cff4fc;color:#087990;border:1px solid #9eeaf9;'; // Tam Gün
                    else if (shiftType === 5) bgStyle = 'background:#f3e8ff;color:#6b21a8;border:1px solid #d8b4fe;'; // Yarım Gün

                    if (status === 4) bgStyle = 'background:#fee2e2;color:#991b1b;border:1px solid #fca5a5;'; // Devamsız

                    html += `
                    <td style="text-align:center;vertical-align:middle;padding:6px;cursor:pointer;" onclick="openEditModal(${shiftId})" title="Tıkla & Düzenle">
                        <div class="p-1.5 rounded shadow-sm" style="${bgStyle}">
                            <div style="font-weight:700;font-size:11.5px;">${escHtml(typeText)}</div>
                            <div style="font-size:10px;opacity:0.9;">${shift.formattedStartTime ?? shift.FormattedStartTime ?? ''} - ${shift.formattedEndTime ?? shift.FormattedEndTime ?? ''}</div>
                        </div>
                    </td>`;
                } else {
                    html += `
                    <td style="text-align:center;vertical-align:middle;color:#cbd5e1;font-size:12px;cursor:pointer;" onclick="quickAddShiftForEmp(${empId}, '${dateStr}')" title="Vardiya Ekle">
                        <span class="text-muted opacity-50"><i class="bi bi-plus-circle me-1"></i>Vardiya Yaz</span>
                    </td>`;
                }
            });

            html += `</tr>`;
        });
    }

    html += `</tbody></table>`;
    container.innerHTML = html;
}

async function quickAddShiftForEmp(empId, dateStr) {
    try {
        const form = document.getElementById('shiftForm');
        if (form) form.reset();
        $('#shiftId').val('0');
        $('.edit-only').addClass('d-none');

        if (!allEmployees || allEmployees.length === 0) {
            await loadEmployeesDropdown();
        } else {
            populateEmployeeDropdownSelect();
        }

        $('#employeeId').val(empId);
        $('#shiftDate').val(dateStr);
        $('#shiftModalTitle').html('<i class="bi bi-calendar-plus me-2"></i>Yeni Vardiya Kaydı');
        shiftModal.show();
    } catch (e) {
        console.error('quickAddShiftForEmp error:', e);
        shiftModal.show();
    }
}

// ─── Modal Actions & CRUD ─────────────────────────────────────────────────────
async function openCreateModal() {
    try {
        const form = document.getElementById('shiftForm');
        if (form) form.reset();
        $('#shiftId').val('0');
        $('.edit-only').addClass('d-none');

        const todayStr = new Date().toISOString().slice(0, 10);
        $('#shiftDate').val(todayStr);
        $('#shiftModalTitle').html('<i class="bi bi-calendar-plus me-2"></i>Yeni Vardiya Kaydı');

        if (!allEmployees || allEmployees.length === 0) {
            await loadEmployeesDropdown();
        } else {
            populateEmployeeDropdownSelect();
        }

        shiftModal.show();
    } catch (err) {
        console.error('openCreateModal error:', err);
        shiftModal.show();
    }
}

window.openEditModal = async function (id) {
    try {
        if (!allEmployees || allEmployees.length === 0) {
            await loadEmployeesDropdown();
        } else {
            populateEmployeeDropdownSelect();
        }

        const response = await efbisAjax.get(`/Shifts/GetDetail/${id}`);
        if (response && response.success && response.data) {
            const data = response.data;
            document.getElementById('shiftForm').reset();
            $('#shiftId').val(id);

            populateEmployeeDropdownSelect();
            $('#employeeId').val(data.employeeId ?? data.EmployeeId);
            $('#shiftDate').val((data.shiftDate ?? data.ShiftDate).substring(0, 10));
            $('#shiftType').val(data.shiftType ?? data.ShiftType);

            $('#startTime').val(data.formattedStartTime ?? data.FormattedStartTime);
            $('#endTime').val(data.formattedEndTime ?? data.FormattedEndTime);

            if (data.actualStartTime || data.ActualStartTime) {
                $('#actualStartTime').val(data.formattedActualStart ?? data.FormattedActualStart);
            }
            if (data.actualEndTime || data.ActualEndTime) {
                $('#actualEndTime').val(data.formattedActualEnd ?? data.FormattedActualEnd);
            }

            $('#status').val(data.status ?? data.Status);
            $('#notes').val(data.notes ?? data.Notes);

            $('.edit-only').removeClass('d-none');
            $('#shiftModalTitle').html('<i class="bi bi-pencil-square me-2"></i>Vardiya Düzenle');
            shiftModal.show();
        }
    } catch (err) {
        console.error('openEditModal error:', err);
    }
};

async function saveShift() {
    const form = document.getElementById('shiftForm');
    if (!form.checkValidity()) {
        form.reportValidity();
        return;
    }

    const id = $('#shiftId').val();
    const url = id === '0' ? '/Shifts/Create' : `/Shifts/Update/${id}`;

    const data = {
        id: parseInt(id),
        employeeId: parseInt($('#employeeId').val()),
        shiftDate: $('#shiftDate').val(),
        shiftType: parseInt($('#shiftType').val()),
        startTime: $('#startTime').val(),
        endTime: $('#endTime').val(),
        actualStartTime: $('#actualStartTime').val() || null,
        actualEndTime: $('#actualEndTime').val() || null,
        status: parseInt($('#status').val() || 1),
        notes: $('#notes').val()
    };

    try {
        const response = await efbisAjax.post(url, data);
        if (response && response.success) {
            showToast(response.message || 'Vardiya başarıyla kaydedildi.', 'success');
            shiftModal.hide();
            loadData(currentPage);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast((response && response.message) || 'İşlem başarısız.', 'error');
        }
    } catch (e) {
        showToast('Vardiya kaydedilirken hata meydana geldi.', 'error');
    }
}

// ─── Check-In / Check-Out / Mark Absent ───────────────────────────────────────
window.checkIn = async function (id) {
    if (!confirm('Personel için vardiya giriş saatini "Şu an" olarak kaydetmek istediğinize emin misiniz?')) return;
    try {
        const response = await efbisAjax.post(`/Shifts/CheckIn/${id}`, {});
        if (response && response.success) {
            showToast(response.message || 'Personel girişi kaydedildi.', 'success');
            loadData(currentPage);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast((response && response.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('İşlem sırasında hata oluştu.', 'error');
    }
};

window.checkOut = async function (id) {
    if (!confirm('Personel için vardiya çıkış saatini "Şu an" olarak kaydetmek istediğinize emin misiniz?')) return;
    try {
        const response = await efbisAjax.post(`/Shifts/CheckOut/${id}`, {});
        if (response && response.success) {
            showToast(response.message || 'Personel çıkışı kaydedildi.', 'success');
            loadData(currentPage);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast((response && response.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('İşlem sırasında hata oluştu.', 'error');
    }
};

window.markAbsent = async function (id) {
    if (!confirm('Personeli "Gelmedi / Devamsız" olarak işaretlemek istediğinize emin misiniz?')) return;
    try {
        const response = await efbisAjax.post(`/Shifts/MarkAbsent/${id}`, {});
        if (response && response.success) {
            showToast(response.message || 'Devamsızlık kaydedildi.', 'info');
            loadData(currentPage);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast((response && response.message) || 'Hata oluştu.', 'error');
        }
    } catch (e) {
        showToast('İşlem sırasında hata oluştu.', 'error');
    }
};

window.deleteShift = async function (id) {
    if (!confirm('Bu vardiya kaydını silmek istediğinize emin misiniz?')) return;
    try {
        const response = await efbisAjax.post(`/Shifts/Delete/${id}`, {});
        if (response && response.success) {
            showToast(response.message || 'Vardiya silindi.', 'success');
            loadData(currentPage);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast((response && response.message) || 'Silinemedi.', 'error');
        }
    } catch (e) {
        showToast('Silme işlemi başarısız.', 'error');
    }
};

// ─── Auto Weekly Schedule Generator ───────────────────────────────────────────
function openAutoGenerateScheduleModal() {
    document.getElementById('autoScheduleDate').valueAsDate = new Date();
    autoScheduleModal.show();
}

async function confirmAutoSchedule() {
    const targetDate = document.getElementById('autoScheduleDate').value;
    if (!targetDate) {
        showToast('Lütfen bir tarih seçiniz.', 'warning');
        return;
    }

    try {
        const res = await efbisAjax.post('/Shifts/GenerateWeeklySchedule', { targetDate: targetDate });
        if (res && res.success) {
            showToast(res.message || 'Haftalık vardiya planı oluşturuldu.', 'success');
            autoScheduleModal.hide();
            loadData(1);
            loadDashboard();
            renderWeeklyRosterGrid();
        } else {
            showToast('Vardiya planı oluşturulamadı.', 'error');
        }
    } catch (e) {
        showToast('Plan oluşturulurken hata meydana geldi.', 'error');
    }
}

// ─── Excel / CSV Export ───────────────────────────────────────────────────────
function exportShiftsToCSV() {
    if (!currentShifts || currentShifts.length === 0) {
        showToast('İndirilecek vardiya kaydı bulunmamaktadır.', 'warning');
        return;
    }

    let csv = "data:text/csv;charset=utf-8,";
    csv += "Vardiya_Kodu;Personel;Departman;Tarih;Vardiya_Tipi;Planlanan;Gercek_Giris;Gercek_Cikis;Fazla_Mesai;Durum\n";

    currentShifts.forEach(s => {
        const code = s.shiftCode || s.ShiftCode || '';
        const emp = s.employeeName || s.EmployeeName || '';
        const dept = s.departmentText || s.DepartmentText || '';
        const date = s.formattedDate || s.FormattedDate || '';
        const type = s.shiftTypeText || s.ShiftTypeText || '';
        const planned = `${s.formattedStartTime || s.FormattedStartTime || ''}-${s.formattedEndTime || s.FormattedEndTime || ''}`;
        const inTime = s.formattedActualStart || s.FormattedActualStart || '';
        const outTime = s.formattedActualEnd || s.FormattedActualEnd || '';
        const overtime = s.overtimeHours || s.OvertimeHours || 0;
        const status = s.statusText || s.StatusText || '';

        csv += `${code};"${emp}";${dept};${date};${type};${planned};${inTime};${outTime};${overtime};${status}\n`;
    });

    const encodedUri = encodeURI(csv);
    const link = document.createElement("a");
    link.setAttribute("href", encodedUri);
    link.setAttribute("download", `Vardiya_Cizelgesi_${new Date().toISOString().slice(0, 10)}.csv`);
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    showToast('Vardiya çizelgesi (CSV) bilgisayarınıza indirildi.', 'success');
}

// ─── Helpers ───────────────────────────────────────────────────────────────────
function setDefaultTimes(shiftType) {
    let start = '', end = '';
    switch (shiftType) {
        case '1': start = '09:00'; end = '17:00'; break;
        case '2': start = '13:00'; end = '21:00'; break;
        case '3': start = '17:00'; end = '01:00'; break;
        case '4': start = '10:00'; end = '22:00'; break;
        case '5': start = '09:00'; end = '13:00'; break;
    }
    if (start && end) {
        $('#startTime').val(start);
        $('#endTime').val(end);
    }
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

function renderPagination(data) {
    const container = $('#paginationContainer');
    container.empty();

    const totalPages = data.totalPages || data.TotalPages || Math.ceil((data.totalCount || data.TotalCount || 0) / pageSize) || 1;
    const totalCount = data.totalCount || data.TotalCount || 0;

    const start = totalCount === 0 ? 0 : ((currentPage - 1) * pageSize) + 1;
    const end = Math.min(currentPage * pageSize, totalCount);

    let html = `
        <div class="text-muted small">
            ${totalCount === 0 ? 'Kayıt bulunamadı' : `Toplam <strong>${totalCount}</strong> kayıttan <strong>${start}–${end}</strong> arası gösteriliyor`}
        </div>
        <ul class="pagination efbis-pagination mb-0">
            <li class="page-item ${currentPage === 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadData(${currentPage - 1})"><i class="bi bi-chevron-left"></i></a>
            </li>
    `;

    if (totalPages > 1) {
        for (let i = 1; i <= totalPages; i++) {
            if (i === 1 || i === totalPages || (i >= currentPage - 1 && i <= currentPage + 1)) {
                html += `
                    <li class="page-item ${i === currentPage ? 'active' : ''}">
                        <a class="page-link page-btn" href="javascript:void(0)" onclick="loadData(${i})">${i}</a>
                    </li>
                `;
            } else if (i === currentPage - 2 || i === currentPage + 2) {
                html += `<li class="page-item disabled"><span class="page-link border-0">...</span></li>`;
            }
        }
    }

    html += `
            <li class="page-item ${currentPage === totalPages || totalPages <= 1 ? 'disabled' : ''}">
                <a class="page-link page-btn" href="javascript:void(0)" onclick="loadData(${currentPage + 1})"><i class="bi bi-chevron-right"></i></a>
            </li>
        </ul>
    `;

    container.html(html);
}
