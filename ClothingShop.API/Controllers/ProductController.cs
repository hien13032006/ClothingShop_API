using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly IReviewService _reviewService;
        private readonly IVariantService _variantService;

        public ProductController(
            IProductService productService,
            IReviewService reviewService,
            IVariantService variantService)
        {
            _productService = productService;
            _reviewService = reviewService;
            _variantService = variantService;
        }

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ── Public ────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/product
        /// Query: page, pageSize, categoryId, keyword, minPrice, maxPrice, color, size
        /// sortBy: newest | price_asc | price_desc | best_selling | top_rated
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDto filter)
            // ✅ Truyền userId để đánh dấu IsInWishlist đúng
            => Ok(await _productService.GetProductsAsync(filter, GetUserId()));

        /// <summary>GET /api/product/{id}</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var r = await _productService.GetProductDetailAsync(id, GetUserId());
            return r.Success ? Ok(r) : NotFound(r);
        }

        [HttpGet("categories-by-gender")]
        public async Task<IActionResult> GetCategoriesByGender()
        {
            var result = await _productService.GetCategoriesByGenderAsync();
            return Ok(result); // Trả về JSON cho Frontend
        }

        /// <summary>GET /api/product/categories</summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
            => Ok(await _productService.GetCategoriesAsync());


        // ── Thêm mới: Các Endpoint phục vụ riêng cho trang chủ hiển thị ───

        /// <summary>
        /// GET /api/product/best-sellers
        /// Lấy danh sách sản phẩm bán chạy nhất dựa trên số lượt đã mua (SoldCount)
        /// </summary>
        [HttpGet("best-sellers")]
        public async Task<IActionResult> GetBestSellers([FromQuery] int limit = 4)
            => Ok(await _productService.GetHotProductsAsync(limit, GetUserId()));

        [HttpGet("new-arrivals")]
        public async Task<IActionResult> GetNewArrivals([FromQuery] int limit = 5)
            => Ok(await _productService.GetNewArrivalsAsync(limit, GetUserId()));

        [HttpGet("discounts")]
        public async Task<IActionResult> GetDiscounts([FromQuery] int limit = 8)
            => Ok(await _productService.GetDiscountProductsAsync(limit, GetUserId()));

        // ── Gallery ───────────────────────────────────────────────────

       

        // ── Reviews ───────────────────────────────────────────────────

        /// <summary>GET /api/product/{id}/reviews?page=1&pageSize=5</summary>
        [HttpGet("{id:int}/reviews")]
        public async Task<IActionResult> GetReviews(
            int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
            => Ok(await _reviewService.GetReviewsAsync(id, page, pageSize));

        /// <summary>POST /api/product/{id}/reviews</summary>
        [HttpPost("{id:int}/reviews")]
        [Authorize]
        public async Task<IActionResult> AddReview(int id, [FromBody] CreateReviewDto dto)
        {
            var r = await _reviewService.AddReviewAsync(GetUserId()!, id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>DELETE /api/product/{id}/reviews/{reviewId}</summary>
        [HttpDelete("{id:int}/reviews/{reviewId:int}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(int id, int reviewId)
        {
            var r = await _reviewService.DeleteReviewAsync(GetUserId()!, reviewId, User.IsInRole("Admin"));
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ── Admin: Products ───────────────────────────────────────────

        /// <summary>POST /api/product/admin</summary>
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
        {
            var r = await _productService.CreateProductAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>PUT /api/product/admin/{id}</summary>
        [HttpPut("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
        {
            var r = await _productService.UpdateProductAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>DELETE /api/product/admin/{id} — Soft delete</summary>
        [HttpDelete("admin/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var r = await _productService.DeleteProductAsync(id);
            return r.Success ? Ok(r) : NotFound(r);
        }

        

        // ── Admin: Variants ───────────────────────────────────────────

        /// <summary>POST /api/product/admin/{id}/variants</summary>
        [HttpPost("admin/{id:int}/variants")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddVariant(int id, [FromBody] CreateVariantDto dto)
        {
            var r = await _variantService.AddVariantAsync(id, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>PUT /api/product/admin/variants/{variantId}</summary>
        [HttpPut("admin/variants/{variantId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateVariant(int variantId, [FromBody] UpdateVariantDto dto)
        {
            var r = await _variantService.UpdateVariantAsync(variantId, dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>DELETE /api/product/admin/variants/{variantId}</summary>
        [HttpDelete("admin/variants/{variantId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteVariant(int variantId)
        {
            var r = await _variantService.DeleteVariantAsync(variantId);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ── Admin: Inventory ──────────────────────────────────────────

        /// <summary>GET /api/product/admin/inventory?page=1&pageSize=20&keyword=áo</summary>
        [HttpGet("admin/inventory")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetInventory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
            => Ok(await _productService.GetInventoryAsync(page, pageSize, keyword));

        /// <summary>PUT /api/product/admin/variants/{variantId}/stock</summary>
        [HttpPut("admin/variants/{variantId:int}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(int variantId, [FromBody] UpdateStockReq dto)
        {
            var r = await _productService.UpdateVariantStockAsync(variantId, dto.NewStock);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>PUT /api/product/admin/inventory/batch</summary>
        [HttpPut("admin/inventory/batch")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStockBatch([FromBody] UpdateStockBatchDto dto)
        {
            var r = await _productService.UpdateStockBatchAsync(dto);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ── Admin: Categories ─────────────────────────────────────────

        /// <summary>POST /api/product/admin/categories</summary>
        [HttpPost("admin/categories")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryReq dto)
        {
            var r = await _productService.CreateCategoryAsync(dto.Name, dto.Description);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>DELETE /api/product/admin/categories/{id}</summary>
        [HttpDelete("admin/categories/{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var r = await _productService.DeleteCategoryAsync(id);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        // ── Admin: Reviews ────────────────────────────────────────────

        /// <summary>DELETE /api/product/admin/reviews/{reviewId} — Xóa review vi phạm</summary>
        [HttpDelete("admin/reviews/{reviewId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDeleteReview(int reviewId)
        {
            var r = await _reviewService.DeleteReviewAsync("admin", reviewId, true);
            return r.Success ? Ok(r) : NotFound(r);
        }
    }

    public record CreateCategoryReq(string Name, string? Description);
    public record UpdateStockReq(int NewStock);
}