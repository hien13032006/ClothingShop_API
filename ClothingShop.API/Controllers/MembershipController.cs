using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClothingShop.Business.Services;

namespace ClothingShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MembershipController : ControllerBase
    {
        private readonly IMembershipService _membershipService;
        public MembershipController(IMembershipService ms) => _membershipService = ms;
        private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        /// <summary>GET /api/membership/benefits — Xem bảng quyền lợi hạng thành viên (public)</summary>
        [HttpGet("benefits")]
        public async Task<IActionResult> GetBenefits()
            => Ok(await _membershipService.GetAllBenefitsAsync());

        /// <summary>GET /api/membership/info — Hạng, điểm, quyền lợi của tôi</summary>
        [HttpGet("info")]
        [Authorize]
        public async Task<IActionResult> GetMyInfo()
        {
            var r = await _membershipService.GetMembershipInfoAsync(GetUserId());
            return r.Success ? Ok(r) : NotFound(r);
        }

        /// <summary>GET /api/membership/points?page=1&pageSize=10 — Lịch sử điểm của tôi</summary>
        [HttpGet("points")]
        [Authorize]
        public async Task<IActionResult> GetPointHistory(
            [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
            => Ok(await _membershipService.GetPointHistoryAsync(GetUserId(), page, pageSize));
    }
}
