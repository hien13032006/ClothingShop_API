using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Data.Repositories;
using ClothingShop.Models; 
using ClothingShop.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClothingShop.Business.Services
{
    public interface IProductService
    {
        Task<ApiResponse<PagedResult<ProductSummaryDto>>> GetProductsAsync(ProductFilterDto filter, string? userId = null);
        Task<List<GenderCategoryDto>> GetCategoriesByGenderAsync();
        Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(int productId, string? userId = null);
        Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductDto dto);
        Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductDto dto);
        Task<List<ProductSummaryDto>> GetHotProductsAsync(int limit, string? userId = null);
        Task<List<ProductSummaryDto>> GetNewArrivalsAsync(int limit, string? userId = null);
        Task<List<ProductSummaryDto>> GetDiscountProductsAsync(int limit, string? userId = null);
        Task<List<ProductSummaryDto>> GetAllProductsAsync(string? userId = null);
        Task<ApiResponse<string>> DeleteProductAsync(int productId);
        Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync();
        Task<ApiResponse<CategoryDto>> CreateCategoryAsync(string name, string? description);
        Task<ApiResponse<string>> DeleteCategoryAsync(int categoryId);
        Task<ApiResponse<VariantDto>> UpdateVariantStockAsync(int variantId, int newStock);
        Task<ApiResponse<string>> UpdateStockBatchAsync(UpdateStockBatchDto dto);
        Task<ApiResponse<PagedResult<InventoryItemDto>>> GetInventoryAsync(int page, int pageSize, string? keyword = null);
        Task<ApiResponse<UploadResultDto>> SaveProductImageAsync(Stream fileStream, string fileName, long fileSize);
    }

    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepo;
        private readonly IReviewRepository _reviewRepo;
        private readonly AppDbContext _context;
        private readonly IHostEnvironment _env;

        public ProductService(
            IProductRepository productRepo,
            IReviewRepository reviewRepo,
            AppDbContext context,
            IHostEnvironment env)
        {
            _productRepo = productRepo;
            _reviewRepo = reviewRepo;
            _context = context;
            _env = env;
        }

        // ── Lấy danh sách với filter/sort ────────────────────────────
        public async Task<ApiResponse<PagedResult<ProductSummaryDto>>> GetProductsAsync(
            ProductFilterDto filter, string? userId = null)
        {
            var query = _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .AsQueryable();
            if (!string.IsNullOrEmpty(filter.Category))
                // Dùng .ToLower() để so sánh không phân biệt hoa thường
                query = query.Where(p => p.Category.ToLower() == filter.Category.ToLower());

            if (!string.IsNullOrEmpty(filter.Gender))
                query = query.Where(p => p.Gender.ToLower() == filter.Gender.ToLower());

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(kw) ||
                    (p.Description != null && p.Description.ToLower().Contains(kw)));
            }

            if (filter.MinPrice.HasValue) query = query.Where(p => p.Price >= filter.MinPrice.Value);
            if (filter.MaxPrice.HasValue) query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(filter.Color))
            {
                var col = filter.Color.ToLower();
                query = query.Where(p => p.Variants.Any(v => v.Color != null && v.Color.ToLower().Contains(col)));
            }

            if (!string.IsNullOrWhiteSpace(filter.Size))
            {
                var sz = filter.Size.ToLower();
                query = query.Where(p => p.Variants.Any(v => v.Size != null && v.Size.ToLower() == sz));
            }

            // Tối ưu hóa SQL translation cho việc sắp xếp top_rated
            query = filter.SortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "best_selling" => query.OrderByDescending(p => p.SoldCount),
                "top_rated" => query.OrderByDescending(p => p.Reviews.Select(r => (double?)r.Rating).Average() ?? 5.0),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            var total = await query.CountAsync();
            var products = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var wishlistIds = await GetUserWishlistIdsAsync(userId);

            return ApiResponse<PagedResult<ProductSummaryDto>>.Ok(new PagedResult<ProductSummaryDto>
            {
                Items = products.Select(p => MapToSummary(p, wishlistIds)).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<List<GenderCategoryDto>> GetCategoriesByGenderAsync()
        {
            // Lấy danh sách duy nhất từng cặp (Gender, Category) từ DB
            var query = await _context.Products
                .Select(p => new { p.Gender, p.Category })
                .Distinct()
                .ToListAsync();

            // Nhóm lại để Frontend nhận được dữ liệu phân cấp
            return query
                .GroupBy(p => p.Gender)
                .Select(g => new GenderCategoryDto
                {
                    Gender = g.Key ?? "Chưa phân loại",
                    // Chỉ lấy danh mục thuộc về đúng giới tính đó
                    Categories = g.Select(x => x.Category)
                                  .Where(c => !string.IsNullOrEmpty(c))
                                  .Distinct()
                                  .ToList()
                })
                .ToList();
        }

        // ── Chi tiết sản phẩm ────────────────────────────────────────
        public async Task<ApiResponse<ProductDetailDto>> GetProductDetailAsync(int productId, string? userId = null)
        {
            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Reviews.OrderByDescending(r => r.CreatedAt).Take(5))
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy sản phẩm");

            bool isInWishlist = false;
            if (!string.IsNullOrEmpty(userId))
                isInWishlist = await _context.Wishlist
                    .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            return ApiResponse<ProductDetailDto>.Ok(MapToDetail(product, isInWishlist));
        }

        // 1. Sản phẩm mới (<= 3 tháng)
        public async Task<List<ProductSummaryDto>> GetNewArrivalsAsync(int limit, string? userId = null) // Thêm userId vào đây
        {
            var wishlistIds = await GetUserWishlistIdsAsync(userId); // Lấy danh sách wishlist
            var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

            return await _context.Products
                .Where(p => p.CreatedAt >= threeMonthsAgo)
                .OrderByDescending(p => p.CreatedAt)
                .Take(limit)
                .Select(p => MapToSummary(p, wishlistIds)) // Truyền wishlistIds vào đây
                .ToListAsync();
        }

        // 2. Sản phẩm Hot (soldCount >= 500)
        public async Task<List<ProductSummaryDto>> GetHotProductsAsync(int limit, string? userId = null)
        {
            var wishlistIds = await GetUserWishlistIdsAsync(userId);
            return await _context.Products
                .Include(p => p.Reviews) // Cần Include để tính AverageRating
                .Where(p => p.SoldCount >= 500)
                .OrderByDescending(p => p.SoldCount)
                .Take(limit)
                .Select(p => MapToSummary(p, wishlistIds)) // Truyền wishlistIds
                .ToListAsync();
        }

        // 3. Sản phẩm khuyến mãi (Discount != null)
        public async Task<List<ProductSummaryDto>> GetDiscountProductsAsync(int limit, string? userId = null)
        {
            var wishlistIds = await GetUserWishlistIdsAsync(userId);
            return await _context.Products
                .Include(p => p.Reviews) // Cần Include để tính AverageRating
                .Where(p => p.Discount != null && p.Discount > 0)
                .OrderByDescending(p => p.Discount)
                .Take(limit)
                .Select(p => MapToSummary(p, wishlistIds)) // Truyền wishlistIds
                .ToListAsync();
        }

        // 4. Tất cả sản phẩm (Có hỗ trợ phân trang nếu cần)
        public async Task<List<ProductSummaryDto>> GetAllProductsAsync(string? userId = null)
        {
            var wishlistIds = await GetUserWishlistIdsAsync(userId);
            return await _context.Products
                .Include(p => p.Reviews)
                .Select(p => MapToSummary(p, wishlistIds)) // Truyền wishlistIds
                .ToListAsync();
        }

        private async Task<HashSet<int>> GetUserWishlistIdsAsync(string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return new HashSet<int>();

            return await _context.Wishlist
                .AsNoTracking()
                .Where(w => w.UserId == userId)
                .Select(w => w.ProductId)
                .ToListAsync()
                .ContinueWith(t => t.Result.ToHashSet());
        }

        // ── Admin: CRUD sản phẩm ─────────────────────────────────────
        public async Task<ApiResponse<ProductDetailDto>> CreateProductAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim() ?? string.Empty,
                Policy = dto.Policy?.Trim() ?? string.Empty,
                Price = dto.BasePrice,
                Discount = dto.Discount,
                MainImage = dto.ImageUrl ?? string.Empty,
                Gender = dto.Gender ?? string.Empty,
                Category = dto.Category ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                Variants = dto.Variants.Select(v => new ProductVariant
                {
                    Color = v.Color?.Trim() ?? string.Empty,
                    Size = v.Size?.Trim() ?? string.Empty,
                    StockQuantity = v.StockQuantity
                }).ToList()
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            return ApiResponse<ProductDetailDto>.Ok(MapToDetail(product, false), "Tạo sản phẩm thành công");
        }

        public async Task<ApiResponse<ProductDetailDto>> UpdateProductAsync(int productId, UpdateProductDto dto)
        {
            // Bổ sung Include Reviews để MapToDetail không bị lỗi tính toán số sao/lượt đánh giá
            var product = await _context.Products
                .Include(p => p.Variants)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return ApiResponse<ProductDetailDto>.Fail("Không tìm thấy sản phẩm");

            if (!string.IsNullOrWhiteSpace(dto.Name)) product.Name = dto.Name.Trim();
            if (dto.Description != null) product.Description = dto.Description.Trim();
            if (dto.BasePrice.HasValue) product.Price = dto.BasePrice.Value;
            if (dto.Discount.HasValue) product.Discount = dto.Discount.Value;
            if (dto.ImageUrl != null) product.MainImage = dto.ImageUrl;
            if (!string.IsNullOrEmpty(dto.Category)) product.Category = dto.Category;

            await _context.SaveChangesAsync();
            return ApiResponse<ProductDetailDto>.Ok(MapToDetail(product, false), "Cập nhật thành công");
        }

        public async Task<ApiResponse<string>> DeleteProductAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return ApiResponse<string>.Fail("Không tìm thấy sản phẩm");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã xóa sản phẩm");
        }

        // ── Danh mục ─────────────────────────────────────────────────
        public async Task<ApiResponse<List<CategoryDto>>> GetCategoriesAsync()
        {
            var categories = await _context.Products
                .Where(p => !string.IsNullOrEmpty(p.Category))
                .Select(p => p.Category)
                .Distinct()
                .ToListAsync();

            var result = categories.Select(c => new CategoryDto
            {
                Name = c,
                ProductCount = _context.Products.Count(p => p.Category == c)
            }).ToList();

            return ApiResponse<List<CategoryDto>>.Ok(result);
        }

        public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(string name, string? description)
        {
            return ApiResponse<CategoryDto>.Ok(new CategoryDto { Name = name }, "Quản lý theo chuỗi trực tiếp");
        }

        public async Task<ApiResponse<string>> DeleteCategoryAsync(int categoryId)
        {
            return ApiResponse<string>.Ok("OK");
        }

        // ── Quản lý kho (Inventory) ──────────────────────────────────
        public async Task<ApiResponse<VariantDto>> UpdateVariantStockAsync(int variantId, int newStock)
        {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) return ApiResponse<VariantDto>.Fail("Không tìm thấy biến thể");
            variant.StockQuantity = newStock;
            await _context.SaveChangesAsync();
            return ApiResponse<VariantDto>.Ok(MapVariant(variant), "Cập nhật tồn kho thành công");
        }

        public async Task<ApiResponse<string>> UpdateStockBatchAsync(UpdateStockBatchDto dto)
        {
            foreach (var item in dto.Items)
            {
                var v = await _context.ProductVariants.FindAsync(item.VariantId);
                if (v != null) v.StockQuantity = item.NewStock;
            }
            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", $"Đã cập nhật {dto.Items.Count} biến thể");
        }

        public async Task<ApiResponse<PagedResult<InventoryItemDto>>> GetInventoryAsync(
            int page, int pageSize, string? keyword = null)
        {
            var query = _context.ProductVariants.Include(v => v.Product).AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(v => v.Product!.Name.ToLower().Contains(kw));
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderBy(v => v.StockQuantity)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return ApiResponse<PagedResult<InventoryItemDto>>.Ok(new PagedResult<InventoryItemDto>
            {
                Items = items.Select(v => new InventoryItemDto
                {
                    VariantId = v.VariantId,
                    ProductId = v.ProductId,
                    ProductName = v.Product?.Name ?? "",
                    Color = v.Color,
                    Size = v.Size,
                    StockQuantity = v.StockQuantity,
                    StockStatus = v.StockQuantity == 0 ? "Hết hàng"
                                  : v.StockQuantity < 5 ? "Sắp hết"
                                  : "Còn hàng"
                }).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        // ── Tải ảnh lên ──────────────────────────────────────────────
        public async Task<ApiResponse<UploadResultDto>> SaveProductImageAsync(
            Stream fileStream, string fileName, long fileSize)
        {
            if (fileSize > 5 * 1024 * 1024)
                return ApiResponse<UploadResultDto>.Fail("Ảnh không vượt quá 5MB");

            var ext = Path.GetExtension(fileName).ToLower();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                return ApiResponse<UploadResultDto>.Fail("Chỉ chấp nhận .jpg, .png, .webp");

            var dir = Path.Combine(_env.ContentRootPath, "wwwroot", "images", "products");
            Directory.CreateDirectory(dir);

            var name = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(dir, name);
            await using (var fs = new FileStream(path, FileMode.Create))
                await fileStream.CopyToAsync(fs);

            return ApiResponse<UploadResultDto>.Ok(new UploadResultDto
            {
                Url = $"/images/products/{name}",
                FileName = name,
                FileSizeBytes = fileSize
            }, "Upload thành công");
        }

        // ── Bộ chuyển đổi thủ công (Vá lỗi đồng bộ trường Discount) ──
        private static ProductSummaryDto MapToSummary(Product p, HashSet<int> wishlistIds)
        {
            return new ProductSummaryDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,          // ✅ ĐÃ ĐỒNG BỘ: Đổi từ BasePrice sang Price
                MainImage = p.MainImage,  // ✅ ĐÃ ĐỒNG BỘ: Đổi từ ImageUrl sang MainImage
                Category = p.Category,
                Gender = p.Gender,        // Gán thêm trường Gender nếu DTO có yêu cầu
                IsActive = true,
                SoldCount = p.SoldCount,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                IsInWishlist = wishlistIds.Contains(p.ProductId),
                Discount = p.Discount
            };
        }

        private static ProductDetailDto MapToDetail(Product p, bool isInWishlist) => new()
        {
            ProductId = p.ProductId,
            Name = p.Name,
            Description = p.Description,
            Policy = p.Policy,
            Category = p.Category,
            Gender = p.Gender,
            SoldCount = p.SoldCount,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            IsInWishlist = isInWishlist,

            // Trả về đúng định nghĩa giá gốc để Front-end tự tính finalPrice
            Price = p.Price,
            OldPrice = p.Price,
            Discount = p.Discount ?? 0,

            // Đảm bảo MainImage luôn được gửi đi để tránh bị rơi vào ảnh placeholder lỗi
            MainImage = p.MainImage,

            Thumbnails = p.Thumbnails != null && p.Thumbnails.Any()
                ? string.Join(",", p.Thumbnails).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList()
                : new List<string>(),

            // Tự map dữ liệu Review tường minh, không gọi hàm ngoài
            Reviews = p.Reviews.Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId,
                ProductId = r.ProductId,
                UserId = r.UserId,
                OrderId = r.OrderId,
                CustomerName = r.CustomerName,
                Rating = r.Rating,
                Comment = r.Comment,
                IsVerifiedPurchase = true,
                CreatedAt = r.CreatedAt
            }).ToList(),

            RecentReviews = p.Reviews.OrderByDescending(r => r.CreatedAt).Take(5).Select(r => new ReviewDto
            {
                ReviewId = r.ReviewId,
                ProductId = r.ProductId,
                UserId = r.UserId,
                OrderId = r.OrderId,
                CustomerName = r.CustomerName,
                Rating = r.Rating,
                Comment = r.Comment,
                IsVerifiedPurchase = true,
                CreatedAt = r.CreatedAt
            }).ToList(),

            
            Variants = p.Variants.Select(MapVariant).ToList()
        };

        private static VariantDto MapVariant(ProductVariant v) => new()
        {
            VariantId = v.VariantId,
            Color = v.Color,
            Size = v.Size,
            StockQuantity = v.StockQuantity,
            
        };
    }
}