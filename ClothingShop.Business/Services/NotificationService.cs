using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface INotificationService
    {
        Task<ApiResponse<NotificationSummaryDto>> GetMyNotificationsAsync(string userId, int page, int pageSize);
        Task<ApiResponse<string>> MarkAsReadAsync(string userId, int notificationId);
        Task<ApiResponse<string>> MarkAllAsReadAsync(string userId);
        Task<ApiResponse<string>> DeleteNotificationAsync(string userId, int notificationId);
        // Internal — called by OrderService, etc.
        Task CreateAsync(string userId, string type, string title, string body, string? relatedId = null);
        Task CreateBroadcastAsync(string type, string title, string body);  // Gửi tất cả khách hàng
    }

    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        public NotificationService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<NotificationSummaryDto>> GetMyNotificationsAsync(
            string userId, int page, int pageSize)
        {
            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var unread = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return ApiResponse<NotificationSummaryDto>.Ok(new NotificationSummaryDto
            {
                Items = items.Select(n => new NotificationDto
                {
                    NotificationId = n.NotificationId,
                    Type      = n.Type,
                    Title     = n.Title,
                    Body      = n.Body,
                    RelatedId = n.RelatedId,
                    IsRead    = n.IsRead,
                    CreatedAt = n.CreatedAt
                }).ToList(),
                UnreadCount = unread
            });
        }

        public async Task<ApiResponse<string>> MarkAsReadAsync(string userId, int notificationId)
        {
            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (notif == null) return ApiResponse<string>.Fail("Không tìm thấy thông báo");

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã đánh dấu đã đọc");
        }

        public async Task<ApiResponse<string>> MarkAllAsReadAsync(string userId)
        {
            var notifs = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            foreach (var n in notifs) n.IsRead = true;
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", $"Đã đọc {notifs.Count} thông báo");
        }

        public async Task<ApiResponse<string>> DeleteNotificationAsync(string userId, int notificationId)
        {
            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
            if (notif == null) return ApiResponse<string>.Fail("Không tìm thấy thông báo");

            _context.Notifications.Remove(notif);
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã xóa thông báo");
        }

        public async Task CreateAsync(string userId, string type, string title, string body, string? relatedId = null)
        {
            await _context.Notifications.AddAsync(new Notification
            {
                UserId    = userId,
                Type      = type,
                Title     = title,
                Body      = body,
                RelatedId = relatedId,
                IsRead    = false,
                CreatedAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
        }

        public async Task CreateBroadcastAsync(string type, string title, string body)
        {
            var userIds = await _context.Customers
                .Where(c => !c.UserId.StartsWith("AD"))
                .Select(c => c.UserId)
                .ToListAsync();

            var notifications = userIds.Select(uid => new Notification
            {
                UserId    = uid,
                Type      = type,
                Title     = title,
                Body      = body,
                IsRead    = false,
                CreatedAt = DateTime.Now
            }).ToList();

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }
    }
}
