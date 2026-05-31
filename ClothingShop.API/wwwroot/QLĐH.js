// 1. Dữ liệu Mockup ban đầu
const initialOrders = [
    {
        id: "#1656", date: "12/05/2026", customer: "Lê Thị Hiền", status: "new", statusText: "Đơn hàng mới",
        phone: "0905 123 456", address: "123 Nguyễn Văn Linh, Đà Nẵng", note: "Giao giờ hành chính",
        shippingMethod: "GHN", paymentMethod: "VNPAY",
        products: [{ name: "Áo sơ mi lụa tơ tằm Premium", quantity: 2, price: 300000 }],
        subtotal: 600000, shippingFee: 30000, discount: 50000, total: "580.000₫"
    },
    { id: "#1655", date: "11/05/2026", customer: "Phan Lam Như", total: "1.200.000₫", status: "completed", statusText: "Đã hoàn thành" },
    { id: "#1654", date: "10/05/2026", customer: "Nguyễn Văn Huy", total: "890.000₫", status: "shipping", statusText: "Đang vận chuyển" },
    { id: "#1 preparation", date: "14/05/2026", customer: "Hoàng An", total: "450.000₫", status: "preparing", statusText: "Đang chuẩn bị" },
    { id: "#1653", date: "09/05/2026", customer: "Trần Thảo Nhi", total: "450.000₫", status: "cancelled", statusText: "Đã hủy" }
];

// Khởi tạo localStorage
if (!localStorage.getItem('orders')) {
    localStorage.setItem('orders', JSON.stringify(initialOrders));
}

// Biến trạng thái lọc toàn cục
let currentStatusFilter = 'all';

// --- CÁC HÀM XỬ LÝ CHÍNH ---

// 2. Hàm lọc theo Tab (Gán vào window để HTML gọi được)
window.filterByStatus = function (status) {
    currentStatusFilter = status;

    // Cập nhật giao diện Tab active
    const tabs = document.querySelectorAll('.tab-item');
    tabs.forEach(tab => {
        // Kiểm tra text hoặc thuộc tính để tìm tab đúng
        if (tab.getAttribute('onclick').includes(`'${status}'`)) {
            tab.classList.add('active');
        } else {
            tab.classList.remove('active');
        }
    });

    handleFilter(); // Chạy bộ lọc tổng hợp
};

// 3. Hàm lọc tổng hợp (Search + Date + Status)
window.handleFilter = function () {
    const keyword = document.getElementById('order-search').value.trim().toLowerCase();
    const startDate = document.getElementById('start-date').value;
    const endDate = document.getElementById('end-date').value;

    // Luôn lấy dữ liệu mới nhất từ localStorage
    const allOrders = JSON.parse(localStorage.getItem('orders')) || [];

    const filtered = allOrders.filter(order => {
        // Lọc Status
        const matchStatus = (currentStatusFilter === 'all') || (order.status === currentStatusFilter);

        // Lọc Keyword
        const matchKeyword = order.customer.toLowerCase().includes(keyword) ||
            order.id.toLowerCase().includes(keyword);

        // Lọc Ngày
        const [day, month, year] = order.date.split('/');
        const orderDate = new Date(year, month - 1, day);
        orderDate.setHours(0, 0, 0, 0);

        let matchDate = true;
        if (startDate) {
            const start = new Date(startDate);
            if (orderDate < start) matchDate = false;
        }
        if (endDate) {
            const end = new Date(endDate);
            end.setHours(23, 59, 59, 999);
            if (orderDate > end) matchDate = false;
        }

        return matchStatus && matchKeyword && matchDate;
    });

    renderOrders(filtered);
};

// 4. Hàm hiển thị dữ liệu ra bảng
function renderOrders(list) {
    const tbody = document.getElementById('order-list');
    if (!tbody) return;

    if (list.length === 0) {
        tbody.innerHTML = `<tr><td colspan="5" style="text-align:center; padding:20px;">Không tìm thấy đơn hàng phù hợp</td></tr>`;
        return;
    }

    tbody.innerHTML = list.map(order => `
        <tr onclick="goToDetail('${order.id}')" style="cursor: pointer;">
            <td style="font-weight: bold; color: #4a90e2;">${order.id}</td>
            <td>${order.date}</td>
            <td>${order.customer}</td>
            <td style="font-weight: 600;">${order.total}</td>
            <td><span class="badge badge-${order.status}">${order.statusText}</span></td>
        </tr>
    `).join('');
}

// 5. Hàm reset bộ lọc
window.resetFilter = function () {
    document.getElementById('order-search').value = '';
    document.getElementById('start-date').value = '';
    document.getElementById('end-date').value = '';
    window.filterByStatus('all');
};

// 6. Chuyển hướng chi tiết
function goToDetail(id) {
    window.location.href = `ChiTietDonHang.html?id=${encodeURIComponent(id)}`;
}

// 7. Khởi chạy khi trang tải xong
document.addEventListener('DOMContentLoaded', () => {
    // Hiển thị dữ liệu lần đầu
    const orders = JSON.parse(localStorage.getItem('orders')) || initialOrders;
    renderOrders(orders);

    // --- KHÔI PHỤC CHỨC NĂNG VỪA NHẬP VỪA LỌC ---
    const searchInput = document.getElementById('order-search');
    if (searchInput) {
        searchInput.addEventListener('input', () => {
            handleFilter(); // Tự động gọi hàm lọc mỗi khi người dùng gõ phím
        });
    }

    // Tự động lọc khi người dùng thay đổi ngày tháng mà không cần nhấn nút "Lọc"
    const startDateInput = document.getElementById('start-date');
    const endDateInput = document.getElementById('end-date');

    if (startDateInput) startDateInput.addEventListener('change', handleFilter);
    if (endDateInput) endDateInput.addEventListener('change', handleFilter);
});