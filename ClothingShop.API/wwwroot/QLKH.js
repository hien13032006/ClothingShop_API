const customerData = [
    {
        id: 1, name: "Lê Thị Hiền", email: "hien12@gmail.com", phone: "0355842123", address: "Phan Văn Đạt, Hòa...", status: "active",
        history: [
            { orderId: "DH101", date: "12/05/2026", total: "550.000đ", status: "Hoàn thành" },
            { orderId: "DH205", date: "14/05/2026", total: "1.200.000đ", status: "Đang giao" }
        ]
    },
    { id: 2, name: "Lê Thị Ánh", email: "", phone: "0987654321", address: "Nguyễn Lương Bằng,...", status: "active", history: [] },
    { id: 3, name: "Phan Lam Như", email: "nhu12@gmail.com", phone: "0987658921", address: "Điện Biên Phủ,...", status: "active", history: [] },
    { id: 4, name: "Nguyễn Văn Huy", email: "huynv@gmail.com", phone: "03476543786", address: "Hùng Vương, Kiến...", status: "active", history: [] },
    { id: 5, name: "Trần Thảo Nhi", email: "", phone: "08876567421", address: "Phạm Như Xương,...", status: "active", history: [] }
];

function renderCustomers(list) {
    const tbody = document.getElementById('customer-list');
    if (!tbody) return;
    tbody.innerHTML = list.map(user => {
        const emailDisplay = (user.email && user.email.trim() !== "") ? user.email : "-";
        const isLocked = user.status === "locked";
        return `
            <tr onclick="showDetail(${user.id})" style="cursor: pointer;">
                <td>${user.name}</td>
                <td>${emailDisplay}</td>
                <td>${user.phone}</td>
                <td>${user.address}</td>
                <td onclick="event.stopPropagation();">
                    <button class="${isLocked ? 'btn-unlock' : 'btn-lock'}" onclick="toggleLock(${user.id})">
                        ${isLocked ? 'Mở khóa' : 'Khóa'}
                    </button>
                </td>
            </tr>
        `;
    }).join('');
}

function showDetail(id) {
    const user = customerData.find(u => u.id === id);
    if (!user) return;

    document.getElementById('det-name').innerText = user.name;
    document.getElementById('det-phone').innerText = user.phone;
    document.getElementById('det-email').innerText = (user.email || "-");
    document.getElementById('det-address').innerText = user.address;

    const historyBody = document.getElementById('purchase-history');
    const orders = user.history || [];
    if (orders.length > 0) {
        historyBody.innerHTML = orders.map(order => `
            <tr>
                <td>${order.orderId}</td>
                <td>${order.date}</td>
                <td>${order.total}</td>
                <td><span class="status-tag">${order.status}</span></td>
            </tr>
        `).join('');
    } else {
        historyBody.innerHTML = '<tr><td colspan="4">Chưa có lịch sử mua hàng</td></tr>';
    }

    // Hiển thị phần chi tiết, ẩn phần danh sách
    document.getElementById('list-section').style.display = 'none';
    document.getElementById('detail-section').style.display = 'block';
    document.getElementById('dynamic-title').innerText = "Chi tiết khách hàng";
}

function backToList() {
    document.getElementById('list-section').style.display = 'block';
    document.getElementById('detail-section').style.display = 'none';
    document.getElementById('dynamic-title').innerText = "Quản lý khách hàng";
}

function toggleLock(id) {
    const customer = customerData.find(u => u.id === id);
    if (customer) {
        customer.status = (customer.status === "locked") ? "active" : "locked";
        const keyword = document.getElementById('customer-search').value.toLowerCase();
        const filtered = customerData.filter(user =>
            user.name.toLowerCase().includes(keyword) || user.phone.includes(keyword)
        );
        renderCustomers(filtered);
    }
}

// Tìm kiếm
const searchInput = document.getElementById('customer-search');
if (searchInput) {
    searchInput.addEventListener('input', function (e) {
        const keyword = e.target.value.toLowerCase();
        const filtered = customerData.filter(user =>
            user.name.toLowerCase().includes(keyword) || user.phone.includes(keyword)
        );
        renderCustomers(filtered);
    });
}

// Chạy lần đầu
renderCustomers(customerData);