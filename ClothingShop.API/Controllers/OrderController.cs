using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrderController(IOrderService orderService) => _orderService = orderService;

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
        public async Task<IActionResult> GetMyOrders()
            => Ok(await _orderService.GetMyOrdersAsync(GetUserId()));

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
