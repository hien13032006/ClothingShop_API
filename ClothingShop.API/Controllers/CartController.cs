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
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>GET /api/cart</summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var result = await _cartService.GetCartAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/cart/add</summary>
        [HttpPost("add")] 
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            var result = await _cartService.AddToCartAsync(GetUserId(), dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>POST /api/cart/add-bulk</summary>
        [HttpPost("add-bulk")]
        public async Task<IActionResult> AddBulkToCart([FromBody] List<AddToCartDto> items)
        {
            var userId = GetUserId();
            foreach (var item in items)
            {
                // Gọi lại logic AddToCartAsync đã có sẵn trong Service
                await _cartService.AddToCartAsync(userId, item);
            }
            return Ok(new { success = true, message = "Đã thêm toàn bộ sản phẩm vào giỏ hàng" });
        }

        /// <summary>PUT /api/cart/update</summary>
        [HttpPut("update")] 
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartDto dto)
        {
            var result = await _cartService.UpdateQuantityAsync(GetUserId(), dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>DELETE /api/cart/{cartId}</summary>
        [HttpDelete("{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var result = await _cartService.RemoveItemAsync(GetUserId(), cartItemId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>DELETE /api/cart/clear</summary>
        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            var result = await _cartService.ClearCartAsync(GetUserId());
            return Ok(result);
        }


    }
}
