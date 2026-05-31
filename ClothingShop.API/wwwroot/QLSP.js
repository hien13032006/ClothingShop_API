// 1. Khởi tạo dữ liệu mẫu với đầy đủ category và gender
const initialProducts = [
    { id: "SP001", name: "Áo babydoll ROSE", category: "ao", gender: "nu", price: 169000, stock: 15, img: "images/a1.jpg" },
    { id: "SP002", name: "Quần jeans nam basic", category: "quan", gender: "nam", price: 209000, stock: 5, img: "images/a3.jpg" },
    { id: "SP003", name: "Váy hoa nhí bé gái", category: "vay", gender: "treem", price: 120000, stock: 0, img: "images/a2.jpg" },
    { id: "SP004", name: "Áo sơ mi lụa nam", category: "ao", gender: "nam", price: 350000, stock: 10, img: "images/a4.jpg" }
];

// Luôn cập nhật LocalStorage để đảm bảo cấu trúc dữ liệu mới nhất
if (!localStorage.getItem('products')) {
    localStorage.setItem('products', JSON.stringify(initialProducts));
}

let currentGenderFilter = 'all';

// 2. Hàm hiển thị danh sách sản phẩm ra bảng
function renderProducts(list) {
    const tbody = document.getElementById('product-list');
    if (!tbody) return;

    if (list.length === 0) {
        tbody.innerHTML = `<tr><td colspan="6" style="text-align:center; padding:20px;">Không tìm thấy sản phẩm phù hợp.</td></tr>`;
        return;
    }

    tbody.innerHTML = list.map(p => {
        // Ánh xạ mã category sang tên hiển thị tiếng Việt
        const categoryMap = {
            'ao': 'ÁO',
            'quan': 'QUẦN',
            'vay': 'VÁY/ĐẦM'
        };
        const categoryDisplay = categoryMap[p.category] || p.category.toUpperCase();

        return `
        <tr>
            <td>
                <img src="${p.img}" 
                     onerror="this.src='https://via.placeholder.com/50'" 
                     style="width: 50px; height: 50px; object-fit: cover; border-radius: 4px; border: 1px solid #eee;">
            </td>
            <td style="font-weight: bold;">${p.name}</td>
            <td>${categoryDisplay}</td> 
            <td>${p.price.toLocaleString()}₫</td>
            <td>
                <span class="badge ${p.stock > 0 ? 'badge-shipping' : 'badge-cancelled'}">
                    ${p.stock > 0 ? 'Còn ' + p.stock : 'Hết hàng'}
                </span>
            </td>
            <td>
                <button class="btn-edit" onclick="editProduct('${p.id}')" title="Sửa"><i class="fa-solid fa-pen"></i></button>
                <button class="btn-delete" onclick="deleteProduct('${p.id}')" title="Xóa"><i class="fa-solid fa-trash"></i></button>
            </td>
        </tr>
    `;
    }).join('');
}

// 3. Logic lọc dữ liệu theo Gender (từ Tabs)
window.filterByGender = function (gender, el) {
    // Cập nhật trạng thái Active cho Tab
    document.querySelectorAll('.tab-item').forEach(t => t.classList.remove('active'));
    el.classList.add('active');

    currentGenderFilter = gender;
    handleFilter();
};

// 4. Hàm lọc tổng hợp (Tab + Search)
window.handleFilter = function () {
    const keyword = document.getElementById('product-search').value.toLowerCase();
    const allProducts = JSON.parse(localStorage.getItem('products')) || [];

    const filtered = allProducts.filter(p => {
        // Lọc theo Tab (Đối tượng giới tính hoặc Hết hàng)
        let matchTab = false;
        if (currentGenderFilter === 'all') {
            matchTab = true;
        } else if (currentGenderFilter === 'out-of-stock') {
            matchTab = (p.stock === 0);
        } else {
            matchTab = (p.gender === currentGenderFilter);
        }

        // Lọc theo từ khóa tìm kiếm (Tên hoặc ID)
        const matchKey = p.name.toLowerCase().includes(keyword) ||
            p.id.toLowerCase().includes(keyword);

        return matchTab && matchKey;
    });

    renderProducts(filtered);
};

// 5. Khởi tạo các sự kiện khi tải trang
document.addEventListener('DOMContentLoaded', () => {
    // Luôn hiển thị dữ liệu từ LocalStorage khi nạp trang
    const currentData = JSON.parse(localStorage.getItem('products')) || initialProducts;
    renderProducts(currentData);

    // Sự kiện vừa nhập vừa lọc (Real-time search)
    const searchInput = document.getElementById('product-search');
    if (searchInput) {
        searchInput.addEventListener('input', handleFilter);
    }
});

// 6. Xóa sản phẩm
function deleteProduct(id) {
    if (confirm("Bạn có chắc chắn muốn xóa sản phẩm này?")) {
        let all = JSON.parse(localStorage.getItem('products')) || [];
        all = all.filter(p => p.id !== id);
        localStorage.setItem('products', JSON.stringify(all));
        handleFilter(); // Load lại danh sách sau khi xóa
    }
}

// 7. Mở Modal/Trang sửa (Hàm chờ)
function editProduct(id) {
    console.log("Sửa sản phẩm mã:", id);
    // Bạn có thể chuyển hướng: window.location.href = `SuaSanPham.html?id=${id}`;
}