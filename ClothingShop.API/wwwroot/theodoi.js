// 1. Dữ liệu Mockup (Sau này fetch từ API của nhóm bạn)
const mockOrderDetails = [
    {
        orderId: "DH1001",
        status: 3, // 1: Chờ xác nhận, 2: Chờ lấy hàng, 3: Đang giao, 4: Hoàn thành
        statusName: "Đang giao hàng",
        customer: { name: "Nguyễn Văn A", phone: "0901 234 567", address: "123 Đường Số 1, Liên Chiểu, Đà Nẵng" },
        expectedDate: "18/05/2026",
        shippingFee: 30000,
        items: [
            { id: "SP01", name: "Áo khoác Blazer Caro", variant: "Xám nhạt, L", qty: 1, price: 450000, img: "images/sp1.jpg" }
        ]
    }
];

document.addEventListener('DOMContentLoaded', () => {
    // Lấy ID từ URL: theodoi.html?id=DH1001
    const params = new URLSearchParams(window.location.search);
    const id = params.get('id');

    // Tìm dữ liệu tương ứng
    const data = mockOrderDetails.find(o => o.orderId === id) || mockOrderDetails[0]; // Mặc định lấy cái đầu nếu ko tìm thấy

    renderTrackingPage(data);
});

function renderTrackingPage(data) {
    // 1. Header
    document.getElementById('order-id').innerText = data.orderId;
    document.getElementById('status-text').innerText = data.statusName;
    document.getElementById('expected-date').innerText = data.expectedDate;

    // 2. Địa chỉ
    document.getElementById('address-info').innerHTML = `
        <strong>${data.customer.name}</strong>
        <p>${data.customer.phone}</p>
        <p>${data.customer.address}</p>
    `;

    // 3. Tiến trình (Stepper)
    const steps = ["Đơn hàng đã đặt", "Đã xác nhận", "Đang giao hàng", "Đã giao hàng"];
    document.getElementById('progress-track').innerHTML = steps.map((step, index) => `
        <div class="step ${index + 1 <= data.status ? 'completed' : ''} ${index + 1 === data.status ? 'active' : ''}">
            <div class="step-icon">${index + 1 < data.status ? '✔' : index + 1}</div>
            <div class="step-text">${step}</div>
        </div>
    `).join('');

    // 4. Sản phẩm & Tiền bạc
    let subtotal = 0;
    const itemHtml = data.items.map(item => {
        subtotal += item.price * item.qty;
        return `
            <div class="item-row">
                <img src="${item.img}" onerror="this.src='images/default.jpg'">
                <div class="item-info">
                    <h4>${item.name}</h4>
                    <span>Phân loại: ${item.variant} | x${item.qty}</span>
                </div>
                <div class="item-price">${item.price.toLocaleString()}đ</div>
            </div>
        `;
    }).join('');

    document.getElementById('product-items-container').innerHTML = itemHtml;

    document.getElementById('order-summary-price').innerHTML = `
        <div class="price-row"><span>Tổng tiền hàng</span> <span>${subtotal.toLocaleString()}đ</span></div>
        <div class="price-row"><span>Phí vận chuyển</span> <span>${data.shippingFee.toLocaleString()}đ</span></div>
        <div class="price-row total"><span>Thành tiền</span> <span>${(subtotal + data.shippingFee).toLocaleString()}đ</span></div>
    `;

    // 5. Nút bấm logic
    renderButtons(data.status);
}

function renderButtons(status) {
    const btnContainer = document.getElementById('action-buttons');
    if (status === 3) { // Đang giao
        btnContainer.innerHTML = `
            <button class="btn btn-main">Đã nhận được hàng</button>
            <button class="btn btn-sub">Yêu cầu Trả hàng/Hoàn tiền</button>
        `;
    } else if (status === 4) { // Hoàn thành
        btnContainer.innerHTML = `<button class="btn btn-main">Mua lại</button><button class="btn btn-sub">Đánh giá</button>`;
    } else {
        btnContainer.innerHTML = `<button class="btn btn-sub">Liên hệ Shop</button>`;
    }
}