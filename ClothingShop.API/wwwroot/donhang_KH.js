const API_BASE_URL = "https://localhost:5001/api";
const BACKEND_BASE_URL = "https://localhost:5001";

document.addEventListener('DOMContentLoaded', async () => {
  
    loadOrders('all');
});

function getActionButtons(order) {
    const oId = `'${order.orderId}'`;
    if (order.status === 'Hoàn thành') {
   
        if (order.hasReviewed === true || order.hasReviewed === 1) {
            return `
                <button class="btn btn-sub" onclick="reorder(event, ${oId})">Mua lại</button>
                <button class="btn btn-disabled" disabled style="background-color: #ccc; cursor: not-allowed;">Đã đánh giá</button>`;
        }
        return `
            <button class="btn btn-sub" onclick="reorder(event, ${oId})">Mua lại</button>
            <button class="btn btn-main" onclick="reviewOrder(event, ${oId})">Đánh giá</button>`;
    }


    switch (order.status) {
        case 'Chờ xác nhận':
            return `<button class="btn btn-sub" onclick="cancelOrder(event, ${oId})">Hủy đơn</button>`;
        case 'Đang giao':
            return `<button class="btn btn-main" onclick="confirmReceived(event, ${oId})">Đã nhận được hàng</button>`;
        default:
            return '';
    }
}

async function loadOrders(status) {
    const container = document.getElementById('order-render-container');
    container.innerHTML = '<p style="text-align:center;">Đang tải dữ liệu...</p>';

    try {
        const token = localStorage.getItem('accessToken');
        const response = await fetch(`${API_BASE_URL}/order/my-orders?status=${status}`, {
            headers: { 'Authorization': `Bearer ${token}` }
        });

        if (!response.ok) throw new Error("Lỗi tải đơn hàng");

        const result = await response.json();

        if (result.success && result.data.length > 0) {
            container.innerHTML = result.data.map(order => `
    <div class="order-card" onclick="window.location.href='Chitietdonhang_KH.html?id=${order.orderId}'">
        <div class="order-card-header">
            <span class="shop-label">Mã đơn: ${order.orderId}</span>
            <span class="order-status">${order.status}</span>
        </div>
        <hr class="order-divider">
        <div class="order-body">
            ${(order.details || []).map(item => `
                <div class="order-product-item">
                    <img src="${item.imageUrl || 'default.jpg'}" alt="${item.productName}">
                    <div class="product-details">
                        <div class="product-title">${item.productName || 'Sản phẩm'}</div>
                        <div class="product-variant">Phân loại: ${item.color || 'N/A'}, ${item.size || 'N/A'}</div>
                        <div>x${item.quantity || 0}</div>
                    </div>
                    <div class="product-price-info">
                        <span class="current-price">${(item.unitPrice || 0).toLocaleString()}₫</span>
                    </div>
                </div>
            `).join('')}
        </div>
        <hr class="order-divider">
        <div class="order-card-footer">
            <div class="total-section">
                Thành tiền: <span class="total-amount">${((order.finalPrice || order.FinalPrice || 0)).toLocaleString()}₫</span>
            </div>
            <div class="order-buttons">
                ${getActionButtons(order)}
            </div>
        </div>
    </div>
`).join('');
        } else {
            container.innerHTML = '<p style="text-align:center;">Không có đơn hàng nào.</p>';
        }
    } catch (error) {
        container.innerHTML = '<p style="text-align:center; color:red;">Có lỗi xảy ra!</p>';
    }
}

// Hàm lọc theo tab
function filterByStatus(status, element) {
    // Cập nhật giao diện tab active
    document.querySelectorAll('.tab-item').forEach(t => t.classList.remove('active'));
    element.classList.add('active');

    // Gọi lại API theo trạng thái
    loadOrders(status);
}

function getStatusClass(status) {
    const map = { 'Chờ xác nhận': 'pending', 'Đang giao': 'shipping', 'Hoàn thành': 'completed', 'Đã hủy': 'cancelled' };
    return map[status] || '';
}


async function cancelOrder(event, orderId) {
    event.stopPropagation();
    if (!confirm("Bạn có chắc chắn muốn hủy đơn hàng này?")) return;

    try {
        // Kiểm tra lại URL này khớp với Controller của bạn chưa
        const response = await fetch(`${API_BASE_URL}/order/${orderId}/cancel`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
        });
        if (response.ok) {
            alert("Đã hủy đơn hàng thành công!\n Nếu bạn đã thanh toán, số tiền sẽ được hoàn sớm nhất có thể ");
            loadOrders('all');
        } else {
            const err = await response.text();
            alert("Lỗi: " + err);
        }
    } catch (e) { console.error(e); alert("Có lỗi xảy ra!"); }
}

async function confirmReceived(event, orderId) {
    event.stopPropagation();
    try {
        const response = await fetch(`${API_BASE_URL}/order/${orderId}/confirm-received`, {
            method: 'PUT',
            headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
        });
        if (response.ok) {
            alert("Cảm ơn bạn đã mua hàng bên tôi!");
            loadOrders('all');
        } else {
            alert("Không thể xác nhận đơn hàng!");
        }
    } catch (e) { alert("Có lỗi xảy ra!"); }
}

async function reorder(event, orderId) {
    event.stopPropagation();

    // 1. Lấy chi tiết đơn hàng (giả sử bạn có API này)
    const response = await fetch(`${API_BASE_URL}/order/${orderId}`, {
        headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const order = await response.json();

    // 2. Map dữ liệu sang mảng các object AddToCartDto
    const itemsToAdd = order.data.details.map(item => ({
        variantId: item.variantId,
        quantity: item.quantity
    }));

    // 3. Gửi danh sách qua API mới
    const cartResponse = await fetch(`${API_BASE_URL}/cart/add-bulk`, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${localStorage.getItem('accessToken')}`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(itemsToAdd)
    });

    if (cartResponse.ok) {
        window.location.href = 'giohang.html';
    }
}

async function reviewOrder(event, orderId) {
    event.stopPropagation();
    document.getElementById('reviewModal').style.display = 'flex';
    document.getElementById('modalOrderId').value = orderId;

    const response = await fetch(`${API_BASE_URL}/order/${orderId}`, {
        headers: { 'Authorization': `Bearer ${localStorage.getItem('accessToken')}` }
    });
    const result = await response.json();

    const listContainer = document.getElementById('reviewProductList');
    listContainer.innerHTML = result.data.details.map(item => {
        const productId = item.productId || (item.product ? item.product.id : null);

        console.log("ID sản phẩm đang dùng:", productId);

        return `
        <div class="review-item" data-product-id="${item.productId}">
        <div class="product-name-in-modal">${item.productName}</div>
            <div class="star-rating" onmouseover="hoverStars(event)" onmouseout="resetStars(event)" onclick="setStars(event)">
                <i class="fa fa-star" data-value="1"></i>
                <i class="fa fa-star" data-value="2"></i>
                <i class="fa fa-star" data-value="3"></i>
                <i class="fa fa-star" data-value="4"></i>
                <i class="fa fa-star" data-value="5"></i>
            </div>
            <input type="hidden" class="rating-value" value="0">
            <textarea class="review-content" placeholder="Chia sẻ cảm nhận của bạn về sản phẩm này..."></textarea>
        </div>
    `;
    }).join('');
}

function closeReviewModal() {
    document.getElementById('reviewModal').style.display = 'none';
}

async function submitReview() {
    const orderId = document.getElementById('modalOrderId').value;
    const items = document.querySelectorAll('.review-item');

    for (const item of items) {
        const productId = item.getAttribute('data-product-id');
        const rating = item.querySelector('.rating-value').value;
        const comment = item.querySelector('.review-content').value;

        if (rating == 0) { alert("Vui lòng chọn số sao!"); return; }

        const response = await fetch(`${API_BASE_URL}/review`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${localStorage.getItem('accessToken')}`
            },
            body: JSON.stringify({
                ProductId: parseInt(productId), 
                OrderId: orderId.trim(),
                Rating: parseInt(rating),
                Comment: comment
            })
        });

        if (response.ok) {
            // Cập nhật UI ngay lập tức cho sản phẩm vừa đánh giá thành công
            const orderCard = document.querySelector(`[data-order-id="${orderId}"]`);
            // Hoặc đơn giản là tìm nút trong toàn trang nếu mã đơn là duy nhất
            const reviewBtns = document.querySelectorAll(`button[onclick*="${orderId}"]`);

            reviewBtns.forEach(btn => {
                btn.disabled = true;
                btn.innerText = "Đã đánh giá";
                btn.style.backgroundColor = "#ccc";
            });
        } else {
            alert("Có lỗi xảy ra với sản phẩm ID: " + productId);
        }
    }

    alert("Đã hoàn tất gửi đánh giá!");
    closeReviewModal();
    loadOrders('all'); // Tải lại danh sách để đồng bộ trạng thái từ Backend
}
function hoverStars(e) {
    if (!e.target.classList.contains('fa-star')) return;
    const val = e.target.getAttribute('data-value');
    const stars = e.target.parentElement.querySelectorAll('.fa-star');
    stars.forEach(s => s.style.color = s.getAttribute('data-value') <= val ? '#ffc107' : '#ccc');
}

function resetStars(e) {
    const parent = e.target.parentElement;
    const currentVal = parent.parentElement.querySelector('.rating-value').value;
    const stars = parent.querySelectorAll('.fa-star');
    stars.forEach(s => s.style.color = s.getAttribute('data-value') <= currentVal ? '#ffc107' : '#ccc');
}

function setStars(e) {
    if (!e.target.classList.contains('fa-star')) return;
    const val = e.target.getAttribute('data-value');
    const parent = e.target.parentElement;
    parent.parentElement.querySelector('.rating-value').value = val;
    parent.querySelectorAll('.fa-star').forEach(s => {
        s.classList.toggle('active', s.getAttribute('data-value') <= val);
    });
}