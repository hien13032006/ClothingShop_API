using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IAuthService
    {
        Task<ApiResponse<LoginResultDto>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<LoginResultDto>> LoginAsync(LoginDto dto);
        Task<ApiResponse<LoginResultDto>> GoogleLoginAsync(GoogleLoginDto dto);
        Task<ApiResponse<LoginResultDto>> RefreshTokenAsync(string refreshToken);
        Task<ApiResponse<string>> RevokeTokenAsync(string refreshToken);
        string HashPassword(string password);
        bool VerifyPassword(string password, string hash);
    }

    public class AuthService : IAuthService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IConfiguration      _config;
        private readonly AppDbContext        _context;

        public AuthService(ICustomerRepository customerRepo, IConfiguration config, AppDbContext context)
        {
            _customerRepo = customerRepo;
            _config       = config;
            _context      = context;
        }

        // ── Register ──────────────────────────────────────────────────
        public async Task<ApiResponse<LoginResultDto>> RegisterAsync(RegisterDto dto)
        {
            if (await _customerRepo.EmailExistsAsync(dto.Email))
                return ApiResponse<LoginResultDto>.Fail("Email đã được sử dụng");

            if (!string.IsNullOrEmpty(dto.Phone) && await _customerRepo.PhoneExistsAsync(dto.Phone))
                return ApiResponse<LoginResultDto>.Fail("Số điện thoại đã được sử dụng");

            var newId    = await _customerRepo.GenerateNextUserIdAsync();
            var customer = new Customer
            {
                UserId          = newId,
                Email           = dto.Email.Trim().ToLower(),
                Password        = HashPassword(dto.Password),
                Phone           = dto.Phone?.Trim(),
                FullName        = dto.FullName?.Trim(),
                Provider        = "Local",
                MembershipLevel = "Bạc",
                CreatedAt       = DateTime.Now
            };

            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();
            return ApiResponse<LoginResultDto>.Ok(await BuildResultAsync(customer), "Đăng ký thành công");
        }

        // ── Login ─────────────────────────────────────────────────────
        public async Task<ApiResponse<LoginResultDto>> LoginAsync(LoginDto dto)
        {
            var customer = await _customerRepo.GetByEmailOrPhoneAsync(dto.EmailOrPhone.Trim());
            if (customer == null)
                return ApiResponse<LoginResultDto>.Fail("Email/SĐT không tồn tại trong hệ thống");

            if (customer.Provider != "Local")
                return ApiResponse<LoginResultDto>.Fail(
                    $"Tài khoản này đăng nhập bằng {customer.Provider}. Vui lòng dùng phương thức đó.");

            if (!VerifyPassword(dto.Password, customer.Password))
                return ApiResponse<LoginResultDto>.Fail("Mật khẩu không đúng");

            return ApiResponse<LoginResultDto>.Ok(await BuildResultAsync(customer), "Đăng nhập thành công");
        }

        // ── Google OAuth ──────────────────────────────────────────────
        public async Task<ApiResponse<LoginResultDto>> GoogleLoginAsync(GoogleLoginDto dto)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
            }
            catch
            {
                return ApiResponse<LoginResultDto>.Fail("Google token không hợp lệ hoặc đã hết hạn");
            }

            var existing = await _customerRepo.GetByEmailAsync(payload.Email);
            if (existing != null)
            {
                if (existing.Provider == "Local")
                    return ApiResponse<LoginResultDto>.Fail(
                        "Email này đã đăng ký bằng mật khẩu. Vui lòng đăng nhập thường.");
                return ApiResponse<LoginResultDto>.Ok(
                    await BuildResultAsync(existing), "Đăng nhập Google thành công");
            }

            var newId    = await _customerRepo.GenerateNextUserIdAsync();
            var customer = new Customer
            {
                UserId          = newId,
                Email           = payload.Email.ToLower(),
                FullName        = payload.Name,
                Password        = HashPassword(Guid.NewGuid().ToString()),
                Provider        = "Google",
                MembershipLevel = "Bạc",
                CreatedAt       = DateTime.Now
            };

            await _customerRepo.AddAsync(customer);
            await _customerRepo.SaveChangesAsync();
            return ApiResponse<LoginResultDto>.Ok(
                await BuildResultAsync(customer), "Đăng nhập Google thành công");
        }

        // ── Refresh Token ─────────────────────────────────────────────
        public async Task<ApiResponse<LoginResultDto>> RefreshTokenAsync(string refreshToken)
        {
            var storedToken = await _context.RefreshTokens
                .Include(r => r.Customer)
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (storedToken == null)
                return ApiResponse<LoginResultDto>.Fail("Refresh token không tồn tại");

            if (!storedToken.IsActive)
                return ApiResponse<LoginResultDto>.Fail("Refresh token đã hết hạn hoặc bị thu hồi");

            // Token rotation: vô hiệu hóa token cũ, cấp token mới
            var newRefreshToken = await GenerateRefreshTokenAsync(storedToken.UserId);
            storedToken.IsRevoked       = true;
            storedToken.ReplacedByToken = newRefreshToken.Token;
            await _context.SaveChangesAsync();

            var customer = storedToken.Customer!;
            var jwt      = GenerateJwt(customer);

            return ApiResponse<LoginResultDto>.Ok(new LoginResultDto
            {
                UserId          = customer.UserId,
                FullName        = customer.FullName ?? "",
                Email           = customer.Email,
                MembershipLevel = customer.MembershipLevel,
                Token           = jwt,
                RefreshToken    = newRefreshToken.Token,
                TokenExpiry     = DateTime.Now.AddHours(2)
            }, "Token đã được làm mới");
        }

        public async Task<ApiResponse<string>> RevokeTokenAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(r => r.Token == refreshToken);

            if (token == null || !token.IsActive)
                return ApiResponse<string>.Fail("Token không hợp lệ hoặc đã bị thu hồi");

            token.IsRevoked = true;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã thu hồi token thành công");
        }

        // ── Helpers ───────────────────────────────────────────────────
        public string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        public bool VerifyPassword(string password, string hash)
            => BCrypt.Net.BCrypt.Verify(password, hash);

        private string GenerateJwt(Customer customer)
        {
            var key    = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new Exception("Jwt:Key missing")));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, customer.UserId),
                new Claim(ClaimTypes.Email,          customer.Email),
                new Claim("membership",              customer.MembershipLevel),
                new Claim(ClaimTypes.Role,           customer.UserId.StartsWith("AD") ? "Admin" : "Customer")
            };
            var token = new JwtSecurityToken(
                issuer:  _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims:   claims,
                expires:  DateTime.Now.AddHours(2),    // Access token ngắn hạn: 2 giờ
                signingCredentials: creds);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<RefreshToken> GenerateRefreshTokenAsync(string userId)
        {
            var token = new RefreshToken
            {
                UserId    = userId,
                Token     = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
                ExpiresAt = DateTime.Now.AddDays(30),
                CreatedAt = DateTime.Now
            };
            await _context.RefreshTokens.AddAsync(token);
            await _context.SaveChangesAsync();
            return token;
        }

        private async Task<LoginResultDto> BuildResultAsync(Customer c)
        {
            var refreshToken = await GenerateRefreshTokenAsync(c.UserId);
            return new LoginResultDto
            {
                UserId          = c.UserId,
                FullName        = c.FullName ?? "",
                Email           = c.Email,
                MembershipLevel = c.MembershipLevel,
                Token           = GenerateJwt(c),
                RefreshToken    = refreshToken.Token,
                TokenExpiry     = DateTime.Now.AddHours(2)
            };
        }
    }
}
