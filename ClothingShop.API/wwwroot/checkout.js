// 1. Mockup Dữ liệu (Cấu trúc này mô phỏng dữ liệu từ SQL Server/API)
const mockData = {
    customer: {
        name: "Lê Thị Hiền",
        phone: "(+84) 355 842 120",
        address: "Âu Cơ, Hòa Khánh, Liên Chiểu, Đà Nẵng"
    },
    shipping: {
        methodName: "Nhanh",
        fee: 20000,
        estimate: "31 Th03 - 3 Th04",
        note: "Nhận Voucher trị giá 20.000đ nếu đơn hàng được giao trễ."
    },
    voucher: {
        code: "SALE10",
        discountAmount: 10000
    },
    paymentMethod: "Thanh toán khi nhận hàng(COD)"
};

// 2. Khởi tạo khi load trang
document.addEventListener('DOMContentLoaded', () => {
    renderAddress();
    renderProducts();
});

// 3. Render Địa chỉ
function renderAddress() {
    const container = document.getElementById('address-display');
    if (!container) return;

    const addr = mockData.customer;
    container.innerHTML = `
        <strong>${addr.name} ${addr.phone}</strong><br>
        ${addr.address}
    `;
}

// 4. Render Sản phẩm & Tính toán
function renderProducts() {
    const container = document.getElementById('checkout-product-list');
    const items = JSON.parse(localStorage.getItem('checkoutItems')) || [];

    if (items.length === 0) {
        container.innerHTML = `<div style="padding: 20px; text-align: center;">Chưa có sản phẩm nào.</div>`;
        return;
    }

    let totalItemsPrice = 0;
    let totalQuantity = 0; // Khởi tạo biến để tính tổng số lượng thực tế

    container.innerHTML = items.map(item => {
        const priceNum = parseInt(item.price.replace(/\D/g, '')) || 0;
        const qty = parseInt(item.quantity) || 0;
        const subtotal = priceNum * qty;

        totalItemsPrice += subtotal;
        totalQuantity += qty; // Cộng dồn số lượng của từng món hàng

        return `
            <div class="checkout-item">
                <div class="col-main" style="display: flex; align-items: center; gap: 15px;">
                    <img src="${item.img}" style="width:80px; height:80px; object-fit:cover; border-radius:8px;">
                    <span>${item.name}</span>
                </div>
                <div class="col-type" style="color:#888; font-size:13px;">Màu: ${item.color}<br>Size: ${item.size}</div>
                <div class="col-price">${item.price}</div>
                <div class="col-qty">${item.quantity}</div>
                <div class="col-total" style="color:#ff6b81; font-weight:bold;">${subtotal.toLocaleString()}đ</div>
            </div>
        `;
    }).join('');

    // Lấy giá trị voucher từ mockData
    const voucherDiscount = mockData.voucher.discountAmount;

    // Tổng tiền sau khi trừ voucher (nhưng chưa cộng ship)
    const priceAfterVoucher = totalItemsPrice - voucherDiscount;

    // Truyền kết quả tổng số lượng vào hàm hiển thị khối xám
    renderExtraInfo(priceAfterVoucher, totalQuantity);
    renderFinalPayment(priceAfterVoucher);
}

// 5. Render Khối thông tin bổ sung (Voucher, Note, Ship)
function renderExtraInfo(priceAfterVoucher, totalQuantity) {
    const ship = mockData.shipping;
    const vch = mockData.voucher;

    // 1. Cập nhật Voucher
    const elVoucher = document.getElementById('display-voucher-discount');
    if (elVoucher) elVoucher.innerText = `-${vch.discountAmount.toLocaleString()}đ`;

    // 2. Cập nhật Thông tin vận chuyển
    const elShipName = document.getElementById('ship-method-name');
    const elShipInfo = document.getElementById('ship-estimate-info');
    const elShipFee = document.getElementById('display-ship-fee');

    if (elShipName) elShipName.innerText = ship.methodName;
    if (elShipInfo) elShipInfo.innerHTML = `Nhận từ ${ship.estimate}<br>${ship.note}`;
    if (elShipFee) elShipFee.innerText = `${ship.fee.toLocaleString()}đ`;

    // 3. Cập nhật Tổng số tiền tạm tính (khối xám)
    const elLabelQty = document.getElementById('label-total-quantity');
    const elSubtotal = document.getElementById('subtotal-items-price');

    if (elLabelQty) elLabelQty.innerText = `Tổng số tiền (${totalQuantity} sản phẩm):`;
    if (elSubtotal) elSubtotal.innerText = `${priceAfterVoucher.toLocaleString()}đ`;
}

// 6. Render Bảng chi tiết thanh toán cuối trang
function renderFinalPayment(priceAfterVoucher) {
    const methodContainer = document.getElementById('payment-method-container');

    if (methodContainer) {
        methodContainer.style.display = "block";
        methodContainer.innerHTML = `
            <div style="display: flex; align-items: center; padding: 20px 0; border-bottom: 1px solid #f2f2f2; margin-bottom: 20px; gap:15px">
                <span style="font-size: 18px; color: #333;">Phương thức thanh toán:</span>
                <strong style="font-size: 16px;">${mockData.paymentMethod}</strong>
                <span style="color: #0056b3; cursor: pointer; font-size: 14px;">Thay đổi</span>
            </div>`;
    } else {
        console.error("Không tìm thấy thẻ #payment-method-container");
    }

    // Cập nhật các con số trong bảng chi tiết bên dưới
    const shipFee = mockData.shipping.fee; // Phí gốc (20.000đ)
    const discountShip = 20000;            // Số tiền giảm giá phí vận chuyển
    const vchDiscount = mockData.voucher.discountAmount; // Voucher đã trừ ở trên

    // Tổng thanh toán = Tiền hàng + (Phí ship - Giảm giá ship)
    const finalTotal = priceAfterVoucher + (shipFee - discountShip);

    document.getElementById('total-items-price').innerText = priceAfterVoucher.toLocaleString() + "đ";
    document.getElementById('display-shipping-fee').innerText = shipFee.toLocaleString() + "đ";
    document.getElementById('vouchership-discount').innerText = "-" + discountShip.toLocaleString() + "đ";
    document.getElementById('final-total').innerText = finalTotal.toLocaleString() + "đ";
    document.getElementById('final-total-red').innerText = finalTotal.toLocaleString() + "đ";
}

// --- CẬP NHẬT LOGIC ĐẶT HÀNG ---
// --- LOGIC XỬ LÝ MODAL TỰ THÂN (Không dùng web.js) ---

// Hàm đóng Modal
function closeCheckoutModal() {
    const modal = document.getElementById('custom-alert');
    if (modal) modal.style.display = 'none';
}

// Hàm mở Modal xác nhận đặt hàng
function placeOrder() {
    const modal = document.getElementById('custom-alert');
    const title = document.getElementById('modal-title');
    const msg = document.getElementById('modal-message');
    const btnConfirm = document.querySelector('.btn-confirm');
    const btnCancel = document.querySelector('.btn-cancel');

    if (modal && title && msg && btnConfirm) {
        // 1. Thiết lập nội dung xác nhận
        title.innerText = "Xác nhận đặt hàng";
        msg.innerText = "Bạn có chắc chắn muốn đặt mua sản phẩm này không?";

        // 2. Hiển thị nút Hủy và nút Xác nhận
        if (btnCancel) btnCancel.style.display = "inline-block";
        btnConfirm.innerText = "Xác nhận";

        // 3. Hiển thị Modal
        modal.style.display = 'flex';

        // 4. Gán sự kiện cho nút Xác nhận
        btnConfirm.onclick = function () {
            executeOrder();
        };
    }
}

// Hàm thực hiện đặt hàng thành công
function executeOrder() {
    // --- Xử lý dữ liệu LocalStorage (Giữ nguyên logic của bạn) ---
    const checkoutItems = JSON.parse(localStorage.getItem('checkoutItems')) || [];
    let mainCart = JSON.parse(localStorage.getItem('cart')) || [];

    mainCart = mainCart.filter(cartItem => {
        return !checkoutItems.some(checkoutItem =>
            checkoutItem.id === cartItem.id &&
            checkoutItem.color === cartItem.color &&
            checkoutItem.size === cartItem.size
        );
    });

    localStorage.setItem('cart', JSON.stringify(mainCart));
    localStorage.removeItem('checkoutItems');

    // --- Cập nhật giao diện Modal sang trạng thái Thành công ---
    const title = document.getElementById('modal-title');
    const msg = document.getElementById('modal-message');
    const btnConfirm = document.querySelector('.btn-confirm');
    const btnCancel = document.querySelector('.btn-cancel');

    if (title) title.innerHTML = "Đặt hàng thành công!";
    if (msg) msg.innerHTML = "Đơn hàng đã được tiếp nhận.<br>Cảm ơn bạn đã mua sắm tại <b>Clothing</b>!";

    // Ẩn nút Hủy, chỉ để lại nút về trang chủ
    if (btnCancel) btnCancel.style.display = "none";

    if (btnConfirm) {
        btnConfirm.innerText = "Quay lại trang chủ";
        btnConfirm.onclick = function () {
            window.location.href = "index.html";
        };
    }
}