using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ICartService _cartService;
        public OrderController(IOrderService orderService, ICartService cartService)
        {
            _orderService = orderService;
            _cartService = cartService;
        }
        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // ── Customer ──────────────────────────────────────────────────

        /// <summary>POST /api/order — Đặt hàng</summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            var r = await _orderService.CreateOrderAsync(GetUserId(), dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>GET /api/order/my-orders</summary>
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders([FromQuery] string status)
        {
            var userId = GetUserId();
            Console.WriteLine("User đang đăng nhập là: " + userId); // Kiểm tra xem ID này có là KH0001 không
            return Ok(await _orderService.GetMyOrdersAsync(userId, status));
        }

        /// <summary>GET /api/order/{orderId}</summary>
        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrder(string orderId)
        {
            var r = await _orderService.GetOrderDetailAsync(orderId, GetUserId());
            return r.Success ? Ok(r) : NotFound(r);
        }

        /// <summary>PUT /api/order/{orderId}/cancel</summary>
        [HttpPut("{orderId}/cancel")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var r = await _orderService.CancelOrderAsync(orderId, GetUserId());
            return r.Success ? Ok(r) : BadRequest(r);
        }
        [HttpPut("{orderId}/confirm-received")]
        public async Task<IActionResult> ConfirmReceived(string orderId)
        {
            // Bạn cần tạo hàm ConfirmReceivedAsync trong OrderService
            var r = await _orderService.ConfirmReceivedAsync(orderId, GetUserId());
            return r.Success ? Ok(r) : BadRequest(r);
        }

        [HttpPost("add-bulk")]
        public async Task<IActionResult> AddBulkToCart([FromBody] List<CartItemDto> items)
        {
            var userId = GetUserId(); // Hàm lấy ID người dùng hiện tại
            foreach (var item in items)
            {
                // Gọi service xử lý logic thêm từng item vào giỏ
                await _cartService.AddItemToCartAsync(userId, item);
            }
            return Ok(new { success = true });
        }

        [HttpPost("{orderId}/reorder")]
        public async Task<IActionResult> Reorder(string orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // 1. Lấy thông tin đơn hàng cũ từ OrderService
            var orderResult = await _orderService.GetOrderDetailAsync(orderId, userId);

            if (!orderResult.Success) return BadRequest("Không tìm thấy đơn hàng để mua lại");

            // 2. Lặp qua từng chi tiết và thêm vào giỏ hàng
            foreach (var detail in orderResult.Data.Details)
            {
                await _cartService.AddItemToCartAsync(userId, new CartItemDto
                {
                    VariantId = detail.VariantId,
                    Quantity = detail.Quantity
                });
            }

            return Ok(new { success = true, message = "Đã thêm toàn bộ sản phẩm vào giỏ hàng" });
        }

        // 2. Endpoint cho Đánh giá (POST review)
        [HttpPost("{orderId}/review")]
        public async Task<IActionResult> AddReview(string orderId, [FromBody] ReviewDto dto)
        {
            // Bạn cần tạo hàm AddReviewAsync trong OrderService
            var r = await _orderService.AddReviewAsync(orderId, GetUserId(), dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }
        [HttpPost("calculate")] 
        public async Task<IActionResult> Calculate([FromBody] List<CartItemDto> items)
        {
            var result = await _orderService.CalculateOrderAsync(items);
            return Ok(result); 
        }

        // ── Admin ─────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/order/admin/all
        /// ?page=1 &amp;pageSize=10 &amp;status=Chờ xác nhận
        /// &amp;keyword=DH0001 &amp;fromDate=2024-01-01 &amp;toDate=2024-12-31
        /// </summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterDto filter)
            => Ok(await _orderService.GetAllOrdersAsync(filter));

        /// <summary>PUT /api/order/admin/{orderId}/status</summary>
        [HttpPut("admin/{orderId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(string orderId, [FromBody] UpdateOrderStatusDto dto)
        {
            var r = await _orderService.UpdateOrderStatusAsync(orderId, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }
    }
}
