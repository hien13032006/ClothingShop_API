const API_BASE_URL = 'https://localhost:5001/api';
function mapStatusToKey(status) {
    const map = {
        "Chờ xác nhận": "placed",
        "Đang chuẩn bị": "new",
        "Đang giao": "shipping",
        "Đã giao": "delivered",
        "Hoàn thành": "completed", // Thêm dòng này để khớp với JSON
        "Đánh giá": "completed"
    };
    return map[status] || "placed";
}

document.addEventListener('DOMContentLoaded', async () => {
    const urlParams = new URLSearchParams(window.location.search);
    const orderId = urlParams.get('id');

    if (!orderId) {
        alert("Không tìm thấy mã đơn hàng!");
        return;
    }

    try {
        // Dùng template literal để đảm bảo đường dẫn chính xác
        const response = await fetch(`${API_BASE_URL}/order/${orderId}`, {
            method: 'GET',
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            // Nếu lỗi 404 hoặc 401, hiển thị thông báo cụ thể
            const errorData = await response.json().catch(() => ({ message: "Lỗi kết nối server" }));
            throw new Error(errorData.message || "Không tìm thấy đơn hàng");
        }

        const result = await response.json();

        if (result.success) {
            displayOrderDetail(result.data);
            renderTimeline(mapStatusToKey(result.data.status));
            // Không cần renderCustomerActions nữa vì bạn đã bỏ chức năng
        }
    } catch (error) {
        console.error("Lỗi:", error);
        alert("Có lỗi xảy ra: " + error.message);
    }
});

// 2. Hiển thị thông tin hóa đơn động
function displayOrderDetail(order) {
    // Đổ thông tin chung - Sử dụng đúng tên field từ JSON API
    const title = document.getElementById('title-id');
    if (title) title.innerText = `Chi tiết đơn hàng ${order.orderId}`;

    const custName = document.getElementById('customer-name');
    if (custName) custName.innerText = order.customerName || "Chưa cập nhật";

    // Gán dữ liệu vào các thẻ nếu chúng tồn tại trong HTML
    const custPhone = document.getElementById('customer-phone');
    if (custPhone) custPhone.innerText = order.phone || "Chưa cập nhật";

    document.getElementById('customer-address').innerText = order.address || "Chưa có địa chỉ";

    const orderDate = document.getElementById('order-date');
    if (orderDate) orderDate.innerText = new Date(order.orderDate).toLocaleString();

    const statusText = document.getElementById('order-status-text');
    if (statusText) statusText.innerText = order.status;

    const paymentMethod = document.getElementById('payment-method');
    if (paymentMethod) {
        paymentMethod.innerText = order.paymentMethod || "Chưa xác định";
    }

    // Đổ sản phẩm (dùng 'details' thay vì 'products')
    const productList = document.getElementById('product-list');
    if (productList && order.details) {
        productList.innerHTML = order.details.map(p => `
            <tr>
                <td>${p.productName}</td>
                <td style="text-align: center;">${p.quantity}</td>
                <td style="text-align: right;">${p.unitPrice.toLocaleString()}₫</td>
                <td style="text-align: right;">${p.lineTotal.toLocaleString()}₫</td>
            </tr>
        `).join('');
    }

    // Đổ tổng tiền
    const subtotal = document.getElementById('subtotal');
    if (subtotal) subtotal.innerText = (order.totalPrice || 0).toLocaleString() + "₫";

    const discount = document.getElementById('discount');
    if (discount) discount.innerText = "-" + (order.discountAmount || 0).toLocaleString() + "₫";

    const shippingFeeElement = document.getElementById('shipping-fee');
    if (shippingFeeElement) {

        shippingFeeElement.innerText = "" + (order.shippingFee || 0).toLocaleString() + "₫";
    }

    const totalAmount = document.getElementById('total-amount');
    if (totalAmount) totalAmount.innerText = order.finalPrice.toLocaleString() + "₫";
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

    // Tìm index dựa trên key. Nếu không tìm thấy, mặc định là 0 (Đã đặt đơn)
    let activeIndex = steps.findIndex(s => s.key === status);
    if (activeIndex === -1) activeIndex = 0;

    const timeline = document.getElementById('timeline');
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

    // Tính toán lại độ rộng vạch kẻ chính xác dựa trên index
    const progressLine = document.getElementById('progress-line');
    const width = (activeIndex / (steps.length - 1)) * 100;
    progressLine.style.width = width + "%";
}
// 4. Các nút chức năng Admin
function renderCustomerActions(status, id) {
    const actionsArea = document.getElementById('admin-actions');

    // Xóa sạch các nút, chỉ để trống hoặc ẩn vùng này
    actionsArea.innerHTML = '';
    actionsArea.style.display = 'none'; // Ẩn hoàn toàn vùng chứa nút
}
