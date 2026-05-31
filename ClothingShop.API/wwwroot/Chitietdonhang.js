document.addEventListener('DOMContentLoaded', () => {
    // 1. Lấy dữ liệu từ localStorage
    const orders = JSON.parse(localStorage.getItem('orders')) || [];
    const urlParams = new URLSearchParams(window.location.search);
    const orderId = urlParams.get('id');
    const order = orders.find(o => o.id === orderId);

    if (order) {
        displayOrderDetail(order);
        renderTimeline(order.status);
        renderActions(order.status, order.id);
    } else {
        alert("Không tìm thấy đơn hàng!");
        window.location.href = "QLĐH.html"; // Chỉnh lại tên file danh sách của bạn
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
function renderActions(status, id) {
    const actionsArea = document.getElementById('admin-actions');
    if (status === 'new' || status === 'placed') {
        actionsArea.innerHTML = `
            <button class="btn btn-delete" onclick="deleteOrder('${id}')"><i class="fa-solid fa-trash"></i> Xóa đơn</button>
            <button class="btn btn-edit"><i class="fa-solid fa-pen-to-square"></i> Sửa đơn</button>
            <button class="btn btn-approve" onclick="approveOrder('${id}')"><i class="fa-solid fa-check"></i> Duyệt đơn hàng</button>
        `;
    } else {
        actionsArea.innerHTML = `
            <button class="btn btn-print" onclick="window.print()"><i class="fa-solid fa-print"></i> In hóa đơn</button>
            <button class="btn btn-approve" style="background:#4a90e2"><i class="fa-solid fa-rotate"></i> Cập nhật lộ trình</button>
        `;
    }
}

// 5. Nghiệp vụ: Duyệt đơn
function approveOrder(id) {
    let orders = JSON.parse(localStorage.getItem('orders'));
    const index = orders.findIndex(o => o.id === id);
    if (index !== -1) {
        orders[index].status = 'shipping';
        orders[index].statusText = 'Đang giao';
        localStorage.setItem('orders', JSON.stringify(orders));
        alert("Đã duyệt đơn hàng thành công!");
        location.reload();
    }
}

// 6. Nghiệp vụ: Xóa đơn
function deleteOrder(id) {
    if (confirm("Bạn có chắc chắn muốn xóa đơn hàng này khỏi hệ thống?")) {
        let orders = JSON.parse(localStorage.getItem('orders'));
        orders = orders.filter(o => o.id !== id);
        localStorage.setItem('orders', JSON.stringify(orders));
        window.location.href = "QLĐH.html";
    }
}