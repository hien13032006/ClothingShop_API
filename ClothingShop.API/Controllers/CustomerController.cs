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
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // Helper: lấy userId từ JWT token
        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        // ===== Profile =====

        /// <summary>GET /api/customer/profile</summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var result = await _customerService.GetProfileAsync(GetUserId());
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>PUT /api/customer/profile</summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var result = await _customerService.UpdateProfileAsync(GetUserId(), dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>PUT /api/customer/change-password</summary>
        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var result = await _customerService.ChangePasswordAsync(GetUserId(), dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== Addresses =====

        /// <summary>GET /api/customer/addresses</summary>
        [HttpGet("addresses")]
        public async Task<IActionResult> GetAddresses()
        {
            var result = await _customerService.GetAddressesAsync(GetUserId());
            return Ok(result);
        }

        /// <summary>POST /api/customer/addresses</summary>
        [HttpPost("addresses")]
        public async Task<IActionResult> AddAddress([FromBody] CreateAddressDto dto)
        {
            var result = await _customerService.AddAddressAsync(GetUserId(), dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>DELETE /api/customer/addresses/{addressId}</summary>
        [HttpDelete("addresses/{addressId}")]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var result = await _customerService.DeleteAddressAsync(GetUserId(), addressId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>PUT /api/customer/addresses/{addressId}/set-default</summary>
        [HttpPut("addresses/{addressId}/set-default")]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var result = await _customerService.SetDefaultAddressAsync(GetUserId(), addressId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ===== Admin only =====

        /// <summary>GET /api/customer/admin/all?page=1&pageSize=10</summary>
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _customerService.GetAllCustomersAsync(page, pageSize);
            return Ok(result);
        }

        /// <summary>DELETE /api/customer/admin/{userId}</summary>
        [HttpDelete("admin/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCustomer(string userId)
        {
            var result = await _customerService.DeleteCustomerAsync(userId);
            return result.Success ? Ok(result) : NotFound(result);
        }
    }
}
