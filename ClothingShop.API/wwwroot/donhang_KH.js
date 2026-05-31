// 1. Mockup dữ liệu mẫu
const mockOrders = [
    {
        orderId: "DH1001",
        status: "completed",
        statusText: "HOÀN THÀNH",
        productName: "Áo khoác Blazer Caro Nam Hàn Quốc",
        variant: "Xám nhạt, L",
        quantity: 1,
        oldPrice: 650000,
        currentPrice: 450000,
        image: "images/a1.jpg"
    },
    {
        orderId: "DH1002",
        status: "shipping",
        statusText: "ĐANG GIAO",
        productName: "Quần Jean Baggy Denim",
        variant: "Xanh bụi, M",
        quantity: 2,
        oldPrice: 500000,
        currentPrice: 350000,
        image: "images/a2.jpg"
    }
];

// 2. Hàm render dữ liệu ra màn hình
function renderOrders(data) {
    const container = document.getElementById('order-render-container');

    if (data.length === 0) {
        container.innerHTML = `<div class="no-order">Chưa có đơn hàng nào.</div>`;
        return;
    }

    container.innerHTML = data.map(order => {
        // LOGIC XỬ LÝ NÚT BẤM DỰA TRÊN TRẠNG THÁI
        let actionButtons = "";

        if (order.status === "completed") {
            actionButtons = `
                <button class="btn btn-main" onclick="event.stopPropagation(); rePurchase('${order.orderId}')">Mua lại</button>
                <button class="btn btn-sub" onclick="event.stopPropagation(); openReview('${order.orderId}')">Đánh giá</button>
            `;
        }
        else if (order.status === "pending") {
            actionButtons = `
                <button class="btn btn-danger" onclick="event.stopPropagation(); cancelOrder('${order.orderId}')">Hủy đơn hàng</button>
            `;
        }
        else if (order.status === "shipping") {
            actionButtons = `
                <button class="btn btn-main" onclick="event.stopPropagation(); confirmReceived('${order.orderId}')">Đã nhận được hàng</button>
            `;
        }
        else {
            // Các trạng thái khác như 'cancelled'
            actionButtons = `<button class="btn btn-sub" onclick="event.stopPropagation()">Xem chi tiết</button>`;
        }

        return `
            <div class="order-card" onclick="window.location.href='theodoi.html?id=${order.orderId}'">
                <div class="order-card-header">
                    <span class="shop-label"><i class="fa-solid fa-store"></i> ${order.shopName}</span>
                    <span class="order-status ${order.status}">${order.statusText}</span>
                </div>
                <hr class="order-divider">
                <div class="order-product-item">
                    <img src="${order.image}" alt="sp" onerror="this.src='images/default.png'">
                    <div class="product-details">
                        <h4 class="product-title">${order.productName}</h4>
                        <p class="product-variant">Phân loại hàng: ${order.variant}</p>
                        <p class="product-quantity">x${order.quantity}</p>
                    </div>
                    <div class="product-price-info">
                        <span class="old-price">${order.oldPrice.toLocaleString()}đ</span>
                        <span class="current-price">${order.currentPrice.toLocaleString()}đ</span>
                    </div>
                </div>
                <hr class="order-divider">
                <div class="order-card-footer">
                    <div class="total-section">
                        <span class="total-label"><i class="fa-solid fa-shield-check"></i> Tổng số tiền:</span>
                        <span class="total-amount">${(order.currentPrice * order.quantity).toLocaleString()}đ</span>
                    </div>
                    <div class="order-buttons">
                        ${actionButtons}
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

// Giả lập các hàm xử lý sự kiện
function cancelOrder(id) { alert("Đang yêu cầu hủy đơn hàng: " + id); }
function confirmReceived(id) { alert("Xác nhận đã nhận đơn hàng: " + id); }

// 3. Hàm lọc theo trạng thái
function filterByStatus(status, element) {
    // Đổi màu tab active
    document.querySelectorAll('.tab-item').forEach(tab => tab.classList.remove('active'));
    element.classList.add('active');

    // Lọc dữ liệu
    const filtered = status === 'all' ? mockOrders : mockOrders.filter(o => o.status === status);
    renderOrders(filtered);
}

// Khởi chạy khi tải trang
document.addEventListener('DOMContentLoaded', () => renderOrders(mockOrders));