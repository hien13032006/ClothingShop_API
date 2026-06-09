using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface ICartService
    {
        Task<ApiResponse<CartSummaryDto>> GetCartAsync(string userId);
        Task<ApiResponse<CartSummaryDto>> AddToCartAsync(string userId, AddToCartDto dto);
        Task<ApiResponse<CartSummaryDto>> UpdateQuantityAsync(string userId, UpdateCartDto dto);
        Task<ApiResponse<string>> RemoveItemAsync(string userId, int cartId);
        Task<ApiResponse<string>> ClearCartAsync(string userId);
        Task<ApiResponse<CartSummaryDto>> AddItemToCartAsync(string userId, CartItemDto item);
    }

    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IProductRepository _productRepo;

        public CartService(ICartRepository cartRepo, IProductRepository productRepo)
        {
            _cartRepo = cartRepo;
            _productRepo = productRepo;
        }

        public async Task<ApiResponse<CartSummaryDto>> GetCartAsync(string userId)
        {
            var items = await _cartRepo.GetCartByUserAsync(userId);
            return ApiResponse<CartSummaryDto>.Ok(BuildSummary(items));
        }

        public async Task<ApiResponse<CartSummaryDto>> AddToCartAsync(string userId, AddToCartDto dto)
        {
            if (dto.Quantity <= 0) return ApiResponse<CartSummaryDto>.Fail("Số lượng thêm phải lớn hơn 0");

            var variant = await _productRepo.GetVariantAsync(dto.VariantId);
            if (variant == null) return ApiResponse<CartSummaryDto>.Fail("Sản phẩm không tồn tại");
            if (variant.StockQuantity < dto.Quantity)
                return ApiResponse<CartSummaryDto>.Fail($"Chỉ còn {variant.StockQuantity} sản phẩm trong kho");

            var existing = await _cartRepo.GetCartItemAsync(userId, dto.VariantId);
            if (existing != null)
            {
                var newQty = existing.Quantity + dto.Quantity;
                if (newQty > variant.StockQuantity)
                    return ApiResponse<CartSummaryDto>.Fail(
                        $"Giỏ hàng của bạn đã có {existing.Quantity} sản phẩm này. Không thể thêm tiếp vì vượt quá số lượng tồn kho là {variant.StockQuantity}!");

                existing.Quantity = newQty;
                await _cartRepo.UpdateAsync(existing);
            }
            else
            {
                await _cartRepo.AddAsync(new CartItem
                {
                    UserId = userId,
                    VariantId = dto.VariantId,
                    Quantity = dto.Quantity,
                    AddedAt = DateTime.UtcNow // 🌟 SỬA LỖI 1: Đồng bộ UtcNow với cấu trúc thực tế của Model
                });
            }

            await _cartRepo.SaveChangesAsync();
            var cart = await _cartRepo.GetCartByUserAsync(userId);
            return ApiResponse<CartSummaryDto>.Ok(BuildSummary(cart), "Đã thêm vào giỏ hàng");
        }

        public async Task<ApiResponse<CartSummaryDto>> UpdateQuantityAsync(string userId, UpdateCartDto dto)
        {
            // 🌟 XỬ LÝ KHI SỐ LƯỢNG GIẢM VỀ <= 0: Tự động chuyển đổi sang nghiệp vụ Xóa sản phẩm
            if (dto.Quantity <= 0)
            {
                // Sử dụng thuộc tính ID của dòng sản phẩm (Hãy đảm bảo dto.CartId chính là ID của CartItem)
                var deleteResult = await RemoveItemAsync(userId, dto.CartId);
                if (!deleteResult.Success)
                    return ApiResponse<CartSummaryDto>.Fail(deleteResult.Message);

                // Lấy lại giỏ hàng sau khi đã xóa mục kia
                var updatedCart = await _cartRepo.GetCartByUserAsync(userId);

                // PHÒNG VỆ: Nếu đây là sản phẩm cuối cùng và giỏ hàng trống (updatedCart bị null)
                if (updatedCart == null)
                {
                    return ApiResponse<CartSummaryDto>.Ok(new CartSummaryDto(), "Đã gỡ sản phẩm cuối cùng. Giỏ hàng hiện đang trống.");
                }

                return ApiResponse<CartSummaryDto>.Ok(BuildSummary(updatedCart), "Đã gỡ sản phẩm khỏi giỏ hàng");
            }

            // Lấy thông tin dòng sản phẩm trong giỏ hàng
            var item = await _cartRepo.GetByIdAsync(dto.CartId);

            // Nếu bảng CartItem của bạn không có trường UserId trực tiếp, hãy thay bằng item.Cart.UserId != userId
            if (item == null || item.UserId != userId)
                return ApiResponse<CartSummaryDto>.Fail("Không tìm thấy sản phẩm hợp lệ trong giỏ hàng của bạn");

            // Lấy thông tin biến thể sản phẩm để kiểm tra tồn kho thời gian thực
            var variant = await _productRepo.GetVariantAsync(item.VariantId);
            if (variant == null)
                return ApiResponse<CartSummaryDto>.Fail("Biến thể sản phẩm không còn tồn tại trên hệ thống");

            // Ngăn chặn nếu số lượng yêu cầu lớn hơn số lượng có sẵn trong kho
            if (variant.StockQuantity < dto.Quantity)
                return ApiResponse<CartSummaryDto>.Fail($"Số lượng bạn chọn vượt quá số lượng hàng có sẵn trong kho (Hiện còn: {variant.StockQuantity})");

            // Cập nhật số lượng mới và lưu xuống database
            item.Quantity = dto.Quantity;
            await _cartRepo.UpdateAsync(item);
            await _cartRepo.SaveChangesAsync();

            // Lấy lại cấu trúc giỏ hàng mới nhất để BuildSummary trả về cho Frontend hiển thị tổng tiền mới
            var cart = await _cartRepo.GetCartByUserAsync(userId);
            return ApiResponse<CartSummaryDto>.Ok(BuildSummary(cart), "Cập nhật số lượng giỏ hàng thành công");
        }

        public async Task<ApiResponse<string>> RemoveItemAsync(string userId, int cartId)
        {
            var item = await _cartRepo.GetByIdAsync(cartId);

            if (item == null || item.UserId != userId)
                return ApiResponse<string>.Fail("Không tìm thấy sản phẩm trong giỏ");

            await _cartRepo.DeleteAsync(item);
            await _cartRepo.SaveChangesAsync();

            return ApiResponse<string>.Ok("OK", "Đã xóa sản phẩm khỏi giỏ hàng thành công");
        }

        public async Task<ApiResponse<string>> ClearCartAsync(string userId)
        {
            await _cartRepo.ClearCartAsync(userId);
            return ApiResponse<string>.Ok("OK", "Đã xóa toàn bộ giỏ hàng");
        }

        private static CartSummaryDto BuildSummary(IEnumerable<CartItem> items)
        {
            var list = items.Select(ci => {
                decimal originalPrice = ci.Variant?.Product?.Price ?? 0;
                // Giả định bảng Product của bạn có cột Discount (kiểu int hoặc decimal ví dụ: 10 nghĩa là giảm 10%)
                decimal discountPercent = ci.Variant?.Product?.Discount ?? 0;

                // 🌟 SỬA LỖI 2: Tính giá thực tế sau khi áp dụng phần trăm giảm giá của sản phẩm giống hệt Frontend
                decimal finalPrice = discountPercent > 0
                    ? originalPrice * (1 - (discountPercent / 100))
                    : originalPrice;

                return new CartItemDto
                {
                    CartId = ci.Id,
                    VariantId = ci.VariantId,
                    ProductName = ci.Variant?.Product?.Name,
                    Color = ci.Variant?.Color,
                    Size = ci.Variant?.Size,
                    ImageUrl = ci.Variant?.Product?.MainImage ?? "default-image.jpg",
                    UnitPrice = originalPrice, // Giá gốc sản phẩm
                    Discount = discountPercent, // Truyền kèm tỉ lệ giảm giá sang DTO để Frontend render thẻ {discountRow}
                    Quantity = ci.Quantity,
                    Subtotal = finalPrice * ci.Quantity, // Thành tiền tính dựa trên GIÁ ĐÃ GIẢM
                    StockQuantity = ci.Variant?.StockQuantity ?? 0
                };
            }).ToList();

            return new CartSummaryDto
            {
                Items = list,
                TotalAmount = list.Sum(i => i.Subtotal),
                TotalItems = list.Sum(i => i.Quantity)
            };
        }

        public async Task<ApiResponse<CartSummaryDto>> AddItemToCartAsync(string userId, CartItemDto item)
        {
            // Chuyển đổi DTO để dùng lại hàm AddToCartAsync đã có sẵn
            var dto = new AddToCartDto
            {
                VariantId = item.VariantId,
                Quantity = item.Quantity
            };

            // Gọi lại hàm có sẵn để đảm bảo logic kiểm tra tồn kho và cộng dồn được thực thi nhất quán
            return await AddToCartAsync(userId, dto);
        }
    }
}