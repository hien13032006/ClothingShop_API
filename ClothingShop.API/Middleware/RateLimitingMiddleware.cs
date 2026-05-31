using System.Collections.Concurrent;

namespace ClothingShop.API.Middleware
{
    /// <summary>
    /// Rate Limiting đơn giản dựa trên IP — không cần thư viện ngoài.
    /// Giới hạn mỗi IP không quá N request / phút.
    /// Các endpoint nhạy cảm (login, register, forgot-password) có giới hạn thấp hơn.
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate                                              _next;
        private readonly ILogger<RateLimitingMiddleware>                             _logger;
        private static readonly ConcurrentDictionary<string, RateLimitEntry>        _cache = new();

        // (path-prefix → max requests per minute)
        private static readonly Dictionary<string, int> _strictEndpoints = new()
        {
            { "/api/auth/login",           10 },
            { "/api/auth/register",        10 },
            { "/api/auth/forgot-password", 5  },
            { "/api/auth/verify-otp",      5  }
        };

        private const int DefaultLimit = 200; // request/phút cho các endpoint còn lại

        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next   = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var ip   = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var path = context.Request.Path.Value?.ToLower() ?? "";

            var limit = _strictEndpoints
                .Where(e => path.StartsWith(e.Key))
                .Select(e => (int?)e.Value)
                .FirstOrDefault() ?? DefaultLimit;

            var key   = $"{ip}:{path}";
            var entry = _cache.GetOrAdd(key, _ => new RateLimitEntry());

            lock (entry)
            {
                // Reset counter sau 1 phút
                if (DateTime.Now - entry.WindowStart > TimeSpan.FromMinutes(1))
                {
                    entry.Count       = 0;
                    entry.WindowStart = DateTime.Now;
                }

                entry.Count++;

                if (entry.Count > limit)
                {
                    _logger.LogWarning("Rate limit exceeded — IP={Ip} Path={Path} Count={Count}", ip, path, entry.Count);
                    context.Response.StatusCode  = 429;
                    context.Response.ContentType = "application/json";
                    context.Response.Headers["Retry-After"] = "60";
                    context.Response.WriteAsync(
                        "{\"success\":false,\"message\":\"Quá nhiều yêu cầu. Vui lòng thử lại sau 60 giây.\"}");
                    return;
                }
            }

            // Thêm headers cho client biết giới hạn còn lại
            context.Response.Headers["X-RateLimit-Limit"]     = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = Math.Max(0, limit - _cache[key].Count).ToString();

            await _next(context);
        }

        private class RateLimitEntry
        {
            public int Count { get; set; } = 0;
            public DateTime WindowStart { get; set; } = DateTime.Now;
        }
    }

    public static class RateLimitingExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
            => app.UseMiddleware<RateLimitingMiddleware>();
    }
}
