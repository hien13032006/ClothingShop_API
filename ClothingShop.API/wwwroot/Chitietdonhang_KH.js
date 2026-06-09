function mapStatusToKey(status) {
    const map = {
        "Chờ xác nhận": "placed",
        "Chuẩn bị hàng": "new",
        "Đang giao": "shipping",
        "Đã giao": "delivered",
        "Đánh giá": "completed"
    };
    return map[status] || "placed"; // Mặc định là 'placed'
}

document.addEventListener('DOMContentLoaded', async () => {
    // Lấy ID từ URL: /chi-tiet?id=ORD000001
    const urlParams = new URLSearchParams(window.location.search);
    const orderId = urlParams.get('id');

    if (!orderId) return alert("Không tìm thấy mã đơn hàng!");

    try {
        // Gọi API backend thay vì localStorage
        const response = await fetch(`/api/orders/${orderId}`);
        const result = await response.json();

        if (result.success) {
            const order = result.data;
            displayOrderDetail(order);
            const activeKey = mapStatusToKey(order.status);
            renderTimeline(activeKey);
            renderCustomerActions(order.status, order.id);
        } else {
            alert("Không tải được đơn hàng!");
        }
    } catch (error) {
        console.error("Lỗi kết nối:", error);
    }
});

// 2. Hiển thị thông tin hóa đơn động
function displayOrderDetail(order) {
    document.getElementById('title-id').innerText = `Chi tiết đơn hàng ${order.id}`;
    document.getElementById('customer-name').innerText = order.customer;
    document.getElementById('customer-phone').innerText = order.phone || "Chưa cập nhật";
    document.getElementById('customer-address').innerText = order.address || "Chưa có địa chỉ";
    document.getElementById('customer-note').innerText = order.note || "Không có ghi chú";
    document.getElementById('order-date').innerText = order.date;
    document.getElementById('order-status-text').innerText = order.statusText;

    // Đổ phương thức (nếu mockup có các trường này)
    if (document.getElementById('shipping-method'))
        document.getElementById('shipping-method').innerText = order.shippingMethod || "Giao hàng nhanh";
    if (document.getElementById('payment-method'))
        document.getElementById('payment-method').innerText = order.paymentMethod || "Thanh toán khi nhận hàng";

    // Hiển thị danh sách sản phẩm
    const productTbody = document.getElementById('product-list');
    if (order.products) {
        productTbody.innerHTML = order.products.map(p => `
            <tr>
                <td>${p.name}</td>
                <td style="text-align: center;">${p.quantity}</td>
                <td style="text-align: right;">${p.price.toLocaleString()}₫</td>
                <td style="text-align: right;">${(p.quantity * p.price).toLocaleString()}₫</td>
            </tr>
        `).join('');
    }

    // Đổ các con số tổng kết
    if (document.getElementById('subtotal'))
        document.getElementById('subtotal').innerText = (order.subtotal || 0).toLocaleString() + "₫";
    if (document.getElementById('shipping-fee'))
        document.getElementById('shipping-fee').innerText = (order.shippingFee || 0).toLocaleString() + "₫";
    if (document.getElementById('discount'))
        document.getElementById('discount').innerText = "-" + (order.discount || 0).toLocaleString() + "₫";

    document.getElementById('total-amount').innerText = order.total;
}

// 3. Vẽ Timeline giống ảnh mẫu (Đơn hàng đã đặt -> Chuẩn bị hàng -> Đang giao -> Đã giao -> Đánh giá)
function renderTimeline(status) {
    const steps = [
        { key: 'placed', icon: 'fa-file-lines', text: 'Đã đặt đơn' },
        { key: 'new', icon: 'fa-box', text: 'Chuẩn bị hàng' },
        { key: 'shipping', icon: 'fa-truck-fast', text: 'Đang giao' },
        { key: 'delivered', icon: 'fa-phone-volume', text: 'Đã giao' },
        { key: 'completed', icon: 'fa-star', text: 'Đánh giá' }
    ];

    let activeIndex = steps.findIndex(s => s.key === status);
    if (status === 'completed') activeIndex = 4;
    if (status === 'new' || !status) activeIndex = 1;

    // Render các Step HTML
    const timeline = document.getElementById('timeline');
    // Lưu lại các phần tử nền
    const bgHtml = `
        <div class="progress-line"></div>
        <div class="progress-fill" id="progress-line"></div>
    `;

    const stepsHtml = steps.map((step, index) => {
        let cls = index < activeIndex ? 'completed' : (index === activeIndex ? 'active' : '');
        return `
            <div class="step ${cls}">
                <div class="step-icon"><i class="fa-solid ${step.icon}"></i></div>
                <div class="step-text">${step.text}</div>
            </div>
        `;
    }).join('');

    timeline.innerHTML = bgHtml + stepsHtml;

    // Cập nhật chiều dài đường kẻ xanh: 
    // (Vị trí hiện tại / Tổng số khoảng cách giữa các điểm) * 100%
    // Trừ đi một chút lề để nó nằm ở giữa icon
    const progressLine = document.getElementById('progress-line');
    const width = (activeIndex / (steps.length - 1)) * 100;
    progressLine.style.width = width + "%";
}
// 4. Các nút chức năng Admin
function renderCustomerActions(status, id) {
    const actionsArea = document.getElementById('admin-actions');

    // Logic nút bấm cho khách hàng
    if (status === 'Chờ xác nhận') {
        actionsArea.innerHTML = `
            <button class="btn btn-cancel" onclick="cancelOrder('${id}')">
                <i class="fa-solid fa-xmark"></i> Hủy đơn hàng
            </button>
        `;
    } else if (status === 'Đang giao') {
        actionsArea.innerHTML = `
            <button class="btn btn-confirm" onclick="confirmReceived('${id}')">
                <i class="fa-solid fa-box-open"></i> Đã nhận được hàng
            </button>
        `;
    } else if (status === 'Đã giao') {
        actionsArea.innerHTML = `
            <button class="btn btn-review" onclick="reviewOrder('${id}')">
                <i class="fa-solid fa-star"></i> Đánh giá sản phẩm
            </button>
            <button class="btn btn-buy-again" onclick="buyAgain('${id}')">
                <i class="fa-solid fa-rotate-right"></i> Mua lại
            </button>
        `;
    }
}

// 1. Hủy đơn
async function cancelOrder(id) {
    if (confirm("Bạn có chắc muốn hủy đơn hàng này?")) {
        const res = await fetch(`/api/orders/${id}/cancel`, { method: 'POST' });
        if ((await res.json()).success) {
            alert("Đã hủy đơn hàng!");
            location.reload();
        }
    }
}

// 2. Xác nhận đã nhận hàng
async function confirmReceived(id) {
    const res = await fetch(`/api/orders/${id}/confirm-received`, { method: 'POST' });
    if ((await res.json()).success) {
        alert("Cảm ơn bạn đã xác nhận nhận hàng!");
        location.reload();
    }
}

// 3. Mua lại (Thêm sản phẩm vào giỏ hàng)
async function buyAgain(id) {
    try {
        const res = await fetch(`/api/orders/${id}/reorder`, { method: 'POST' });
        const result = await res.json();
        if (result.success) {
            alert("Đã thêm sản phẩm vào giỏ hàng!");
            window.location.href = "cart.html";
        } else {
            alert("Có lỗi: " + result.message);
        }
    } catch (e) {
        alert("Không thể kết nối đến máy chủ.");
    }
}