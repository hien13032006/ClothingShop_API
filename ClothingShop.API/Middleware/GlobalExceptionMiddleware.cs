using System.Net;
using System.Text.Json;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Middleware
{
    /// <summary>
    /// Middleware bắt toàn bộ exception chưa được xử lý trong app.
    /// Thay vì để .NET trả về trang lỗi mặc định, ta trả về JSON chuẩn.
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = ex switch
            {
                ArgumentNullException    => (HttpStatusCode.BadRequest,      "Dữ liệu đầu vào không hợp lệ"),
                ArgumentException        => (HttpStatusCode.BadRequest,      ex.Message),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Bạn không có quyền thực hiện thao tác này"),
                KeyNotFoundException     => (HttpStatusCode.NotFound,        "Không tìm thấy dữ liệu yêu cầu"),
                InvalidOperationException => (HttpStatusCode.BadRequest,     ex.Message),
                _                        => (HttpStatusCode.InternalServerError, "Lỗi hệ thống, vui lòng thử lại sau")
            };

            context.Response.StatusCode = (int)statusCode;

            var response = ApiResponse<object>.Fail(message);
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }

    // Extension method để đăng ký middleware gọn hơn
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
            => app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
