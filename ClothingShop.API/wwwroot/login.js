const API_BASE_URL = "https://localhost:5001/api";
function displayMessage(title, message) {
    // Tạm thời dùng alert để test không bị sập script. 
    // Sau này bạn có thể thay thế bằng SweetAlert2 hoặc Modal Bootstrap tùy ý.
    alert(`${title}: ${message}`);
}

// ========================================================
// 1. XỬ LÝ ĐĂNG NHẬP (LOGIN)
// ========================================================
const loginForm = document.getElementById('login-form'); // Hãy đảm bảo id này khớp với id của thẻ <form> đăng nhập trong HTML
if (loginForm) {
    document.addEventListener('DOMContentLoaded', function () {
        const loginForm = document.getElementById('login-form');

        if (loginForm) {
            loginForm.addEventListener('submit', async function (e) {
                e.preventDefault();

                // 1. SỬA TẠI ĐÂY: Lấy chính xác phần tử theo id="email_phone" và id="password" từ login.html
                const accountInput = document.getElementById('email_phone');
                const passwordInput = document.getElementById('password');

                // Đoạn mã phòng vệ: Nếu vô tình đổi id ở HTML, JS sẽ cảnh báo chứ không làm sập trang
                if (!accountInput || !passwordInput) {
                    alert("Lỗi hệ thống: Không tìm thấy phần tử id='email_phone' hoặc id='password' trong HTML.");
                    return;
                }

                const accountValue = accountInput.value.trim();
                const passwordValue = passwordInput.value.trim();

                // Kiểm tra nhanh ở Front-end xem người dùng đã nhập đủ chưa
                if (!accountValue || !passwordValue) {
                    alert("Vui lòng nhập đầy đủ Tài khoản và Mật khẩu!");
                    return;
                }

                // 2. Tạo gói tin JSON gửi lên API Login khớp cấu trúc LoginDto ở C#
                const loginBody = {
                    EmailOrPhone: accountValue, // Đổi từ "Email" thành "EmailOrPhone" ở đây
                    Password: passwordValue
                };

                try {
                    // Thực hiện fetch gọi API đăng nhập
                    const response = await fetch(`${API_BASE_URL}/auth/login`, {
                        method: 'POST',
                        headers: {
                            'Content-Type': 'application/json',
                            'Accept': 'application/json'
                        },
                        body: JSON.stringify(loginBody)
                    });

                    const result = await response.json();

                    // 3. Xử lý kết quả trả về từ máy chủ API
                    if (response.ok && result.success) {
                        alert("Đăng nhập thành công!");

                        // 4. LƯU TRỮ TOKEN: Lưu các thông tin bảo mật vào localStorage
                        if (result.data) {
                            localStorage.setItem('accessToken', result.data.token || result.data.accessToken);
                            localStorage.setItem('refreshToken', result.data.refreshToken);

                            // Lưu thông tin vai trò (Role) và thông tin cá nhân cơ bản để hiển thị lên Header trang chủ
                            localStorage.setItem('userRole', result.data.role || 'Customer');
                            localStorage.setItem('userProfile', JSON.stringify({
                                userId: result.data.userId,
                                fullName: result.data.fullName,
                                email: result.data.email
                            }));
                        }

                        // 5. CHUYỂN HƯỚNG: Đưa người dùng về trang chủ mua sắm (hoặc trang admin tùy theo Role)
                        if (result.data && (result.data.role === 'Admin' || result.data.role === 'Staff')) {
                            window.location.href = "index_QTV.html"; // Nếu là admin hoặc nhân viên thì vào trang quản trị
                        } else {
                            window.location.href = "index.html"; // Khách hàng thông thường về trang chủ shop
                        }

                    } else {
                        // Nếu Backend trả về lỗi validation cụ thể (ví dụ lỗi 422)
                        if (result.errors) {
                            const validationErrors = Object.values(result.errors).flat().join('\n');
                            alert("Dữ liệu không hợp lệ:\n" + validationErrors);
                        } else {
                            // Hiển thị thông báo lỗi nghiệp vụ (Ví dụ: "Sai tài khoản hoặc mật khẩu")
                            alert(result.message || "Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.");
                        }
                    }
                } catch (error) {
                    console.error("Login Fetch Error:", error);
                    alert("Không thể kết nối đến máy chủ API. Vui lòng kiểm tra xem Backend C# đã khởi chạy chưa.");
                }
            });
        }
    });
}

// ========================================================
// 2. XỬ LÝ ĐĂNG KÝ (REGISTER)
// ========================================================
const registerForm = document.getElementById('register-form');
if (registerForm) {
    registerForm.addEventListener('submit', async function (e) {
        e.preventDefault();

        const fullnameValue = document.getElementById('fullname').value.trim();
        const phoneValue = document.getElementById('reg-phone').value.trim();
        const emailValue = document.getElementById('reg-email').value.trim();
        const passwordValue = document.getElementById('reg-password').value.trim();
        const confirmPasswordValue = document.getElementById('confirm-password').value.trim();

        if (passwordValue !== confirmPasswordValue) {
            alert("Mật khẩu xác nhận không trùng khớp!");
            return;
        }

        // Định dạng Object khớp chuẩn 100% với RegisterDto trong C#
        const registerBody = {
            Email: emailValue,
            Password: passwordValue,
            ConfirmPassword: confirmPasswordValue,
            Phone: phoneValue,
            FullName: fullnameValue
        };

        try {
            const response = await fetch(`${API_BASE_URL}/auth/register`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(registerBody)
            });

            const result = await response.json();

            if (response.ok && result.success) {
                alert("Đăng ký tài khoản thành công!");

                // Lưu Token tự động đăng nhập từ LoginResultDto backend trả về
                if (result.data) {
                    localStorage.setItem('accessToken', result.data.token);
                    localStorage.setItem('refreshToken', result.data.refreshToken);
                    localStorage.setItem('userRole', 'Customer');
                }
                window.location.href = "login.html";
            } else {
                // ĐỌC CHI TIẾT LỖI 422: Nếu Backend trả về danh sách lỗi validation cụ thể
                if (result.errors) {
                    const validationErrors = Object.values(result.errors).flat().join('\n');
                    alert("Dữ liệu không hợp lệ:\n" + validationErrors);
                } else {
                    // Hiện câu thông báo từ lớp ApiResponse (Ví dụ: "Email đã được sử dụng")
                    alert(result.message || "Đăng ký thất bại: Vui lòng kiểm tra lại thông tin.");
                }
            }
        } catch (error) {
            console.error("Fetch Error:", error);
            alert("Không thể kết nối đến máy chủ API. Vui lòng kiểm tra Backend.");
        }
    });
}

async function autoRefreshAccessToken() {
    const refreshToken = localStorage.getItem('refreshToken');
    if (!refreshToken) return;

    try {
        const response = await fetch(`${API_BASE_URL}/auth/refresh-token`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: refreshToken }) // Hoặc truyền chuỗi trần tùy cấu hình Route API
        });
        const result = await response.json();
        if (response.ok && result.success) {
            // Ghi đè Token mới và Refresh Token xoay vòng mới vào bộ nhớ
            localStorage.setItem('accessToken', result.data.token);
            localStorage.setItem('refreshToken', result.data.refreshToken);
            console.log("Access Token đã được cập nhật tự động thành công!");
        }
    } catch (err) {
        console.error("Không thể tự động làm mới mã Token", err);
    }
}