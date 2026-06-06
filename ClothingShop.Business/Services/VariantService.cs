using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IVariantService
    {
        Task<ApiResponse<VariantDto>> AddVariantAsync(int productId, CreateVariantDto dto);
        Task<ApiResponse<VariantDto>> UpdateVariantAsync(int variantId, UpdateVariantDto dto);
        Task<ApiResponse<string>> DeleteVariantAsync(int variantId);
    }

    public class VariantService : IVariantService
    {
        private readonly AppDbContext _context;
        public VariantService(AppDbContext context) => _context = context;

        public async Task<ApiResponse<VariantDto>> AddVariantAsync(int productId, CreateVariantDto dto)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return ApiResponse<VariantDto>.Fail("Không tìm thấy sản phẩm");

            // Kiểm tra trùng màu + size
            var exists = await _context.ProductVariants.AnyAsync(v =>
                v.ProductId == productId &&
                v.Color == dto.Color &&
                v.Size == dto.Size);
            if (exists)
                return ApiResponse<VariantDto>.Fail($"Biến thể {dto.Color}/{dto.Size} đã tồn tại");

            // Tạo biến thể mới (ĐÃ XÓA PriceAdjustment vì Model thực tế không có trường này)
            var variant = new ProductVariant
            {
                ProductId = productId,
                Color = dto.Color?.Trim() ?? string.Empty,
                Size = dto.Size?.Trim() ?? string.Empty,
                StockQuantity = dto.StockQuantity,
                Product = product
            };

            await _context.ProductVariants.AddAsync(variant);
            await _context.SaveChangesAsync();

            return ApiResponse<VariantDto>.Ok(new VariantDto
            {
                VariantId = variant.VariantId,
                Color = variant.Color,
                Size = variant.Size,
                StockQuantity = variant.StockQuantity,
            }, "Thêm biến thể thành công");
        }

        public async Task<ApiResponse<VariantDto>> UpdateVariantAsync(int variantId, UpdateVariantDto dto)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.VariantId == variantId);
            if (variant == null) return ApiResponse<VariantDto>.Fail("Không tìm thấy biến thể");

            if (dto.Color != null) variant.Color = dto.Color.Trim();
            if (dto.Size != null) variant.Size = dto.Size.Trim();
            if (dto.StockQuantity.HasValue) variant.StockQuantity = dto.StockQuantity.Value;

            // ĐÃ XÓA dòng cập nhật variant.PriceAdjustment

            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();

            return ApiResponse<VariantDto>.Ok(new VariantDto
            {
                VariantId = variant.VariantId,
                Color = variant.Color,
                Size = variant.Size,
                StockQuantity = variant.StockQuantity,
            }, "Cập nhật biến thể thành công");
        }

        public async Task<ApiResponse<string>> DeleteVariantAsync(int variantId)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return ApiResponse<string>.Fail("Không tìm thấy biến thể");

            // Kiểm tra có đang trong đơn hàng chưa hoàn thành không
            var inActiveOrder = await _context.OrderDetails.AnyAsync(od =>
                od.VariantId == variantId &&
                od.Order!.Status != "Hoàn thành" &&
                od.Order.Status != "Hủy");
            if (inActiveOrder)
                return ApiResponse<string>.Fail("Không thể xóa biến thể đang có trong đơn hàng đang xử lý");

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã xóa biến thể");
        }
    }
}