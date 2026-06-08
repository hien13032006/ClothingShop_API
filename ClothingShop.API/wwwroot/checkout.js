const API_BASE_URL = "https://localhost:5001/api";

// State để quản lý dữ liệu checkout tập trung
const checkoutState = {
    items: JSON.parse(localStorage.getItem('checkout_data')) || [],
    appliedVoucher: JSON.parse(localStorage.getItem('checkout_voucher')) || null,
    address: null,
    shippingMethod: "Tiêu chuẩn",
    paymentMethod: "Thanh toán khi nhận hàng",
    orderSummary: null
};

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
    if (checkoutState.appliedVoucher) {
        const discount = checkoutState.appliedVoucher.discountAmount || 0;
        voucherDisplay.innerText = `-${discount.toLocaleString()}đ`;
    } else {
        voucherDisplay.innerText = "-0đ";
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

        // Giả sử nếu có địa chỉ thì hiển thị box mặc định, không thì hiện form
        if (userProfile.address) {
            checkoutState.address = userProfile;
            document.getElementById('display-address-text').innerHTML =
                `<strong>${userProfile.fullName}</strong> - ${userProfile.phone}<br>${userProfile.address}`;
            defaultBox.style.display = 'block';
        } else {
            formBox.style.display = 'block'; // Chưa có địa chỉ, hiện form để nhập
        }
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
    // 1. Lấy dữ liệu thô từ state
    const subTotal = checkoutState.orderSummary?.subTotal || 0;
    
    // 2. Lấy giá trị giảm giá từ voucher (Khởi tạo biến trước khi dùng)
    const discount = checkoutState.appliedVoucher ? checkoutState.appliedVoucher.discountAmount : 0;

    // 3. Lấy phí ship gốc từ radio
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
    document.getElementById('display-voucher-discount').innerText = `-${discount.toLocaleString()}đ`;

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
    const payload = {
        FullName: document.getElementById('input-full-name').value || checkoutState.address?.fullName,
        Phone: document.getElementById('input-phone').value || checkoutState.address?.phone,
        ShippingAddress: document.getElementById('input-address').value || checkoutState.address?.address,
        AddressId: document.getElementById('address-id-hidden')?.value,
        ShippingMethod: shippingRadio.value,
        PaymentMethod: paymentRadio.value,
        PromotionCode: checkoutState.appliedVoucher?.code || "",
        Items: checkoutState.items.map(item => ({
            VariantId: item.variantId,
            Quantity: item.quantity
        }))
    };

    if (!payload.ShippingAddress) {
        alert("Vui lòng nhập thông tin địa chỉ giao hàng!");
        return;
    }

    const response = await fetch(`${API_BASE_URL}/order`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
        },
        body: JSON.stringify(payload)
    });

    const result = await response.json();
    if (result.success) {
        alert("Đặt hàng thành công!");
        localStorage.removeItem('checkout_data');
        window.location.href = "order-history.html";
    } else {
        alert("Lỗi: " + result.message);
    }
}


//THANH TOÁN ONLINE
let hasPaidOnline = false;

// Hàm hiển thị Popup khi chọn Thanh toán online
function handlePaymentMethodChange() {
    const paymentMethod = document.querySelector('input[name="payment"]:checked').value;
    const btnOrder = document.querySelector('.btn-order');

    if (paymentMethod === "Thanh toán online") {
        btnOrder.innerText = "Thanh toán ngay";
        btnOrder.onclick = showPaymentModal; // Thay đổi hành động nút bấm
    } else {
        btnOrder.innerText = "Đặt hàng";
        btnOrder.onclick = placeOrder; // Quay lại hành động thường
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
    alert("Đã xác nhận thanh toán! Bạn có thể nhấn Đặt hàng.");

    // Đổi nút thành Đặt hàng sau khi thanh toán xong
    const btnOrder = document.querySelector('.btn-order');
    btnOrder.innerText = "Đặt hàng";
    btnOrder.onclick = placeOrder;
}