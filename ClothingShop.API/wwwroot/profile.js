const BACKEND_BASE_URL = "https://localhost:5001"; // Giữ nguyên cổng Port 5001 của bạn

document.addEventListener("DOMContentLoaded", async () => {
    // Tải dữ liệu hồ sơ từ API Server lên Form thông tin cá nhân
    await loadUserProfile();

    // 🌟 Tải thêm danh sách địa chỉ nhận hàng của khách hàng
    await loadAddressList();

    // Lắng nghe sự kiện bấm nút Lưu thông tin cá nhân
    const form = document.getElementById("profile-form");
    if (form) {
        form.addEventListener("submit", handleSaveProfile);
    }

    // 🌟 Lắng nghe sự kiện submit Form thêm địa chỉ mới (Thay thế cho onsubmit inline)
    const addressForm = document.getElementById("address-form");
    if (addressForm) {
        addressForm.addEventListener("submit", handleSaveAddress);
    }
});

// ==========================================
// ĐOẠN 1: QUẢN LÝ THÔNG TIN CÁ NHÂN (Cũ)
// ==========================================

async function loadUserProfile() {
    const token = localStorage.getItem("accessToken");
    if (!token) {
        alert("Vui lòng đăng nhập để xem thông tin hồ sơ!");
        window.location.href = "login.html";
        return;
    }

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/profile`, {
            method: "GET",
            headers: { "Authorization": `Bearer ${token}` }
        });

        const result = await response.json();

        if (response.ok && result.success) {
            const user = result.data;

            document.getElementById("profile-email").innerText = user.email || "Chưa cập nhật";
            document.getElementById("fullname").value = user.fullName || "";
            document.getElementById("phone").value = user.phone || "";
            document.getElementById("membership-level").innerText = user.membershipLevel || "Bạc";
            document.getElementById("total-points").innerText = `${user.totalPoints || 0} điểm`;

            if (user.createdAt) {
                const date = new Date(user.createdAt);
                document.getElementById("created-at").innerText = date.toLocaleDateString('vi-VN');
            }
        }
    } catch (error) {
        console.error("Lỗi kết nối API Profile:", error);
    }
}

async function handleSaveProfile(e) {
    e.preventDefault();

    const token = localStorage.getItem("accessToken");
    if (!token) return;

    const fullName = document.getElementById("fullname").value.trim();
    const phone = document.getElementById("phone").value.trim();

    const updateDto = {
        FullName: fullName,
        Phone: phone
    };

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/profile`, {
            method: "PUT",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json"
            },
            body: JSON.stringify(updateDto)
        });

        const result = await response.json();

        if (response.ok && result.success) {
            alert("Cập nhật thông tin hồ sơ thành công!");
            await loadUserProfile();
        } else {
            alert(result.message || "Cập nhật thất bại.");
        }
    } catch (error) {
        console.error("Lỗi gửi dữ liệu cập nhật:", error);
        alert("Không thể kết nối đến máy chủ.");
    }
}

// ==========================================
// 🌟 ĐOẠN 2: PHẦN MỚI - QUẢN LÝ SỔ ĐỊA CHỈ NHẬN HÀNG
// ==========================================

// 1. Hàm tải danh sách địa chỉ từ API và render động lên giao diện HTML
async function loadAddressList() {
    const container = document.getElementById("address-list-container");
    if (!container) return;

    const token = localStorage.getItem("accessToken");
    if (!token) return;

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/addresses`, {
            method: "GET",
            headers: { "Authorization": `Bearer ${token}` }
        });
        const result = await response.json();

        if (response.ok && result.success && result.data && result.data.length > 0) {
            // Render danh sách bằng các Class đã được định nghĩa trong file profile_address.css
            container.innerHTML = result.data.map(addr => `
                <div class="address-item">
                    <div>
                        <div class="address-meta-row">
                            <strong>${addr.receiverName}</strong>
                            <span class="phone-split">| (+84) ${addr.receiverPhone}</span>
                            <span class="address-type-tag">${addr.addressType}</span>
                        </div>
                        <div class="address-detail-text">${addr.addressDetail}</div>
                        ${addr.isDefault ? `<div class="default-badge-container"><span class="default-badge">Mặc định</span></div>` : ''}
                    </div>
                    <div class="address-actions-box">
                        <button type="button" onclick="deleteAddress(${addr.addressId})" class="btn-delete-address">
                            <i class="fa-solid fa-trash"></i> Xóa
                        </button>
                        ${!addr.isDefault ? `<button type="button" onclick="setDefaultAddress(${addr.addressId})" class="btn-set-default">Thiết lập mặc định</button>` : ''}
                    </div>
                </div>
            `).join('');
        } else {
            container.innerHTML = `<p style="color: #999; text-align: center; padding: 20px 0;">Bạn chưa lưu địa chỉ giao hàng nào.</p>`;
        }
    } catch (err) {
        console.error("Lỗi lấy danh sách địa chỉ:", err);
        container.innerHTML = `<p style="color: red;">Không thể kết nối dữ liệu sổ địa chỉ.</p>`;
    }
}

// 2. Logic bật/tắt Modal nhập địa chỉ mới
function openAddressModal() {
    const modal = document.getElementById("address-modal");
    if (modal) modal.style.display = "flex";
}

function closeAddressModal() {
    const modal = document.getElementById("address-modal");
    if (modal) {
        modal.style.display = "none";
        document.getElementById("address-form").reset(); // Reset trống form sau khi đóng
    }
}

// 3. Hàm gửi dữ liệu Thêm địa chỉ mới lên API Backend
async function handleSaveAddress(e) {
    e.preventDefault();
    const token = localStorage.getItem("accessToken");
    if (!token) return;

    const addressTypeEl = document.querySelector('input[name="address-type"]:checked');

    const dto = {
        ReceiverName: document.getElementById("receiver-name").value.trim(),
        ReceiverPhone: document.getElementById("receiver-phone").value.trim(),
        AddressDetail: document.getElementById("address-detail").value.trim(),
        AddressType: addressTypeEl ? addressTypeEl.value : "Nhà riêng",
        IsDefault: document.getElementById("is-default").checked
    };

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/addresses`, {
            method: "POST",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Content-Type": "application/json",
                "Accept": "application/json"
            },
            body: JSON.stringify(dto)
        });
        const result = await response.json();

        if (response.ok && result.success) {
            closeAddressModal();     // Đóng form
            await loadAddressList(); // Tải lại danh sách ngầm (không reload trang)
        } else {
            alert(result.message || "Không thể thêm địa chỉ.");
        }
    } catch (err) {
        console.error("Lỗi thêm mới địa chỉ:", err);
        alert("Không thể kết nối đến máy chủ.");
    }
}

// 4. Hàm gọi API đặt địa chỉ được chọn làm địa chỉ mặc định
async function setDefaultAddress(addressId) {
    const token = localStorage.getItem("accessToken");
    if (!token) return;

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/addresses/${addressId}/set-default`, {
            method: "PUT",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Accept": "application/json"
            }
        });
        const result = await response.json();

        if (response.ok && result.success) {
            await loadAddressList(); // Làm mới lại danh sách
        } else {
            alert(result.message || "Không thể đặt làm mặc định.");
        }
    } catch (err) {
        console.error("Lỗi thiết lập địa chỉ mặc định:", err);
    }
}

// 5. Hàm gọi API Xóa địa chỉ
async function deleteAddress(addressId) {
    if (!confirm("Bạn có chắc chắn muốn xóa địa chỉ giao hàng này không?")) return;

    const token = localStorage.getItem("accessToken");
    if (!token) return;

    try {
        const response = await fetch(`${BACKEND_BASE_URL}/api/customer/addresses/${addressId}`, {
            method: "DELETE",
            headers: {
                "Authorization": `Bearer ${token}`,
                "Accept": "application/json"
            }
        });
        const result = await response.json();

        if (response.ok && result.success) {
            await loadAddressList(); // Làm mới lại danh sách sau khi xóa thành công
        } else {
            alert(result.message || "Không thể xóa địa chỉ này.");
        }
    } catch (err) {
        console.error("Lỗi xóa địa chỉ:", err);
    }
}