const API_BASE_URL = "https://localhost:5001/api";
const BACKEND_BASE_URL = "https://localhost:5001";

// State để quản lý dữ liệu checkout tập trung
const checkoutState = {
    items: JSON.parse(localStorage.getItem('checkout_data')) || [],
    appliedVoucher: JSON.parse(localStorage.getItem('checkout_voucher')) || null,
    address: null,
    shippingMethod: "Tiêu chuẩn",
    paymentMethod: "Thanh toán khi nhận hàng",
    orderSummary: null
};

function formatVND(amount) {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
}

document.addEventListener('DOMContentLoaded', async () => {
    if (!localStorage.getItem('accessToken')) {
        window.location.href = "login.html";
        return;
    }
    await initCheckout();
    handlePaymentMethodChange();
});

async function initCheckout() {
    // 1. Tải dữ liệu song song
    await Promise.all([
        fetchProfile(),
        renderCheckoutProducts() // Gọi sau khi render sẽ có orderSummary để tính tổng
    ]);

    const voucherDisplay = document.getElementById('display-voucher-discount');
    if (checkoutState.appliedVoucher && checkoutState.appliedVoucher.code) {
        // Trường hợp ĐÃ CHỌN voucher: hiển thị mã và nút hủy
        voucherDisplay.innerHTML = `
        <span style="color: #2ed573; font-weight: bold;">${checkoutState.appliedVoucher.code}</span>
        <button onclick="removeVoucher()" style="border:none; background:none; color:red; cursor:pointer; margin-left:10px;">(Hủy)</button>
    `;
    } else {
        // Trường hợp CHƯA CHỌN voucher: hiển thị nút để bấm vào chọn
        voucherDisplay.innerHTML = `
        <a href="javascript:void(0)" onclick="openVoucherModal()" style="color: blue; text-decoration: underline;">Chọn voucher</a>
    `;
    }

    calculateOrderFees();
}

function toggleAddressForm(showForm) {
    document.getElementById('default-address-box').style.display = showForm ? 'none' : 'block';
    document.getElementById('address-form-box').style.display = showForm ? 'block' : 'none';
}

// Lấy thông tin user và địa chỉ mặc định
async function fetchProfile() {
    try {
        const res = await fetch(`${API_BASE_URL}/customer/profile`, {
            headers: { 'Authorization': `Bearer ${localStorage.getItem("accessToken")}` }
        });
        const result = await res.json();
        const userProfile = result.data || result;

        const defaultBox = document.getElementById('default-address-box');
        const formBox = document.getElementById('address-form-box');
    } catch (e) {
        console.error("Lỗi:", e);
        document.getElementById('address-form-box').style.display = 'block';
    }
}

// Render sản phẩm và lưu summary vào state
async function renderCheckoutProducts() {
    const response = await fetch(`${API_BASE_URL}/order/calculate`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
        },
        body: JSON.stringify(checkoutState.items)
    });

    const res = await response.json();
    
    // Kiểm tra xem API có trả về thành công không
    if (!res.success) {
        console.error("Lỗi tính toán đơn hàng:", res.message);
        return;
    }

    // Gán dữ liệu đúng vào biến
    const orderSummary = res.data;
    checkoutState.orderSummary = orderSummary;

    const container = document.getElementById('checkout-product-list');
    
    // SỬA LỖI: dùng orderSummary.items thay vì data.items
    container.innerHTML = orderSummary.items.map(item => `
        <div class="product-row">
            <div class="col-main"><img src="${item.imageUrl}" width="50" style="margin-right:10px;"> ${item.name}</div>
            <div class="col-type">${item.color || 'N/A'} / ${item.size || 'N/A'}</div>
            <div class="col-price">${item.price.toLocaleString()}đ</div>
            <div class="col-qty">${item.quantity}</div>
            <div class="col-total">${(item.price * item.quantity).toLocaleString()}đ</div>
        </div>
    `).join('');

    // Cập nhật tổng số lượng và giá
    document.getElementById('label-total-quantity').innerText = `Tổng số tiền (${orderSummary.items.length} sản phẩm):`;
    document.getElementById('subtotal-items-price').innerText = `${orderSummary.subTotal.toLocaleString()}đ`;
}

function calculateOrderFees() {
    const subTotal = checkoutState.orderSummary?.subTotal || 0;
    const discount = checkoutState.appliedVoucher ? checkoutState.appliedVoucher.discountAmount : 0;
    const shippingRadio = document.querySelector('input[name="shipping"]:checked');
    let shipFee = shippingRadio.value === "Giao hàng nhanh" ? 50000 : 30000;

    const subTotalDisplay = (subTotal - discount) + shipFee;
    document.getElementById('subtotal-items-price').innerText = subTotalDisplay.toLocaleString() + 'đ';
    document.getElementById('display-shipping-fee').innerText = shipFee.toLocaleString() + 'đ';

    // 4. Logic Free Ship: Nếu tiền hàng (subTotal) >= 300.000đ thì phí ship = 0
    let shippingDiscount = 0;
    if (subTotal >= 300000) {
        shippingDiscount = shipFee; 
        shipFee = 0;
    }

    // FinalTotal = Giá trị đơn hàng gốc - Giảm giá voucher + Phí ship (đã được xử lý 0đ nếu free ship)
    const finalTotal = subTotal - discount + shipFee;

    document.getElementById('total-items-price').innerText = subTotal.toLocaleString() + 'đ';
    document.getElementById('vouchership-discount').innerText = `-${shippingDiscount.toLocaleString()}đ`;

    // Cập nhật voucher
    const voucherDiscountDisplay = document.getElementById('voucher-discount-amount');
    const voucherRow = document.getElementById('voucher-row');

    // 2. Cập nhật số tiền giảm
    if (discount > 0) {
        voucherRow.style.display = 'flex'; // Hiện dòng giảm giá
        voucherDiscountDisplay.innerText = `-${discount.toLocaleString()}đ`; // Gán số tiền thực tế
    } else {
        voucherRow.style.display = 'none'; // Ẩn dòng nếu không giảm
    }

    const voucherDisplay = document.getElementById('display-voucher-discount');

    if (checkoutState.appliedVoucher && checkoutState.appliedVoucher.code) {
        // Nếu đã có voucher: Hiện mã và nút Hủy
        voucherDisplay.innerHTML = `
        <span style="color: #2ed573; font-weight: bold;">${checkoutState.appliedVoucher.code}</span>
        <button onclick="removeVoucher()" style="border:none; background:none; color:red; cursor:pointer; margin-left:10px;">(Hủy)</button>
    `;
    } else {
        // Nếu chưa có voucher: Hiện link Chọn voucher
        voucherDisplay.innerHTML = `
        <a href="javascript:void(0)" onclick="openVoucherModal()" style="color: blue; text-decoration: underline;">Chọn voucher</a>
    `;
    }

    // Cập nhật tổng cuối
    document.getElementById('final-total').innerText = finalTotal.toLocaleString() + 'đ';
    document.getElementById('final-total-red').innerText = finalTotal.toLocaleString() + 'đ';
}

function updateGrandTotal(shipFee) {
    const subTotal = checkoutState.orderSummary?.subTotal || 0;
    const discount = checkoutState.voucher ? checkoutState.voucher.discountAmount : 0
    const finalTotal = subTotal + shipFee - discount;
    document.getElementById('final-total').innerText = finalTotal.toLocaleString() + 'đ';
    document.getElementById('final-total-red').innerText = finalTotal.toLocaleString() + 'đ';
}


// Hàm đặt hàng chuẩn (lấy dữ liệu từ checkoutState)
async function placeOrder() {
    const shippingRadio = document.querySelector('input[name="shipping"]:checked');
    const paymentRadio = document.querySelector('input[name="payment"]:checked');

    if (!shippingRadio || !paymentRadio) {
        alert("Vui lòng chọn đầy đủ phương thức vận chuyển và thanh toán!");
        return;
    }

    const addressId = document.getElementById('address-id-hidden')?.value;
    if (!addressId) {
        alert("Vui lòng chọn địa chỉ giao hàng!");
        return;
    }

    const payload = {
        AddressId: parseInt(addressId),
        ShippingMethod: shippingRadio.value,
        PaymentMethod: paymentRadio.value,
        // Đảm bảo voucher code gửi đi là chuỗi, nếu không có thì gửi null hoặc rỗng
        PromotionCode: checkoutState.appliedVoucher?.code || "",
        Items: checkoutState.items.map(item => ({
            VariantId: item.variantId,
            Quantity: item.quantity
        }))
    };
    console.log("Payload gửi đi:", JSON.stringify(payload, null, 2));

    try {
        const response = await fetch(`${API_BASE_URL}/order`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            },
            body: JSON.stringify(payload)
        });

        // Đọc kết quả
        const result = await response.json();

        if (response.ok && result.success) { // response.ok kiểm tra mã trạng thái HTTP 200-299
            alert("Đặt hàng thành công!");
            localStorage.removeItem('checkout_voucher');
            localStorage.removeItem('checkout_data');
            window.location.href = `Chitietdonhang_KH.html?id=${orderId}`;
        } else {
            // Xử lý lỗi trả về từ API (ví dụ: lỗi 400 Bad Request)
            alert("Lỗi đặt hàng: " + (result.message || "Đã có lỗi xảy ra"));
        }
    } catch (err) {
        console.error("Lỗi kết nối:", err);
        alert("Không thể kết nối đến máy chủ.");
    }
}

async function loadDefaultAddress() {
    try {
        const response = await fetch(`${API_BASE_URL}/customer/addresses`, {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) throw new Error("Không thể tải địa chỉ");

        const result = await response.json();
        // Dựa vào cấu trúc ApiResponse của bạn, data nằm ở result.data
        const addresses = result.data || [];

        // Tìm địa chỉ mặc định (IsDefault == true) hoặc lấy cái đầu tiên
        const defaultAddress = addresses.find(a => a.isDefault) || addresses[0];

        if (defaultAddress) {
            // Cập nhật giá trị vào các thẻ span
            document.getElementById('display-name').innerText = defaultAddress.receiverName;
            document.getElementById('display-phone').innerText = defaultAddress.receiverPhone;
            document.getElementById('display-address-text').innerText = defaultAddress.addressDetail;

            // Lưu ID ẩn để khi đặt hàng còn gửi lên server
            document.getElementById('address-id-hidden').value = defaultAddress.addressId;
        } else {
            document.getElementById('address-info-view').innerHTML =
                `<p>Bạn chưa có địa chỉ nhận hàng. Hãy thêm địa chỉ trong hồ sơ.</p>`;
        }
    } catch (err) {
        console.error("Lỗi khi load địa chỉ:", err);
    }
}

// Gọi hàm ngay khi trang web tải xong
document.addEventListener('DOMContentLoaded', () => {
    loadDefaultAddress();
});

function toggleAddressForm(show) {
    const view = document.getElementById('address-info-view');
    const form = document.getElementById('address-form-box');

    if (show) {
        view.style.display = 'none';
        form.style.display = 'block';
    } else {
        view.style.display = 'block';
        form.style.display = 'none';
    }
}

// Khi lưu thành công, cập nhật nội dung vào các thẻ span và đóng form
function saveNewAddress() {
    document.getElementById('display-name').innerText = document.getElementById('input-full-name').value;
    document.getElementById('display-phone').innerText = document.getElementById('input-phone').value;
    document.getElementById('display-address-text').innerText = document.getElementById('input-address').value;
    toggleAddressForm(false);
}


//THANH TOÁN ONLINE
let hasPaidOnline = false;

// Hàm hiển thị Popup khi chọn Thanh toán online
function handlePaymentMethodChange() {
    const paymentMethod = document.querySelector('input[name="payment"]:checked').value;
    const btnOrder = document.querySelector('.btn-order');

    if (paymentMethod === "Thanh toán online") {
        btnOrder.innerText = "Thanh toán ngay";
        btnOrder.onclick = showPaymentModal; 
    } else {
        btnOrder.innerText = "Đặt hàng";
        btnOrder.onclick = placeOrder; 
    }
}

function showPaymentModal() {
    document.getElementById('payment-qr-modal').style.display = 'flex';
}

function closePaymentModal() {
    document.getElementById('payment-qr-modal').style.display = 'none';
}

function confirmPayment() {
    hasPaidOnline = true; // Đánh dấu đã thanh toán
    closePaymentModal();
    alert("Bạn chắc chắn đã thanh toán");

    // Đổi nút thành Đặt hàng sau khi thanh toán xong
    const btnOrder = document.querySelector('.btn-order');
    btnOrder.innerText = "Đặt hàng";
    btnOrder.onclick = placeOrder;
}

async function openVoucherModal() {
    const modal = document.getElementById('voucher-modal');
    if (!modal) return;

    modal.style.display = 'flex';
    const currentTotal = checkoutState.orderSummary?.subTotal || 0;

    const container = document.getElementById('modal-voucher-list');
    container.innerHTML = "Đang tải mã giảm giá...";

    let voucherList = [];

    try {
        try {
            console.log("Đang gọi API tới:", `${API_BASE_URL}/promotion`);
            const response = await fetch(`${API_BASE_URL}/promotion/available`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
                    'Content-Type': 'application/json'
                }
            });

            if (!response.ok) throw new Error("Lỗi Server: " + response.status);

            const result = await response.json();
            voucherList = Array.isArray(result) ? result : (result.data || []); // Kiểm tra cấu trúc JSON trả về
            console.log("Danh sách voucher nhận được:", voucherList);
        } catch (err) {
            console.error("Chi tiết lỗi API:", err);
            container.innerHTML = "Lỗi kết nối server!";
        }

        if (voucherList.length === 0) {
            container.innerHTML = `
                <div style="text-align:center; padding: 20px;">
                    <p>Bạn hiện không có mã giảm giá nào khả dụng.</p>
                    <p style="font-size: 12px; color: #747d8c;">(Có thể bạn đã sử dụng hết hoặc chưa có mã mới)</p>
                </div>
            `;
            return;
        }

        container.innerHTML = voucherList.map(voucher => {
            const code = voucher.code || voucher.Code;
            const type = voucher.discountType || voucher.DiscountType;
            const value = Number(voucher.discountValue ?? voucher.DiscountValue ?? 0);
            const minOrder = Number(voucher.minOrderAmount ?? voucher.MinOrderAmount ?? 0);

            const isEligible = currentTotal >= minOrder;
            const textDiscount = type === "Percent" ? `Giảm ${value}%` : `Giảm ${formatVND(value)}`;
            const textCondition = `Đơn tối thiểu từ ${formatVND(minOrder)}`;

            return `
            <div class="voucher-card-item ${!isEligible ? 'voucher-disabled' : ''}" 
                 style="display: flex; align-items: center; border: 1px dashed #ff4757; padding: 12px; margin-bottom: 10px; border-radius: 6px; background: #fff5f6;">
                <div style="flex: 1;">
                    <div style="font-weight: bold; font-size: 15px; color: #2f3542;">Mã: ${code}</div>
                    <div style="color: #ff4757; font-weight: bold; margin: 4px 0;">${textDiscount}</div>
                    <div style="font-size: 12px; color: #747d8c;">${textCondition}</div>
                    ${!isEligible ? `<div style="font-size: 11px; color: #ffa502; font-weight: 500; margin-top: 4px;">Còn thiếu ${formatVND(minOrder - currentTotal)} để áp dụng</div>` : ''}
                </div>
                <div>
                    <button onclick="applyVoucherToCart('${code}', '${type}', ${value}, ${minOrder})"
                            ${!isEligible ? 'disabled' : ''} 
                            style="padding: 6px 14px; background: #2ed573; color: white; border: none; border-radius: 4px; cursor: pointer; font-weight: 500;">
                        Áp dụng
                    </button>
                </div>
            </div>
        `;
        }).join('');
    } catch (err) {
        container.innerHTML = "Có lỗi khi tải mã giảm giá.";
    }
}

function closeVoucherModal() {
    const modal = document.getElementById('voucher-modal');
    if (modal) modal.style.display = 'none';
}

function applyVoucherToCart(code, type, value, minOrder) {
    const subTotal = checkoutState.orderSummary?.subTotal || 0;

    // Tính toán số tiền được giảm
    let discountAmount = 0;
    if (type === "Percent") {
        discountAmount = (subTotal * value) / 100;
    } else {
        discountAmount = value;
    }

    // Cập nhật vào state
    checkoutState.appliedVoucher = {
        code: code,
        discountAmount: discountAmount
    };

    // Lưu vào LocalStorage để đồng bộ giữa các lần tải trang
    localStorage.setItem("checkout_voucher", JSON.stringify(checkoutState.appliedVoucher));

    // Đóng modal
    closeVoucherModal();

    // Gọi lại hàm tính toán phí để cập nhật UI ngay lập tức
    calculateOrderFees();

    alert(`Đã áp dụng mã ${code} thành công!`);
}

function removeVoucher() {
    console.log("Đang hủy voucher...");
    checkoutState.appliedVoucher = null;
    localStorage.removeItem("checkout_voucher");

    // Gọi lại các hàm cập nhật giao diện
    calculateOrderFees();
    
}