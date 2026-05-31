using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IShippingService
    {
        Task<ApiResponse<List<ShippingOptionDto>>> GetShippingOptionsAsync(decimal orderAmount);
        Task<ApiResponse<ShippingOptionDto>> GetFeeAsync(string method, decimal orderAmount);
        Task<ApiResponse<List<ShippingOptionDto>>> GetAllFeesAsync();          // Admin
        Task<ApiResponse<ShippingOptionDto>> UpsertShippingFeeAsync(UpsertShippingFeeDto dto); // Admin
    }

    public class ShippingService : IShippingService
    {
        private readonly AppDbContext _context;

        public ShippingService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<List<ShippingOptionDto>>> GetShippingOptionsAsync(decimal orderAmount)
        {
            var fees = await _context.ShippingFees.ToListAsync();
            return ApiResponse<List<ShippingOptionDto>>.Ok(
                fees.Select(f => ToDto(f, orderAmount)).ToList());
        }

        public async Task<ApiResponse<ShippingOptionDto>> GetFeeAsync(string method, decimal orderAmount)
        {
            var fee = await _context.ShippingFees
                .FirstOrDefaultAsync(f => f.Method == method);

            if (fee == null)
                return ApiResponse<ShippingOptionDto>.Fail($"Không tìm thấy phương thức '{method}'");

            return ApiResponse<ShippingOptionDto>.Ok(ToDto(fee, orderAmount));
        }

        public async Task<ApiResponse<List<ShippingOptionDto>>> GetAllFeesAsync()
        {
            var fees = await _context.ShippingFees.ToListAsync();
            return ApiResponse<List<ShippingOptionDto>>.Ok(
                fees.Select(f => ToDto(f, 0)).ToList());
        }

        public async Task<ApiResponse<ShippingOptionDto>> UpsertShippingFeeAsync(UpsertShippingFeeDto dto)
        {
            var existing = await _context.ShippingFees
                .FirstOrDefaultAsync(f => f.Method == dto.Method);

            if (existing != null)
            {
                existing.Fee              = dto.Fee;
                existing.FreeShippingFrom = dto.FreeShippingFrom;
                existing.EstimatedDays    = dto.EstimatedDays;
            }
            else
            {
                existing = new ShippingFee
                {
                    Method            = dto.Method,
                    Fee               = dto.Fee,
                    FreeShippingFrom  = dto.FreeShippingFrom,
                    EstimatedDays     = dto.EstimatedDays
                };
                await _context.ShippingFees.AddAsync(existing);
            }

            await _context.SaveChangesAsync();
            return ApiResponse<ShippingOptionDto>.Ok(ToDto(existing, 0), "Cập nhật phí vận chuyển thành công");
        }

        private static ShippingOptionDto ToDto(ShippingFee f, decimal orderAmount) => new()
        {
            Method           = f.Method,
            Fee              = orderAmount >= f.FreeShippingFrom && f.FreeShippingFrom > 0 ? 0 : f.Fee,
            OriginalFee      = f.Fee,
            FreeShippingFrom = f.FreeShippingFrom,
            EstimatedDays    = f.EstimatedDays,
            IsFree           = f.FreeShippingFrom > 0 && orderAmount >= f.FreeShippingFrom
        };
    }
}
