using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IPromotionService
    {
        Task<ApiResponse<PromoResultDto>> ApplyPromoCodeAsync(ApplyPromoDto dto);
        Task<ApiResponse<Promotion>> CreatePromotionAsync(CreatePromotionDto dto);
        Task<ApiResponse<List<Promotion>>> GetAllPromotionsAsync();
        Task<ApiResponse<string>> TogglePromotionAsync(int promotionId);
    }

    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promoRepo;

        public PromotionService(IPromotionRepository promoRepo)
        {
            _promoRepo = promoRepo;
        }

        public async Task<ApiResponse<PromoResultDto>> ApplyPromoCodeAsync(ApplyPromoDto dto)
        {
            var promo = await _promoRepo.GetByCodeAsync(dto.Code);
            if (promo == null)
                return ApiResponse<PromoResultDto>.Ok(new PromoResultDto
                {
                    IsValid = false,
                    Message = "Mã khuyến mãi không tồn tại hoặc đã hết hạn",
                    DiscountAmount = 0,
                    FinalAmount = dto.OrderAmount
                });

            if (!promo.IsValid(dto.OrderAmount))
            {
                // Nếu không hợp lệ do chưa đủ tiền tối thiểu
                if (dto.OrderAmount < promo.MinOrderAmount)
                {
                    return ApiResponse<PromoResultDto>.Ok(new PromoResultDto
                    {
                        IsValid = false,
                        Message = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này",
                        DiscountAmount = 0,
                        FinalAmount = dto.OrderAmount
                    });
                }

                // Các lý do không hợp lệ khác (hết hạn, bị khóa)
                return ApiResponse<PromoResultDto>.Ok(new PromoResultDto
                {
                    IsValid = false,
                    Message = "Mã khuyến mãi đã hết hạn hoặc không còn hiệu lực",
                    DiscountAmount = 0,
                    FinalAmount = dto.OrderAmount
                });
            }

            if (dto.OrderAmount < promo.MinOrderAmount)
                return ApiResponse<PromoResultDto>.Ok(new PromoResultDto
                {
                    IsValid = false,
                    Message = $"Đơn hàng tối thiểu {promo.MinOrderAmount:N0}đ để áp dụng mã này",
                    DiscountAmount = 0,
                    FinalAmount = dto.OrderAmount
                });

            var discount = promo.CalcDiscount(dto.OrderAmount);

            return ApiResponse<PromoResultDto>.Ok(new PromoResultDto
            {
                IsValid = true,
                Message = $"Áp dụng thành công! Giảm {discount:N0}đ",
                DiscountAmount = discount,
                FinalAmount = dto.OrderAmount - discount
            });
        }

        public async Task<ApiResponse<Promotion>> CreatePromotionAsync(CreatePromotionDto dto)
        {
            var existing = await _promoRepo.GetByCodeAsync(dto.Code);
            if (existing != null)
                return ApiResponse<Promotion>.Fail("Mã khuyến mãi đã tồn tại");

            var promo = new Promotion
            {
                Code = dto.Code.ToUpper(),
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                MinOrderAmount = dto.MinOrderAmount,
                MaxDiscountAmount = dto.MaxDiscountAmount,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                IsActive = true
            };

            await _promoRepo.AddAsync(promo);
            await _promoRepo.SaveChangesAsync();
            return ApiResponse<Promotion>.Ok(promo, "Tạo mã khuyến mãi thành công");
        }

        public async Task<ApiResponse<List<Promotion>>> GetAllPromotionsAsync()
        {
            var promos = await _promoRepo.GetAllAsync();
            return ApiResponse<List<Promotion>>.Ok(promos.ToList());
        }

        public async Task<ApiResponse<string>> TogglePromotionAsync(int promotionId)
        {
            var promo = await _promoRepo.GetByIdAsync(promotionId);
            if (promo == null) return ApiResponse<string>.Fail("Không tìm thấy khuyến mãi");

            promo.IsActive = !promo.IsActive;
            await _promoRepo.UpdateAsync(promo);
            await _promoRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", promo.IsActive ? "Đã kích hoạt" : "Đã tắt khuyến mãi");
        }
    }
}
