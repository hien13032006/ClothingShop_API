using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notifService;
        public NotificationController(INotificationService ns) => _notifService = ns;
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>GET /api/notification?page=1&pageSize=20</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
            => Ok(await _notifService.GetMyNotificationsAsync(GetUserId(), page, pageSize));

        /// <summary>PUT /api/notification/{id}/read</summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var r = await _notifService.MarkAsReadAsync(GetUserId(), id);
            return r.Success ? Ok(r) : NotFound(r);
        }

        /// <summary>PUT /api/notification/read-all</summary>
        [HttpPut("read-all")]
        public async Task<IActionResult> MarkAllRead()
            => Ok(await _notifService.MarkAllAsReadAsync(GetUserId()));

        /// <summary>DELETE /api/notification/{id}</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _notifService.DeleteNotificationAsync(GetUserId(), id);
            return r.Success ? Ok(r) : NotFound(r);
        }

        /// <summary>POST /api/notification/admin/broadcast — Gửi thông báo đến tất cả khách</summary>
        [HttpPost("admin/broadcast")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest dto)
        {
            await _notifService.CreateBroadcastAsync(dto.Type, dto.Title, dto.Body);
            return Ok(new { success = true, message = "Đã gửi thông báo đến tất cả khách hàng" });
        }
    }

    public record BroadcastRequest(string Type, string Title, string Body);
}
