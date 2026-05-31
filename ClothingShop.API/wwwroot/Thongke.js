// 1. Mockup dữ liệu tổng quan
const dashboardData = {
    adminName: "Lê Văn B",
    stats: [
        { label: "Doanh thu ngày", value: "5.400.000đ", icon: "fa-money-bill-trend-up", color: "#2ecc71" },
        { label: "Đơn hàng mới", value: "12", icon: "fa-cart-plus", color: "#3498db" },
        { label: "Khách hàng mới", value: "45", icon: "fa-user-plus", color: "#f1c40f" },
        { label: "Sản phẩm sắp hết", value: "3", icon: "fa-triangle-exclamation", color: "#e74c3c" }
    ],
    revenueMonths: ["T1", "T2", "T3", "T4", "T5", "T6"],
    revenueValues: [12000000, 19000000, 15000000, 25000000, 22000000, 30000000],
    categories: ["Áo thun", "Quần Jean", "Áo khoác", "Váy"],
    categoryCounts: [40, 25, 20, 15]
};

document.addEventListener('DOMContentLoaded', () => {
    // Đổ tên Admin
    document.getElementById('admin-name').innerText = dashboardData.adminName;

    // Render các thẻ thống kê
    const statsContainer = document.getElementById('stats-container');
    statsContainer.innerHTML = dashboardData.stats.map(s => `
        <div class="stat-card">
            <div class="stat-icon" style="background: ${s.color}"><i class="fa-solid ${s.icon}"></i></div>
            <div class="stat-info">
                <h4>${s.label}</h4>
                <p>${s.value}</p>
            </div>
        </div>
    `).join('');

    // Vẽ biểu đồ doanh thu (Line Chart)
    const ctxRevenue = document.getElementById('revenueChart').getContext('2d');
    new Chart(ctxRevenue, {
        type: 'line',
        data: {
            labels: dashboardData.revenueMonths,
            datasets: [{
                label: 'Doanh thu (VNĐ)',
                data: dashboardData.revenueValues,
                borderColor: '#3498db',
                backgroundColor: 'rgba(52, 152, 219, 0.1)',
                fill: true,
                tension: 0.4
            }]
        }
    });

    // Vẽ biểu đồ tròn (Pie Chart)
    const ctxCategory = document.getElementById('categoryChart').getContext('2d');
    new Chart(ctxCategory, {
        type: 'doughnut',
        data: {
            labels: dashboardData.categories,
            datasets: [{
                data: dashboardData.categoryCounts,
                backgroundColor: ['#e74c3c', '#3498db', '#2ecc71', '#f1c40f']
            }]
        }
    });
});