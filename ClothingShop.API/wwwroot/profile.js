document.addEventListener('DOMContentLoaded', () => {
    // 1. Lấy dữ liệu người dùng từ localStorage
    const user = JSON.parse(localStorage.getItem('user')) || {};

    // 2. Điền vào các ô input
    if (user.username) document.getElementById('username').value = user.username;
    if (user.fullname) document.getElementById('fullname').value = user.fullname;

    // Xử lý hiển thị SĐT (ẩn bớt số)
    if (user.phone) {
        const maskedPhone = "*******" + user.phone.slice(-3);
        document.querySelector('.info-static').innerText = maskedPhone;
    }

    // 3. Khởi tạo danh sách Ngày/Tháng/Năm
    initDateSelectors();

});

