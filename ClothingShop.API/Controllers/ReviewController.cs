using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        /// <summary>
        /// Lấy danh sách đánh giá của sản phẩm (Có phân trang)
        /// GET: api/review/{productId}?page=1&pageSize=10
        /// </summary>
        [HttpGet("{productId}")]
        public async Task<IActionResult> GetReviews(int productId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _reviewService.GetReviewsAsync(productId, page, pageSize);
            return Ok(result);
        }

        /// <summary>
        /// Đăng đánh giá cho sản phẩm trong một đơn hàng
        /// POST: api/review/{productId}
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddReview([FromBody] CreateReviewDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Người dùng chưa đăng nhập");

            // Lấy productId từ chính dto luôn, không cần tham số riêng
            var result = await _reviewService.AddReviewAsync(userId, dto.ProductId, dto);

            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Xóa đánh giá (Người dùng tự xóa hoặc Admin xóa)
        /// DELETE: api/review/{reviewId}
        /// </summary>
        [HttpDelete("{reviewId}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Kiểm tra quyền Admin (giả sử role được lưu trong Claims)
            bool isAdmin = User.IsInRole("Admin");

            var result = await _reviewService.DeleteReviewAsync(userId ?? "", reviewId, isAdmin);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}