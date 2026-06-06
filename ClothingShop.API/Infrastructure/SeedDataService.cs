using ClothingShop.Data;
using ClothingShop.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClothingShop.API.Infrastructure
{
    public static class SeedDataService
    {
        public static async Task SeedAsync(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Đảm bảo Database đã được tạo
                await context.Database.EnsureCreatedAsync();

                // 1. THÊM DỮ LIỆU SẢN PHẨM (Để hiển thị trang chủ/danh mục)
                if (!await context.Set<Product>().AnyAsync())
                {
                    var products = new List<Product>
                    {
                        new Product
                        {
                            Name = "Áo babydoll ROSE top thô boil tay bồng phối dây buộc nơ",
                            Category = "kieu",
                            Gender = "nu",
                            Price = 169000,
                            Discount = 23,
                            MainImage = "images/a1.jpg",
                            Thumbnails = new List<string>{"images/a1_1.jpg,images/a1_2.jpg,images/a1_3.jpg" },
                            SoldCount = 1200,
                            Description = "Chất liệu thô boil mềm mại, thấm hút mồ hôi tốt. Thiết kế tay bồng nữ tính.",
                            Policy = "Hỗ trợ đổi trả trong vòng 7 ngày.",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Product
                        {
                            Name = "Áo sơ mi cổ bẻ dài tay sọc ZUTE - Nam Linen cao cấp",
                            Category = "somi",
                            Gender = "nam",
                            Price = 140000,
                            Discount = 22,
                            MainImage = "images/a2.jpg",
                            Thumbnails = new List<string>{"images/a2.jpg"},
                            SoldCount = 95,
                            Description = "Phong cách lịch lãm, phù hợp công sở.",
                            Policy = "Giặt máy nhẹ nhàng.",
                            CreatedAt = DateTime.UtcNow
                        },
                        new Product
                        {
                            Name = "Váy Nữ Chấm Bi Ngắn Tay Bồng Họa Tiết Kèm Tag Nơ",
                            Category = "vaydam",
                            Gender = "nu",
                            Price = 350000,
                            Discount = 0,
                            MainImage = "images/moi1.jpg",
                            Thumbnails = new List<string>{"images/moi1.jpg" },
                            SoldCount = 10,
                            Description = "Đầm dự tiệc dạo phố chấm bi phối nơ dáng ngắn 130 là mẫu đầm nữ mang phong cách trẻ trung, " +
                                          "nữ tính và thanh lịch, phù hợp cho các dịp dạo phố, đi chơi, hẹn hò, chụp ảnh hoặc dự tiệc nhẹ. " +
                                          "Thiết kế họa tiết chấm bi cổ điển kết hợp chi tiết nơ duyên dáng tạo điểm nhấn nổi bật, " +
                                          "giúp tổng thể thêm phần ngọt ngào và cuốn hút.\r\n\r\nPhần dáng ngắn năng động giúp đôi " +
                                          "chân trông dài và thon gọn hơn, đồng thời mang lại vẻ ngoài trẻ trung, dễ thương. " +
                                          "Phom dáng được cắt may khéo léo giúp tôn eo, tôn dáng, che khuyết điểm nhẹ vùng hông, " +
                                          "tạo silhouette cân đối và hài hòa. Chất liệu mềm mại, thoáng mát, mang lại cảm giác dễ chịu khi mặc trong thời gian dài.\r\n\r\n" +
                                          "Đầm dễ dàng phối cùng giày cao gót, sandal hoặc túi xách nhỏ để hoàn thiện phong cách từ nhẹ nhàng đến thanh lịch. " +
                                          "Đây là lựa chọn lý tưởng cho những ai yêu thích đầm chấm bi dáng ngắn phối nơ nữ tính, dễ mặc và dễ phối đồ.",
                            Policy = "Đổi trả nếu có lỗi sản xuất.",
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    await context.Set<Product>().AddRangeAsync(products);
                }

                // 2. THÊM DỮ LIỆU VOUCHER/PROMOTION (Để hiển thị banner/mã giảm giá)
                // Giả sử bảng của bạn tên là Promotion hoặc Voucher
                // 2. THÊM DỮ LIỆU VOUCHER/PROMOTION (Khớp 100% với Model Promotion mới của bạn)
                if (!await context.Set<Promotion>().AnyAsync())
                {
                    var promotions = new List<Promotion>
                    {
                        new Promotion
                        {
                            Code = "HE2026",
                            DiscountType = "Percent", // Giảm theo phần trăm
                            DiscountValue = 15,       // Giảm 15%
                            MinOrderAmount = 300000,   // Đơn tối thiểu 300k mới áp dụng được
                            MaxDiscountAmount = 50000, // Giảm tối đa 50k (Chặn trần theo hàm CalcDiscount của bạn)
                            StartDate = DateTime.UtcNow.AddDays(-1), // Đã bắt đầu từ hôm qua
                            EndDate = DateTime.UtcNow.AddMonths(2),  // Hết hạn sau 2 tháng nữa
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        },
                        new Promotion
                        {
                            Code = "COMPAS",
                            DiscountType = "FixedAmount", // Giảm số tiền cố định
                            DiscountValue = 20000,        // Giảm thẳng 20k
                            MinOrderAmount = 100000,      // Đơn tối thiểu chỉ cần 100k
                            MaxDiscountAmount = null,     // Giảm cố định nên không cần trần tối đa
                            StartDate = DateTime.UtcNow.AddDays(-5),
                            EndDate = DateTime.UtcNow.AddMonths(1),
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        }
                    };
                    await context.Set<Promotion>().AddRangeAsync(promotions);
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Bạn có thể log lỗi ở đây để debug
                Console.WriteLine($"Lỗi Seed Data: {ex.Message}");
            }
        }
    }
}