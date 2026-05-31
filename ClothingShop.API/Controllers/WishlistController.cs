// ═══════════════════════════════════════════════════════════════
// WishlistController
// ═══════════════════════════════════════════════════════════════
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
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        public WishlistController(IWishlistService ws) => _wishlistService = ws;
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>GET /api/wishlist</summary>
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
            => Ok(await _wishlistService.GetWishlistAsync(GetUserId()));

        /// <summary>POST /api/wishlist/{productId} — Toggle (thêm nếu chưa có, xóa nếu đã có)</summary>
        [HttpPost("{productId}")]
        public async Task<IActionResult> Toggle(int productId)
        {
            var r = await _wishlistService.ToggleWishlistAsync(GetUserId(), productId);
            return r.Success ? Ok(r) : BadRequest(r);
        }

        /// <summary>GET /api/wishlist/check/{productId}</summary>
        [HttpGet("check/{productId}")]
        public async Task<IActionResult> Check(int productId)
            => Ok(await _wishlistService.IsInWishlistAsync(GetUserId(), productId));
    }
}
