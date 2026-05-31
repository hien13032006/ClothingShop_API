using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// POST /api/payment/{orderId}/create-url
        /// Tạo URL để redirect sang cổng thanh toán
        /// </summary>
        [HttpPost("{orderId}/create-url")]
        [Authorize]
        public async Task<IActionResult> CreatePaymentUrl(string orderId, [FromQuery] string returnUrl = "")
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = $"{Request.Scheme}://{Request.Host}/payment-result";

            var result = await _paymentService.CreatePaymentUrlAsync(orderId, returnUrl);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// POST /api/payment/callback
        /// Endpoint nhận callback từ cổng thanh toán (không cần auth — cổng thanh toán gọi vào)
        /// Dùng HMAC signature để xác thực thay vì JWT
        /// </summary>
        [HttpPost("callback")]
        public async Task<IActionResult> Callback([FromBody] PaymentCallbackDto dto)
        {
            var result = await _paymentService.HandleCallbackAsync(dto);
            // Trả 200 dù success hay fail — cổng thanh toán cần nhận 200 để dừng retry
            return Ok(result);
        }

        /// <summary>
        /// GET /api/payment/{orderId}/status
        /// Kiểm tra trạng thái thanh toán
        /// </summary>
        [HttpGet("{orderId}/status")]
        [Authorize]
        public async Task<IActionResult> GetStatus(string orderId)
        {
            var result = await _paymentService.GetPaymentStatusAsync(orderId);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
