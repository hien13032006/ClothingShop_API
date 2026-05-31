using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClothingShop.Business.Services
{
    public interface IPaymentService
    {
        /// <summary>Tạo URL thanh toán online (mô phỏng VNPay/MoMo)</summary>
        Task<ApiResponse<string>> CreatePaymentUrlAsync(string orderId, string returnUrl);

        /// <summary>Nhận callback từ cổng thanh toán sau khi khách thanh toán</summary>
        Task<ApiResponse<string>> HandleCallbackAsync(PaymentCallbackDto dto);

        /// <summary>Kiểm tra trạng thái thanh toán của đơn hàng</summary>
        Task<ApiResponse<PaymentInfoDto>> GetPaymentStatusAsync(string orderId);
    }

    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(AppDbContext context, IConfiguration config, ILogger<PaymentService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<ApiResponse<string>> CreatePaymentUrlAsync(string orderId, string returnUrl)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
                return ApiResponse<string>.Fail("Không tìm thấy đơn hàng");

            if (order.Payment?.PaymentMethod != "Thanh toán online")
                return ApiResponse<string>.Fail("Đơn hàng này không sử dụng thanh toán online");

            if (order.Payment.PaymentStatus == "Đã thanh toán")
                return ApiResponse<string>.Fail("Đơn hàng đã được thanh toán rồi");

            // -------------------------------------------------------
            // Mô phỏng URL cổng thanh toán
            // Thực tế: gọi API của VNPay / MoMo / ZaloPay để lấy URL
            // -------------------------------------------------------
            var secretKey = _config["Payment:SecretKey"] ?? "default-secret-key";
            var amount = (long)(order.FinalPrice * 100); // Đơn vị: đồng x100
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var rawData = $"{orderId}|{amount}|{timestamp}";
            var signature = ComputeHmac(rawData, secretKey);

            var paymentUrl = $"https://sandbox.payment-gateway.vn/pay" +
                             $"?orderId={orderId}" +
                             $"&amount={amount}" +
                             $"&returnUrl={Uri.EscapeDataString(returnUrl)}" +
                             $"&timestamp={timestamp}" +
                             $"&signature={signature}";

            _logger.LogInformation("Created payment URL for order {OrderId}", orderId);
            return ApiResponse<string>.Ok(paymentUrl, "Tạo URL thanh toán thành công");
        }

        public async Task<ApiResponse<string>> HandleCallbackAsync(PaymentCallbackDto dto)
        {
            // 1. Verify chữ ký HMAC để đảm bảo callback là thật
            var secretKey = _config["Payment:SecretKey"] ?? "default-secret-key";
            var rawData = $"{dto.OrderId}|{(long)(dto.Amount * 100)}";
            var expectedSig = ComputeHmac(rawData, secretKey);

            if (!string.Equals(dto.Signature, expectedSig, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Invalid payment signature for order {OrderId}", dto.OrderId);
                return ApiResponse<string>.Fail("Chữ ký không hợp lệ — có thể là yêu cầu giả mạo");
            }

            // 2. Tìm bản ghi Payment
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == dto.OrderId);

            if (payment == null)
                return ApiResponse<string>.Fail("Không tìm thấy bản ghi thanh toán");

            if (payment.PaymentStatus == "Đã thanh toán")
                return ApiResponse<string>.Ok("OK", "Đơn hàng đã được xác nhận trước đó");

            // 3. Cập nhật trạng thái
            if (dto.Status == "success")
            {
                payment.PaymentStatus = "Đã thanh toán";
                payment.TransactionId = dto.TransactionId;
                payment.PaymentDate = DateTime.Now;

                // Cập nhật trạng thái đơn hàng sang "Đang chuẩn bị"
                var order = await _context.Orders.FindAsync(dto.OrderId);
                if (order != null && order.Status == "Chờ xác nhận")
                    order.Status = "Đang chuẩn bị";

                _logger.LogInformation("Payment SUCCESS for order {OrderId}, txn {TxnId}",
                    dto.OrderId, dto.TransactionId);
            }
            else
            {
                payment.PaymentStatus = "Thất bại";
                _logger.LogWarning("Payment FAILED for order {OrderId}", dto.OrderId);
            }

            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK",
                dto.Status == "success" ? "Thanh toán thành công" : "Thanh toán thất bại");
        }

        public async Task<ApiResponse<PaymentInfoDto>> GetPaymentStatusAsync(string orderId)
        {
            var payment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId);

            if (payment == null)
                return ApiResponse<PaymentInfoDto>.Fail("Không tìm thấy thông tin thanh toán");

            return ApiResponse<PaymentInfoDto>.Ok(new PaymentInfoDto
            {
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus,
                TransactionId = payment.TransactionId,
                AmountPaid = payment.AmountPaid
            });
        }

        // ===== Helper =====
        private static string ComputeHmac(string data, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash).ToLower();
        }
    }
}
