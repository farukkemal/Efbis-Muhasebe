// ─── Hızlı Satış (POS Kasa) JavaScript Modülü (Gelişmiş Versiyon) ─────────────────

'use strict';

let posProducts = [];
let posCart = [];
let currentCategory = 0;
let globalDiscountPercent = 0;
let heldOrders = [];
let lastReceiptData = null;

document.addEventListener('DOMContentLoaded', () => {
    loadPosProducts();
    loadHeldOrdersFromStorage();
});

const fmt = v => Number(v || 0).toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

async function loadPosProducts() {
    try {
        const res = await fetch('/api/Pos/products');
        if (!res.ok) return;
        posProducts = await res.json();
        renderPosProducts();
    } catch (e) {
        console.error('POS Products load error:', e);
    }
}

function selectPosCategory(catId, btn) {
    currentCategory = catId;
    document.querySelectorAll('#posCategoryPills .pos-category-btn').forEach(b => b.classList.remove('active', 'btn-primary'));
    document.querySelectorAll('#posCategoryPills .pos-category-btn').forEach(b => b.classList.add('btn-light', 'border'));

    if (btn) {
        btn.classList.remove('btn-light', 'border');
        btn.classList.add('active', 'btn-primary');
        btn.blur();
    }
    renderPosProducts();
}

function handlePosSearchKeyup(e) {
    if (e.key === 'Enter') {
        const val = (document.getElementById('posProductSearch')?.value || '').trim();
        if (val.length > 0) {
            // Find exact match by barcode or code
            const match = posProducts.find(p => {
                const b = p.barcode || p.Barcode || '';
                const c = p.productCode || p.ProductCode || '';
                return b === val || c.toLowerCase() === val.toLowerCase();
            });

            if (match) {
                addToPosCart(match.id || match.Id);
                document.getElementById('posProductSearch').value = '';
                renderPosProducts();
                return;
            }
        }
    }
    renderPosProducts();
}

function renderPosProducts() {
    const grid = document.getElementById('posProductGrid');
    if (!grid) return;

    const searchTerm = (document.getElementById('posProductSearch')?.value || '').toLowerCase().trim();

    let filtered = posProducts;

    if (currentCategory > 0) {
        filtered = filtered.filter(p => p.categoryId === currentCategory || p.CategoryId === currentCategory);
    }

    if (searchTerm.length > 0) {
        filtered = filtered.filter(p => {
            const name = (p.productName || p.ProductName || '').toLowerCase();
            const code = (p.productCode || p.ProductCode || '').toLowerCase();
            const bc = (p.barcode || p.Barcode || '').toLowerCase();
            return name.includes(searchTerm) || code.includes(searchTerm) || bc.includes(searchTerm);
        });
    }

    if (filtered.length === 0) {
        grid.innerHTML = '<div class="col-12 text-center text-muted py-5"><i class="bi bi-search fs-1 d-block mb-2 text-muted opacity-50"></i>Aranan kriterlere uygun ürün bulunamadı.</div>';
        return;
    }

    grid.innerHTML = filtered.map(p => {
        const id = p.id || p.Id;
        const name = p.productName || p.ProductName;
        const code = p.productCode || p.ProductCode;
        const price = p.salePrice ?? p.SalePrice ?? 0;
        const stock = p.currentStock ?? p.CurrentStock ?? 0;
        const cat = p.categoryName || p.CategoryName || 'Genel';

        return `
        <div class="col-6 col-md-4 col-xl-3">
            <div class="pos-product-card p-3 h-100 d-flex flex-column justify-content-between" onclick="addToPosCart(${id})">
                <div>
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="badge bg-light text-muted border" style="font-size:10px;">${escHtml(cat)}</span>
                        <span class="badge ${stock > 0 ? 'bg-success-subtle text-success' : 'bg-dark text-white'}" style="font-size:10px;">Stok: ${stock}</span>
                    </div>
                    <div class="fw-bold text-dark text-truncate mb-1" style="font-size:13.5px;" title="${escHtml(name)}">${escHtml(name)}</div>
                    <div class="font-monospace text-muted small" style="font-size:11px;">${escHtml(code)}</div>
                </div>
                <div class="pt-2 border-top mt-2 d-flex justify-content-between align-items-center">
                    <strong class="fs-5 text-primary font-monospace">${fmt(price)} ₺</strong>
                    <span class="btn btn-sm btn-primary rounded-circle p-1" style="width:28px;height:28px;display:flex;align-items:center;justify-content:center;"><i class="bi bi-plus-lg"></i></span>
                </div>
            </div>
        </div>`;
    }).join('');
}

function addToPosCart(productId) {
    const prod = posProducts.find(p => (p.id || p.Id) === productId);
    if (!prod) return;

    const existing = posCart.find(item => item.productId === productId);
    if (existing) {
        existing.quantity += 1;
        existing.lineTotal = existing.quantity * existing.unitPrice;
    } else {
        const price = prod.salePrice ?? prod.SalePrice ?? 0;
        const vatRate = prod.vatRate ?? prod.VatRate ?? 20;
        posCart.push({
            productId: productId,
            productName: prod.productName || prod.ProductName,
            productCode: prod.productCode || prod.ProductCode,
            quantity: 1,
            unitPrice: price,
            vatRate: vatRate,
            lineTotal: price
        });
    }

    renderPosCart();
}

function updatePosCartQty(productId, delta) {
    const item = posCart.find(i => i.productId === productId);
    if (!item) return;

    item.quantity += delta;
    if (item.quantity <= 0) {
        posCart = posCart.filter(i => i.productId !== productId);
    } else {
        item.lineTotal = item.quantity * item.unitPrice;
    }

    renderPosCart();
}

function removeFromPosCart(productId) {
    posCart = posCart.filter(i => i.productId !== productId);
    renderPosCart();
}

function clearPosCart() {
    posCart = [];
    globalDiscountPercent = 0;
    document.getElementById('posReceivedAmount').value = '';
    renderPosCart();
}

function applyPosGlobalDiscount(percent) {
    globalDiscountPercent = percent;
    renderPosCart();
}

function renderPosCart() {
    const list = document.getElementById('posCartItemList');
    const badge = document.getElementById('cartItemCountBadge');
    const btnPrintCart = document.getElementById('btnPrintCartDraft');
    if (!list) return;

    badge.textContent = `${posCart.length} Kalem`;

    // Toggle Print Receipt Button Visibility as soon as products are added!
    if (btnPrintCart) {
        if (posCart.length > 0) {
            btnPrintCart.classList.remove('d-none');
        } else {
            btnPrintCart.classList.add('d-none');
        }
    }

    if (posCart.length === 0) {
        list.innerHTML = `
        <div class="text-center text-muted py-5" id="emptyCartNotice">
            <i class="bi bi-cart-x fs-1 d-block mb-2 text-muted opacity-50"></i>
            Sepetiniz boş.<br>Soldaki ürünlere veya barkod okuyucuya tıklayarak ekleyin.
        </div>`;
        updatePosCartTotals(0, 0, 0, 0);
        return;
    }

    let subTotal = 0;
    let vatTotal = 0;

    list.innerHTML = posCart.map(item => {
        const netLine = item.quantity * item.unitPrice;
        const vatLine = netLine * (item.vatRate / 100);
        subTotal += netLine;
        vatTotal += vatLine;

        return `
        <div class="cart-item-row d-flex align-items-center justify-content-between">
            <div class="flex-grow-1 me-2 overflow-hidden">
                <div class="fw-bold text-dark text-truncate" style="font-size:12.5px;">${escHtml(item.productName)}</div>
                <div class="text-muted small font-monospace" style="font-size:11px;">${fmt(item.unitPrice)} ₺ × ${item.quantity}</div>
            </div>
            <div class="d-flex align-items-center gap-1">
                <button class="btn btn-sm btn-light border p-0 text-center" style="width:24px;height:24px;" onclick="updatePosCartQty(${item.productId}, -1)">-</button>
                <span class="fw-bold px-1 font-monospace" style="font-size:12px;">${item.quantity}</span>
                <button class="btn btn-sm btn-light border p-0 text-center" style="width:24px;height:24px;" onclick="updatePosCartQty(${item.productId}, 1)">+</button>
                <strong class="font-monospace text-dark ms-2" style="font-size:12.5px;">${fmt(item.lineTotal)} ₺</strong>
                <button class="btn btn-sm btn-link text-danger p-0 ms-1" onclick="removeFromPosCart(${item.productId})"><i class="bi bi-x-circle-fill"></i></button>
            </div>
        </div>`;
    }).join('');

    const rawGrand = subTotal + vatTotal;
    const discountAmount = rawGrand * (globalDiscountPercent / 100);
    const grandTotal = Math.max(0, rawGrand - discountAmount);

    updatePosCartTotals(subTotal, vatTotal, discountAmount, grandTotal);
}

function updatePosCartTotals(sub, vat, disc, grand) {
    document.getElementById('cartSubtotal').textContent = fmt(sub) + ' ₺';
    document.getElementById('cartVatTotal').textContent = fmt(vat) + ' ₺';
    document.getElementById('cartDiscountTotal').textContent = (disc > 0 ? '-' : '') + fmt(disc) + ' ₺';
    document.getElementById('cartGrandTotal').textContent = fmt(grand) + ' ₺';
    calculatePosChange();
}

function setPosReceivedPreset(val) {
    const grand = getPosGrandTotalNum();
    if (val === 'EXACT') {
        document.getElementById('posReceivedAmount').value = grand.toFixed(2);
    } else {
        document.getElementById('posReceivedAmount').value = val;
    }
    calculatePosChange();
}

function getPosGrandTotalNum() {
    const text = document.getElementById('cartGrandTotal')?.textContent || '0';
    return parseFloat(text.replace('.', '').replace(',', '.').replace('₺', '').trim()) || 0;
}

function calculatePosChange() {
    const grand = getPosGrandTotalNum();
    const received = parseFloat(document.getElementById('posReceivedAmount')?.value) || 0;
    const change = Math.max(0, received - grand);
    document.getElementById('posChangeAmount').textContent = fmt(change) + ' ₺';
}

// ─── Print Cart Draft / Pre-Bill Receipt (Ürün Eklendiğinde Fiş Yazdırma) ──
function printCartDraftReceipt() {
    if (posCart.length === 0) return;

    let subTotal = 0;
    let vatTotal = 0;

    posCart.forEach(i => {
        const net = i.quantity * i.unitPrice;
        subTotal += net;
        vatTotal += net * (i.vatRate / 100);
    });

    const rawGrand = subTotal + vatTotal;
    const disc = rawGrand * (globalDiscountPercent / 100);
    const grand = Math.max(0, rawGrand - disc);

    const draftReceipt = {
        invoiceNumber: "TASLAK-ADİSYON",
        date: new Date(),
        customerTitle: document.getElementById('posCustomerSelect')?.selectedOptions[0]?.text || "Perakende Müşteri",
        paymentType: "Ödeme Bekliyor (Hesap Fişi)",
        subTotal: subTotal,
        vatTotal: vatTotal,
        grandTotal: grand,
        receivedAmount: grand,
        changeAmount: 0,
        items: posCart
    };

    renderThermalReceipt(draftReceipt);
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('receiptModal'));
    modal.show();
}

// ─── Process Checkout & Show Payment Success Modal ─────────────────────────
async function processPosCheckout(paymentType) {
    if (posCart.length === 0) {
        alert('Lütfen sepetinize en az 1 ürün ekleyin.');
        return;
    }

    const custSelect = document.getElementById('posCustomerSelect');
    const cashAccSelect = document.getElementById('posCashAccountSelect');

    const custId = parseInt(custSelect?.value) || 0;
    let cashAccId = parseInt(cashAccSelect?.value) || 0;

    if (cashAccId <= 0 && cashAccSelect && cashAccSelect.options.length > 0) {
        for (let i = 0; i < cashAccSelect.options.length; i++) {
            const val = parseInt(cashAccSelect.options[i].value);
            if (val > 0) {
                cashAccId = val;
                cashAccSelect.value = val;
                break;
            }
        }
    }

    const received = parseFloat(document.getElementById('posReceivedAmount')?.value) || 0;
    const grand = getPosGrandTotalNum();
    const change = Math.max(0, received - grand);

    const payload = {
        customerId: custId > 0 ? custId : null,
        cashAccountId: cashAccId,
        paymentType: paymentType,
        totalAmount: grand,
        receivedAmount: received > 0 ? received : grand,
        changeAmount: change,
        items: posCart
    };

    try {
        const res = await fetch('/api/Pos/checkout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        const data = await res.json();
        const isOk = res.ok && (data.success === true || data.Success === true);

        if (isOk) {
            const rcpt = data.receipt || data.Receipt;
            lastReceiptData = rcpt;

            // Update Payment Success Confirmation Modal
            document.getElementById('successInvoiceNum').textContent = rcpt.invoiceNumber || rcpt.InvoiceNumber;
            document.getElementById('successPaymentType').textContent = paymentType;
            document.getElementById('successGrandTotal').textContent = fmt(rcpt.grandTotal || rcpt.GrandTotal) + ' ₺';
            document.getElementById('successChangeAmount').textContent = fmt(rcpt.changeAmount || rcpt.ChangeAmount) + ' ₺';

            clearPosCart();
            loadPosProducts();

            // Open Payment Success Modal!
            const successModal = bootstrap.Modal.getOrCreateInstance(document.getElementById('paymentSuccessModal'));
            successModal.show();
        } else {
            alert('Hata: ' + (data.message || data.Message || 'Ödeme tamamlanamadı.'));
        }
    } catch (e) {
        console.error('POS Checkout error:', e);
        alert('Ödeme işlemi sırasında bir hata oluştu: ' + e.message);
    }
}

function printReceiptFromSuccessModal() {
    if (lastReceiptData) {
        renderThermalReceipt(lastReceiptData);
        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('receiptModal'));
        modal.show();
    }
}

// ─── Hold Orders / Askıya Alma ──────────────────────────────────────────────
function holdCurrentPosOrder() {
    if (posCart.length === 0) {
        alert('Askıya alınacak ürün yok.');
        return;
    }

    const heldItem = {
        id: Date.now(),
        date: new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }),
        items: [...posCart],
        customerTitle: document.getElementById('posCustomerSelect')?.selectedOptions[0]?.text || 'Perakende'
    };

    heldOrders.push(heldItem);
    saveHeldOrdersToStorage();
    clearPosCart();
    alert('Satış sepeti askıya alındı.');
}

function loadHeldOrdersFromStorage() {
    try {
        const str = localStorage.getItem('efbis_pos_held_orders');
        if (str) heldOrders = JSON.parse(str);
        updateHeldBadge();
    } catch (e) { heldOrders = []; }
}

function saveHeldOrdersToStorage() {
    localStorage.setItem('efbis_pos_held_orders', JSON.stringify(heldOrders));
    updateHeldBadge();
}

function updateHeldBadge() {
    const badge = document.getElementById('heldOrdersBadge');
    if (badge) badge.textContent = heldOrders.length;
}

function openHeldOrdersModal() {
    const body = document.getElementById('heldOrdersModalBody');
    if (!body) return;

    if (heldOrders.length === 0) {
        body.innerHTML = '<div class="text-center text-muted py-4"><i class="bi bi-inbox fs-1 d-block mb-2"></i>Askıda bekleyen sepet bulunmamaktadır.</div>';
    } else {
        body.innerHTML = heldOrders.map((o, idx) => `
        <div class="p-3 bg-light rounded-3 border mb-2 d-flex align-items-center justify-content-between">
            <div>
                <strong class="text-dark">Saat: ${o.date}</strong> - <span class="text-primary">${escHtml(o.customerTitle)}</span>
                <div class="text-muted small">${o.items.length} Kalem Ürün</div>
            </div>
            <button class="btn btn-sm btn-success fw-bold" onclick="restorePosOrder(${idx})">
                <i class="bi bi-play-circle me-1"></i> Sepeti Geri Yükle
            </button>
        </div>`).join('');
    }

    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('heldOrdersModal'));
    modal.show();
}

function restorePosOrder(index) {
    if (index >= 0 && index < heldOrders.length) {
        const order = heldOrders.splice(index, 1)[0];
        posCart = order.items;
        saveHeldOrdersToStorage();
        renderPosCart();
        const modal = bootstrap.Modal.getInstance(document.getElementById('heldOrdersModal'));
        modal?.hide();
    }
}

// ─── Z-Report Modal ────────────────────────────────────────────────────────
async function openZReportModal() {
    const body = document.getElementById('zReportModalBody');
    const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('zReportModal'));
    modal.show();

    try {
        const res = await fetch('/api/Pos/z-report');
        if (!res.ok) return;
        const d = await res.json();

        body.innerHTML = `
        <div class="text-center mb-3">
            <h5 class="fw-bold text-dark mb-0">Z-RAPORU KASA GÜN SONU</h5>
            <small class="text-muted font-monospace">${new Date(d.date || d.Date).toLocaleDateString('tr-TR')} Tarihli Kasa Kapanış Raporu</small>
        </div>
        <div class="table-responsive">
            <table class="table table-bordered align-middle mb-0">
                <tbody>
                    <tr><th>Bugünkü Toplam Satış Adedi</th><td class="text-end font-monospace fw-bold fs-6">${d.totalSalesCount || d.TotalSalesCount} Fiş</td></tr>
                    <tr><th>Nakit Tahsilat Toplamı</th><td class="text-end font-monospace text-success fw-bold">${fmt(d.totalCashSales || d.TotalCashSales)} ₺</td></tr>
                    <tr><th>Kredi Kartı Tahsilat Toplamı</th><td class="text-end font-monospace text-primary fw-bold">${fmt(d.totalCreditCardSales || d.TotalCreditCardSales)} ₺</td></tr>
                    <tr><th>Yemek Kartı (Sodexo/Ticket)</th><td class="text-end font-monospace text-warning fw-bold">${fmt(d.totalMealCardSales || d.TotalMealCardSales)} ₺</td></tr>
                    <tr class="table-dark fs-5 fw-bold"><td>GENEL TOPLAM CİRO</td><td class="text-end font-monospace text-success">${fmt(d.grandTotalSales || d.GrandTotalSales)} ₺</td></tr>
                </tbody>
            </table>
        </div>`;
    } catch (e) {
        body.innerHTML = '<div class="text-center text-danger py-4">Z-Raporu yüklenemedi.</div>';
    }
}

function renderThermalReceipt(r) {
    const container = document.getElementById('thermalReceiptContent');
    if (!container || !r) return;

    container.innerHTML = `
    <div style="text-align:center; font-weight:bold; font-size:16px; margin-bottom:4px;">EFBİS MUHASEBE</div>
    <div style="text-align:center; font-size:12px; margin-bottom:12px;">*** SATIŞ BİLGİ FİŞİ ***</div>
    <div style="font-size:11px; margin-bottom:8px;">
        Fiş No: <strong>${escHtml(r.invoiceNumber || r.InvoiceNumber)}</strong><br>
        Tarih : ${new Date(r.date || r.Date).toLocaleString('tr-TR')}<br>
        Müşteri: ${escHtml(r.customerTitle || r.CustomerTitle)}<br>
        Ödeme : <strong>${escHtml(r.paymentType || r.PaymentType)}</strong>
    </div>
    <hr style="border-top:1px dashed #000; margin:8px 0;">
    <table style="width:100%; font-size:11px;">
        <thead>
            <tr style="border-bottom:1px solid #000;">
                <th style="text-align:left;">Ürün</th>
                <th style="text-align:center;">Adet</th>
                <th style="text-align:right;">Tutar</th>
            </tr>
        </thead>
        <tbody>
            ${(r.items || r.Items || []).map(i => `
            <tr>
                <td>${escHtml(i.productName || i.ProductName)}</td>
                <td style="text-align:center;">${i.quantity || i.Quantity}</td>
                <td style="text-align:right;">${fmt(i.lineTotal || i.LineTotal)} ₺</td>
            </tr>`).join('')}
        </tbody>
    </table>
    <hr style="border-top:1px dashed #000; margin:8px 0;">
    <div style="font-size:12px; margin-top:6px;">
        <div style="display:flex; justify-content:space-between;"><span>Ara Toplam:</span><span>${fmt(r.subTotal || r.SubTotal)} ₺</span></div>
        <div style="display:flex; justify-content:space-between;"><span>KDV Toplam:</span><span>${fmt(r.vatTotal || r.VatTotal)} ₺</span></div>
        <div style="display:flex; justify-content:space-between; font-weight:bold; font-size:14px; margin-top:4px;"><span>GENEL TOPLAM:</span><span>${fmt(r.grandTotal || r.GrandTotal)} ₺</span></div>
        <div style="display:flex; justify-content:space-between; margin-top:4px;"><span>Alınan Nakit:</span><span>${fmt(r.receivedAmount || r.ReceivedAmount)} ₺</span></div>
        <div style="display:flex; justify-content:space-between;"><span>Para Üstü:</span><span>${fmt(r.changeAmount || r.ChangeAmount)} ₺</span></div>
    </div>
    <div style="text-align:center; font-size:10px; margin-top:16px; border-top:1px solid #000; padding-top:6px;">
        Mali Değeri Yoktur. Bilgi Fişidir.<br>Teşekkür Eder, Yine Bekleriz!
    </div>`;
}

function escHtml(str) {
    if (!str) return '';
    return String(str)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
