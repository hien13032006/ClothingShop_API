using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService           _authService;
        private readonly IForgotPasswordService _forgotService;

        public AuthController(IAuthService authService, IForgotPasswordService forgotService)
        {
            _authService   = authService;
            _forgotService = forgotService;
        }

        /// <summary>POST /api/auth/register</summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var r = await _authService.RegisterAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>POST /api/auth/login</summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var r = await _authService.LoginAsync(dto);
            return r.Success ? Ok(r) : Unauthorized(r);
        }

        /// <summary>POST /api/auth/google — Đăng nhập / đăng ký bằng Google</summary>
        [HttpPost("google")]
        public async Task<IActionResult> Google([FromBody] GoogleLoginDto dto)
        {
            var r = await _authService.GoogleLoginAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>POST /api/auth/refresh — Làm mới JWT bằng refresh token</summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
        {
            var r = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return r.Success ? Ok(r) : Unauthorized(r);
        }

        /// <summary>POST /api/auth/revoke — Đăng xuất (thu hồi refresh token)</summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] RefreshTokenDto dto)
        {
            var r = await _authService.RevokeTokenAsync(dto.RefreshToken);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ── Quên mật khẩu 3 bước ─────────────────────────────────────

        /// <summary>POST /api/auth/forgot-password — Bước 1: Gửi OTP về email</summary>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var r = await _forgotService.SendOtpAsync(dto.Email);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>POST /api/auth/verify-otp — Bước 2: Xác minh OTP, nhận reset token</summary>
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto dto)
        {
            var r = await _forgotService.VerifyOtpAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>POST /api/auth/reset-password — Bước 3: Đặt mật khẩu mới</summary>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var r = await _forgotService.ResetPasswordAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
