using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IMembershipService
    {
        Task<ApiResponse<MembershipInfoDto>> GetMembershipInfoAsync(string userId);
        Task<ApiResponse<List<PointHistoryDto>>> GetPointHistoryAsync(string userId, int page, int pageSize);
        Task<ApiResponse<MembershipBenefitsDto>> GetAllBenefitsAsync();
    }

    public class MembershipService : IMembershipService
    {
        private readonly AppDbContext _context;

        public MembershipService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<MembershipInfoDto>> GetMembershipInfoAsync(string userId)
        {
            var customer = await _context.Customers.FindAsync(userId);
            if (customer == null) return ApiResponse<MembershipInfoDto>.Fail("Không tìm thấy tài khoản");

            var nextLevel     = GetNextLevel(customer.MembershipLevel);
            var pointsToNext  = GetPointsToNextLevel(customer.MembershipLevel, customer.TotalPoints);

            return ApiResponse<MembershipInfoDto>.Ok(new MembershipInfoDto
            {
                UserId          = customer.UserId,
                FullName        = customer.FullName,
                MembershipLevel = customer.MembershipLevel,
                TotalPoints     = customer.TotalPoints,
                DiscountRate    = GetDiscountRate(customer.MembershipLevel),
                NextLevel       = nextLevel,
                PointsToNextLevel = pointsToNext,
                Benefits        = GetBenefits(customer.MembershipLevel)
            });
        }

        public async Task<ApiResponse<List<PointHistoryDto>>> GetPointHistoryAsync(
            string userId, int page, int pageSize)
        {
            var history = await _context.PointHistories
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResponse<List<PointHistoryDto>>.Ok(history.Select(h => new PointHistoryDto
            {
                Points    = h.Points,
                Reason    = h.Reason,
                OrderId   = h.OrderId,
                CreatedAt = h.CreatedAt
            }).ToList());
        }

        public async Task<ApiResponse<MembershipBenefitsDto>> GetAllBenefitsAsync()
        {
            return ApiResponse<MembershipBenefitsDto>.Ok(new MembershipBenefitsDto
            {
                Levels = new List<LevelBenefitDto>
                {
                    new() { Level = "Bạc",       MinPoints = 0,    MaxPoints = 1499,
                            DiscountRate = 0,   Description = "Thành viên cơ bản. Tích 1 điểm / 10.000đ" },
                    new() { Level = "Vàng",      MinPoints = 1500, MaxPoints = 4999,
                            DiscountRate = 5,   Description = "Giảm 5% mỗi đơn hàng" },
                    new() { Level = "Kim cương", MinPoints = 5000, MaxPoints = int.MaxValue,
                            DiscountRate = 10,  Description = "Giảm 10% mỗi đơn hàng + ưu tiên hỗ trợ" },
                },
                PointsPerAmount = 1,       // 1 điểm / 10.000đ
                AmountPerPoint  = 10_000
            });
        }

        private static decimal GetDiscountRate(string level) => level switch
        {
            "Vàng"      => 5,
            "Kim cương" => 10,
            _           => 0
        };

        private static string? GetNextLevel(string level) => level switch
        {
            "Bạc"  => "Vàng",
            "Vàng" => "Kim cương",
            _      => null
        };

        private static int GetPointsToNextLevel(string level, int currentPoints) => level switch
        {
            "Bạc"  => Math.Max(0, 1500  - currentPoints),
            "Vàng" => Math.Max(0, 5000  - currentPoints),
            _      => 0
        };

        private static List<string> GetBenefits(string level) => level switch
        {
            "Vàng" => new() { "Giảm 5% mỗi đơn hàng", "Ưu tiên hỗ trợ khách hàng" },
            "Kim cương" => new() { "Giảm 10% mỗi đơn hàng", "Giao hàng ưu tiên", "Hỗ trợ 24/7" },
            _ => new() { "Tích điểm mỗi đơn hàng" }
        };
    }
}
