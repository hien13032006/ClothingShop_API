using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;
using ClothingShop.Models.DTOs;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromotionController : ControllerBase
    {
        private readonly IPromotionService _promoService;

        public PromotionController(IPromotionService promoService)
        {
            _promoService = promoService;
        }

        /// <summary>POST /api/promotion/apply — Kiểm tra và tính giảm giá</summary>
        [HttpPost("apply")]
        [Authorize]
        public async Task<IActionResult> ApplyPromo([FromBody] ApplyPromoDto dto)
        {
            var result = await _promoService.ApplyPromoCodeAsync(dto);
            return Ok(result);
        }

        /// <summary>GET /api/promotion/admin — Xem tất cả khuyến mãi</summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _promoService.GetAllPromotionsAsync();
            return Ok(result);
        }

        /// <summary>POST /api/promotion/admin — Tạo mã khuyến mãi</summary>
        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreatePromotionDto dto)
        {
            var result = await _promoService.CreatePromotionAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>PUT /api/promotion/admin/{id}/toggle — Bật/tắt khuyến mãi</summary>
        [HttpPut("admin/{id}/toggle")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Toggle(int id)
        {
            var result = await _promoService.TogglePromotionAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
