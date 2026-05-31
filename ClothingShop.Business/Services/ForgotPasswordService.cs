using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IForgotPasswordService
    {
        Task<ApiResponse<string>> SendOtpAsync(string email);
        Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto);
        Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto);
    }

    public class ForgotPasswordService : IForgotPasswordService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IEmailService       _emailService;
        private readonly IAuthService        _authService;
        private readonly AppDbContext        _context;

        public ForgotPasswordService(
            ICustomerRepository customerRepo,
            IEmailService       emailService,
            IAuthService        authService,
            AppDbContext        context)
        {
            _customerRepo = customerRepo;
            _emailService = emailService;
            _authService  = authService;
            _context      = context;
        }

        /// <summary>Bước 1: Gửi OTP 6 số về email</summary>
        public async Task<ApiResponse<string>> SendOtpAsync(string email)
        {
            var customer = await _customerRepo.GetByEmailAsync(email.Trim().ToLower());
            if (customer == null)
                return ApiResponse<string>.Fail("Email không tồn tại trong hệ thống");

            if (customer.Provider != "Local")
                return ApiResponse<string>.Fail(
                    $"Tài khoản này đăng nhập bằng {customer.Provider}, không dùng mật khẩu");

            // Vô hiệu hóa OTP cũ chưa dùng
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == customer.UserId && !t.IsUsed)
                .ToListAsync();
            foreach (var t in oldTokens) t.IsUsed = true;

            // Tạo OTP mới 6 chữ số
            var otp = new Random().Next(100000, 999999).ToString();
            await _context.PasswordResetTokens.AddAsync(new PasswordResetToken
            {
                UserId    = customer.UserId,
                Token     = otp,
                ExpiresAt = DateTime.Now.AddMinutes(15),
                IsUsed    = false
            });
            await _context.SaveChangesAsync();

            // Gửi email
            await _emailService.SendAsync(
                to:      email,
                subject: "[ClothingShop] Mã xác nhận đặt lại mật khẩu",
                body:    $@"
                    <p>Xin chào <strong>{customer.FullName ?? email}</strong>,</p>
                    <p>Mã OTP đặt lại mật khẩu của bạn là:</p>
                    <h2 style='color:#e74c3c;letter-spacing:8px'>{otp}</h2>
                    <p>Mã có hiệu lực trong <strong>15 phút</strong>. Không chia sẻ mã này với ai.</p>
                    <p>Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                "
            );

            return ApiResponse<string>.Ok("SENT",
                $"Đã gửi mã OTP đến {MaskEmail(email)}. Kiểm tra hộp thư (và thư rác).");
        }

        /// <summary>Bước 2: Xác minh OTP — trả về reset token</summary>
        public async Task<ApiResponse<string>> VerifyOtpAsync(VerifyOtpDto dto)
        {
            var customer = await _customerRepo.GetByEmailAsync(dto.Email.Trim().ToLower());
            if (customer == null)
                return ApiResponse<string>.Fail("Email không tồn tại");

            var record = await _context.PasswordResetTokens
                .Where(t => t.UserId == customer.UserId && t.Token == dto.Otp && !t.IsUsed)
                .OrderByDescending(t => t.ExpiresAt)
                .FirstOrDefaultAsync();

            if (record == null || !record.IsValid())
                return ApiResponse<string>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn");

            // Đánh dấu dùng rồi và cấp reset token (UUID) để đặt mật khẩu mới
            record.IsUsed = true;
            var resetToken = Guid.NewGuid().ToString("N");

            await _context.PasswordResetTokens.AddAsync(new PasswordResetToken
            {
                UserId    = customer.UserId,
                Token     = resetToken,
                ExpiresAt = DateTime.Now.AddMinutes(10),  // 10 phút để đặt lại
                IsUsed    = false
            });
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok(resetToken, "OTP hợp lệ. Dùng resetToken để đặt mật khẩu mới.");
        }

        /// <summary>Bước 3: Đặt mật khẩu mới bằng resetToken từ bước 2</summary>
        public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return ApiResponse<string>.Fail("Mật khẩu xác nhận không khớp");

            var record = await _context.PasswordResetTokens
                .Include(t => t.Customer)
                .FirstOrDefaultAsync(t => t.Token == dto.ResetToken && !t.IsUsed);

            if (record == null || !record.IsValid())
                return ApiResponse<string>.Fail("Reset token không hợp lệ hoặc đã hết hạn");

            record.Customer!.Password = _authService.HashPassword(dto.NewPassword);
            record.IsUsed = true;
            await _context.SaveChangesAsync();

            return ApiResponse<string>.Ok("OK", "Đặt lại mật khẩu thành công. Vui lòng đăng nhập lại.");
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2) return email;
            var name = parts[0];
            var masked = name.Length <= 2 ? name : name[..2] + "***";
            return $"{masked}@{parts[1]}";
        }
    }
}
