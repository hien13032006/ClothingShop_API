using Microsoft.EntityFrameworkCore;
using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface IOrderService
    {
        Task<ApiResponse<OrderDto>> CreateOrderAsync(string userId, CreateOrderDto dto);
        Task<ApiResponse<OrderDto>> GetOrderDetailAsync(string orderId, string userId);
        Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId, string status);
        Task<ApiResponse<string>> CancelOrderAsync(string orderId, string userId, string reason);
        Task<ApiResponse<string>> ReturnOrderAsync(string orderId, string userId, string reason);
        Task<ApiResponse<string>> ConfirmReceivedAsync(string orderId, string userId);
        Task<ApiResponse<string>> AddReviewAsync(string orderId, string userId, ReviewDto dto);
        Task<ApiResponse<PagedResult<OrderDto>>> GetAllOrdersAsync(OrderFilterDto filter);
        Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(string orderId, UpdateOrderStatusDto dto);
        Task<ApiResponse<OrderCalculationDto>> CalculateOrderAsync(List<CartItemDto> items, string? promoCode = null);
    }

    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepo;
        private readonly IProductRepository _productRepo;
        private readonly ICartRepository _cartRepo;
        private readonly ICustomerRepository _customerRepo;
        private readonly IPromotionRepository _promoRepo;
        private readonly INotificationService _notifService;
        private readonly AppDbContext _context;
        private readonly IShippingService _shippingService;

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            ICartRepository cartRepo,
            ICustomerRepository customerRepo,
            IPromotionRepository promoRepo,
            INotificationService notifService,
            IShippingService shippingService,
            AppDbContext context)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _cartRepo = cartRepo;
            _customerRepo = customerRepo;
            _promoRepo = promoRepo;
            _notifService = notifService;
            _shippingService = shippingService;
            _context = context;
        }

        public async Task<ApiResponse<OrderDto>> CreateOrderAsync(string userId, CreateOrderDto dto)
        {
            // 1. Kiểm tra cơ bản (Validation)
            if (!dto.Items.Any()) return ApiResponse<OrderDto>.Fail("Đơn hàng không có sản phẩm");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 2. Lấy thông tin khách hàng & địa chỉ
                    var customer = await _context.Customers.FirstOrDefaultAsync(u => u.UserId == userId);
                    var address = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.AddressId == dto.AddressId && a.UserId == userId);
                    if (customer == null) return ApiResponse<OrderDto>.Fail("Không tìm thấy thông tin khách hàng");
                    if (address == null) return ApiResponse<OrderDto>.Fail("Địa chỉ giao hàng không hợp lệ");

                    // 3. Tính toán giá trị & kiểm tra tồn kho
                    decimal subTotal = 0;
                    var orderId = await _orderRepo.GenerateNextOrderIdAsync(); // Tạo ID từ Repo
                    var orderDetails = new List<OrderDetail>();

                    foreach (var item in dto.Items)
                    {
                        var v = await _context.ProductVariants
                            .Include(x => x.Product)
                            .FirstOrDefaultAsync(x => x.VariantId == item.VariantId);

                        if (v == null || v.Product == null) throw new Exception("Sản phẩm không tồn tại");
                        if (v.StockQuantity < item.Quantity) throw new Exception($"Sản phẩm {v.Product.Name} không đủ hàng");

                        decimal unitPrice = v.Product.Price;
                        subTotal += unitPrice * item.Quantity;

                        // Thêm vào chi tiết đơn hàng
                        orderDetails.Add(new OrderDetail
                        {
                            OrderId = orderId,
                            VariantId = item.VariantId,
                            Quantity = item.Quantity,
                            UnitPrice = unitPrice,
                            LineTotal = unitPrice * item.Quantity
                        });

                        // Trừ kho & tăng lượt bán
                        v.StockQuantity -= item.Quantity;
                        v.Product.SoldCount += item.Quantity;
                    }

                    // 4. Xử lý Voucher (Nếu có)
                    decimal discount = 0;
                    if (!string.IsNullOrWhiteSpace(dto.PromotionCode))
                    {
                        var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == dto.PromotionCode.ToUpper());
                        if (promo == null || !promo.HasUsageLeft()) throw new Exception("Mã giảm giá không hợp lệ hoặc đã hết lượt dùng");

                        var isUsed = await _context.PromotionUsages.AnyAsync(u => u.UserId == userId && u.PromotionId == promo.PromotionId);
                        if (isUsed) throw new Exception("Bạn đã sử dụng mã này rồi");

                        discount = promo.CalcDiscount(subTotal);
                        promo.UsedCount++;
                        await _context.PromotionUsages.AddAsync(new PromotionUsage
                        {
                            UserId = userId,
                            PromotionId = promo.PromotionId,
                            OrderId = orderId,
                            UsedAt = DateTime.UtcNow
                        });
                    }
                    var shippingResult = await _shippingService.GetFeeAsync(dto.ShippingMethod, subTotal);

                    Console.WriteLine($"[CRITICAL DEBUG]");
                    Console.WriteLine($"Subtotal: {subTotal}");
                    Console.WriteLine($"Shipping Method input: {dto.ShippingMethod}");
                    Console.WriteLine($"Shipping Result Success: {shippingResult.Success}");

                    if (shippingResult.Success)
                    {
                        Console.WriteLine($"Fee trả về từ Service: {shippingResult.Data.Fee}");
                    }
                    else
                    {
                        Console.WriteLine($"Lỗi từ Service: {shippingResult.Message}");
                    }

                    decimal shippingFee = shippingResult.Success ? shippingResult.Data.Fee : 0;

                    if (subTotal >= 300000)
                    {
                        shippingFee = 0;
                    }
                    Console.WriteLine($"DEBUG: Giá trị shippingFee tính được là: {shippingFee}");
                    // 5. Tạo đối tượng Order
                    var order = new Order
                    {
                        OrderId = orderId,
                        UserId = userId,
                        OrderDate = DateTime.Now,
                        TotalPrice = subTotal,
                        DiscountAmount = discount,
                        // Đảm bảo không bao giờ là null và không bao giờ âm
                        ShippingFee = shippingFee,
                        FinalPrice = Math.Max(0, (subTotal + shippingFee) - discount),
                        ShippingMethod = dto.ShippingMethod ?? "Standard",
                        PaymentMethod = dto.PaymentMethod ?? "COD",
                        Status = "Chờ xác nhận",
                        OrderDetails = orderDetails // orderDetails phải được khởi tạo ở trên
                    };

                    // 6. Tích điểm
                    int points = (int)((subTotal - discount) / 10_000);
                    if (points > 0)
                    {
                        customer.TotalPoints += points;
                        await _context.PointHistories.AddAsync(new PointHistory { UserId = userId, Points = points, Reason = "Mua hàng", OrderId = orderId });
                    }

                    // 7. Lưu tất cả
                    await _context.Orders.AddAsync(order);
                    await _cartRepo.ClearCartAsync(userId);
                    var entry = _context.Entry(order);
                    Console.WriteLine($"Trạng thái của Order: {entry.State}");
                    foreach (var property in entry.Properties)
                    {
                        Console.WriteLine($"Cột: {property.Metadata.Name}, Giá trị: {property.CurrentValue}");
                    }
                    await _context.SaveChangesAsync();      // LƯU TẤT CẢ TẠI ĐÂY
                    await tx.CommitAsync();

                    // 8. Gửi thông báo & trả về kết quả
                    await _notifService.CreateAsync(userId, "order_update", "Đặt hàng thành công! 🎉", $"Đơn hàng #{orderId} đã được đặt.", orderId);

                    return ApiResponse<OrderDto>.Ok(new OrderDto { OrderId = orderId }, "Đặt hàng thành công");
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync();

                    // Ghi log chi tiết nhất có thể
                    var message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += " | Inner: " + ex.InnerException.Message;
                        // Nếu là lỗi liên quan đến EF, lấy thêm thông tin từ Entries
                        if (ex is Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                        {
                            foreach (var entry in dbEx.Entries)
                            {
                                message += $" | Entity bị lỗi: {entry.Metadata.Name}";
                            }
                        }
                    }

                    Console.WriteLine($"[LỖI ĐẶT HÀNG]: {message}");
                    return ApiResponse<OrderDto>.Fail(message);
                }
            });
        }

        public async Task<ApiResponse<OrderDto>> GetOrderDetailAsync(string orderId, string userId)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (order == null) return ApiResponse<OrderDto>.Fail("Không tìm thấy đơn hàng");
            if (order.UserId != userId && !userId.StartsWith("AD"))
                return ApiResponse<OrderDto>.Fail("Không có quyền xem đơn hàng này");

            var dto = MapToDto(order);
            if (order.Status == "Hoàn thành")
            {
                foreach (var detail in dto.Details)
                {
                    var variant = await _context.ProductVariants.FindAsync(detail.VariantId);
                    if (variant == null) continue;
                    detail.CanReview = !await _context.ProductReviews.AnyAsync(r =>
                        r.UserId == userId && r.ProductId == variant.ProductId && r.OrderId == orderId);
                }
            }

            return ApiResponse<OrderDto>.Ok(dto);
        }

        private string MapStatus(string status)
        {
            var map = new Dictionary<string, string> {
                { "pending", "Chờ xác nhận" },
                { "preparing", "Đang chuẩn bị" },
                { "shipping", "Đang giao" },
                { "completed", "Hoàn thành" },
                { "returned", "Trả hàng/Hoàn tiền" },
                { "cancelled", "Đã hủy" }
            };
             return map.ContainsKey(status) ? map[status] : status;
        }

        public async Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId, string status)
        {
            // 1. Lấy tất cả đơn hàng
            var orders = await _orderRepo.GetByCustomerAsync(userId);

            // 2. Lọc dữ liệu
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                if (status.ToLower() == "cancelled")
                {
                    orders = orders.Where(o =>
                        !string.IsNullOrEmpty(o.Status) &&
                        (o.Status.Trim().Equals("Đã hủy", StringComparison.OrdinalIgnoreCase) ||
                         o.Status.Trim().Equals("Trả hàng/Hoàn tiền", StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }
                else
                {
                    // Các tab thông thường khác
                    var dbStatus = MapStatus(status);
                    orders = orders.Where(o =>
                        !string.IsNullOrEmpty(o.Status) &&
                        o.Status.Trim().Equals(dbStatus, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }
                
            }

            // 3. Map sang DTO và trả về
            return ApiResponse<List<OrderDto>>.Ok(orders.Select(MapToDto).ToList());
        }

        private async Task<ApiResponse<string>> ProcessOrderReversalAsync(string orderId, string userId, string reason, string newStatus, bool updateStock)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (order == null) return ApiResponse<string>.Fail("Không tìm thấy đơn hàng");
            if (order.UserId != userId) return ApiResponse<string>.Fail("Không có quyền thực hiện thao tác này");

            // 1. Cập nhật trạng thái và lý do
            order.Status = newStatus;
            order.CancelReason = reason;
            _orderRepo.Update(order);

            // 2. Cập nhật kho và lượt bán (chỉ chạy khi updateStock = true)
            if (updateStock)
            {
                foreach (var d in order.OrderDetails)
                {
                    await _productRepo.UpdateStockAsync(d.VariantId, d.Quantity);
                    var v = await _context.ProductVariants.Include(x => x.Product)
                        .FirstOrDefaultAsync(x => x.VariantId == d.VariantId);
                    if (v?.Product != null)
                    {
                        v.Product.SoldCount = Math.Max(0, v.Product.SoldCount - d.Quantity);
                    }
                }
            }

            // 3. Hoàn điểm (Logic giữ nguyên)
            var pointRecord = await _context.PointHistories
                .FirstOrDefaultAsync(p => p.UserId == userId && p.OrderId == orderId && p.Points > 0);
            if (pointRecord != null)
            {
                var customer = await _customerRepo.GetByIdAsync(userId);
                if (customer != null)
                {
                    customer.TotalPoints = Math.Max(0, customer.TotalPoints - pointRecord.Points);
                    UpdateMembershipLevel(customer);
                    await _customerRepo.UpdateAsync(customer);
                    await _context.PointHistories.AddAsync(new PointHistory
                    {
                        UserId = userId,
                        OrderId = orderId,
                        Points = -pointRecord.Points,
                        Reason = $"Hoàn điểm do {newStatus} đơn {orderId}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();

            // 4. Tracking và Thông báo
            await _orderRepo.AddTrackingAsync(new OrderTracking
            {
                OrderId = orderId,
                StatusUpdate = $"Đơn hàng chuyển trạng thái: {newStatus}",
                UpdatedAt = DateTime.Now
            });

            string notifBody = $"Đơn #{orderId} đã chuyển sang trạng thái {newStatus}.";
            if (order.PaymentMethod == "Thanh toán online") notifBody += " Bạn sẽ được hoàn tiền sớm.";
            await _notifService.CreateAsync(userId, "order_update", newStatus, notifBody, orderId);

            return ApiResponse<string>.Ok("OK", "Thao tác thành công");
        }

        public async Task<ApiResponse<string>> CancelOrderAsync(string orderId, string userId, string reason)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (!new[] { "Chờ xác nhận", "Đang chuẩn bị" }.Contains(order?.Status))
                return ApiResponse<string>.Fail("Không thể hủy đơn ở trạng thái này");

            return await ProcessOrderReversalAsync(orderId, userId, reason, "Đã hủy", true);
        }

        // Dùng cho Trả hàng (KHÔNG cập nhật kho)
        public async Task<ApiResponse<string>> ReturnOrderAsync(string orderId, string userId, string reason)
        {
            // Có thể thêm kiểm tra trạng thái khác, ví dụ chỉ cho trả nếu status là "Hoàn thành"
            return await ProcessOrderReversalAsync(orderId, userId, $"Trả hàng: {reason}", "Trả hàng/Hoàn tiền", false);
        }

        // --- XÁC NHẬN ĐÃ NHẬN HÀNG ---
        public async Task<ApiResponse<string>> ConfirmReceivedAsync(string orderId, string userId)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (order == null || order.UserId != userId)
                return ApiResponse<string>.Fail("Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu");

            if (order.Status != "Đang giao")
                return ApiResponse<string>.Fail("Đơn hàng phải đang ở trạng thái 'Đang giao' mới có thể xác nhận");

            order.Status = "Hoàn thành";
            // Nếu thanh toán COD, tự động cập nhật là đã thanh toán khi khách nhận hàng
            if (order.Payment != null) order.Payment.PaymentStatus = "Đã thanh toán";

            await _orderRepo.AddTrackingAsync(new OrderTracking
            {
                OrderId = orderId,
                StatusUpdate = "Khách hàng đã xác nhận nhận hàng",
                UpdatedAt = DateTime.Now
            });

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();

            return ApiResponse<string>.Ok("Đơn hàng đã được chuyển sang Hoàn thành");
        }

        // --- THÊM ĐÁNH GIÁ ---
        public async Task<ApiResponse<string>> AddReviewAsync(string orderId, string userId, ReviewDto dto)
        {
            var order = await _context.Orders.Include(o => o.OrderDetails).FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);
            if (order == null || order.Status != "Hoàn thành")
                return ApiResponse<string>.Fail("Chỉ có thể đánh giá đơn hàng đã Hoàn thành");

            foreach (var detail in order.OrderDetails)
            {
                var variant = await _context.ProductVariants.FindAsync(detail.VariantId);
                if (variant == null) continue;

                var review = new ProductReview
                {
                    UserId = userId,
                    ProductId = variant.ProductId,
                    OrderId = orderId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.Now
                };
                await _context.ProductReviews.AddAsync(review);
            }

            await _context.SaveChangesAsync();
            return ApiResponse<string>.Ok("Đánh giá của bạn đã được ghi nhận");
        }

        public async Task<ApiResponse<PagedResult<OrderDto>>> GetAllOrdersAsync(OrderFilterDto filter)
        {
            var query = _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.Payment)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(o => o.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var kw = filter.Keyword.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderId.ToLower().Contains(kw) ||
                    (o.Customer != null && o.Customer.FullName != null &&
                     o.Customer.FullName.ToLower().Contains(kw)));
            }

            if (filter.FromDate.HasValue) query = query.Where(o => o.OrderDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue) query = query.Where(o => o.OrderDate <= filter.ToDate.Value);

            var total = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return ApiResponse<PagedResult<OrderDto>>.Ok(new PagedResult<OrderDto>
            {
                Items = orders.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = filter.Page,
                PageSize = filter.PageSize
            });
        }

        public async Task<ApiResponse<OrderCalculationDto>> CalculateOrderAsync(List<CartItemDto> items, string? promoCode = null)
        {
            decimal subTotal = 0;
            // 1. Khai báo biến discount ở đây
            decimal discount = 0;
            var calculatedItems = new List<CalculatedItemDto>();

            foreach (var item in items)
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantId == item.VariantId);

                if (variant == null) continue;

                decimal price = variant.Product.Price;
                subTotal += price * item.Quantity;

                calculatedItems.Add(new CalculatedItemDto
                {
                    Name = variant.Product.Name,
                    Price = price,
                    Quantity = item.Quantity,
                    Color = variant.Color,
                    Size = variant.Size,
                    ImageUrl = variant.Product.MainImage
                });
            }

            if (!string.IsNullOrWhiteSpace(promoCode))
            {
                var promo = await _promoRepo.GetByCodeAsync(promoCode.ToUpper());
                // Kiểm tra xem promo có hợp lệ với subTotal hiện tại không
                if (promo != null && promo.IsValid(subTotal))
                {
                    discount = promo.CalcDiscount(subTotal);
                }
            }

            // 2. Bây giờ biến 'discount' đã tồn tại và có thể sử dụng
            return ApiResponse<OrderCalculationDto>.Ok(new OrderCalculationDto
            {
                Items = calculatedItems,
                SubTotal = subTotal,
                DiscountAmount = discount,
                FinalTotal = subTotal - discount
            });
        }

        public async Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(string orderId, UpdateOrderStatusDto dto)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (order == null) return ApiResponse<OrderDto>.Fail("Không tìm thấy đơn hàng");

            var oldStatus = order.Status;
            order.Status = dto.Status;

            if (dto.Status == "Hoàn thành" && order.Payment?.PaymentMethod == "Thanh toán khi nhận hàng")
                order.Payment.PaymentStatus = "Đã thanh toán";

            if (dto.Status == "Hủy" && oldStatus != "Hủy")
                foreach (var d in order.OrderDetails)
                    await _productRepo.UpdateStockAsync(d.VariantId, d.Quantity);

            await _orderRepo.AddTrackingAsync(new OrderTracking
            {
                OrderId = orderId,
                StatusUpdate = dto.StatusNote ?? $"Trạng thái: {dto.Status}",
                LocationLatLong = dto.LocationLatLong,
                UpdatedAt = DateTime.Now
            });

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();

            var messages = new Dictionary<string, (string t, string b)>
            {
                ["Đang chuẩn bị"] = ("Đơn hàng đang được chuẩn bị 📦", $"Đơn #{orderId} đang đóng gói."),
                ["Đang giao"] = ("Đơn hàng đang trên đường 🚚", $"Đơn #{orderId} đang giao đến bạn."),
                ["Hoàn thành"] = ("Giao hàng thành công! ✅", $"Đơn #{orderId} đã giao thành công."),
                ["Hủy"] = ("Đơn hàng đã bị hủy ❌", $"Đơn #{orderId} đã bị hủy.")
            };

            if (messages.TryGetValue(dto.Status, out var msg))
                await _notifService.CreateAsync(order.UserId, "order_update", msg.t, msg.b, orderId);

            return ApiResponse<OrderDto>.Ok(MapToDto(order), "Cập nhật thành công");
        }

        private static decimal CalcMemberDiscount(string level, decimal total) => level switch
        {
            "Vàng" => Math.Round(total * 0.05m, 2),
            "Kim cương" => Math.Round(total * 0.10m, 2),
            _ => 0
        };

        private static void UpdateMembershipLevel(Customer c)
        {
            c.MembershipLevel = c.TotalPoints switch
            {
                >= 5000 => "Kim cương",
                >= 1500 => "Vàng",
                _ => "Bạc"
            };
        }



        private static OrderDto MapToDto(Order o) => new()
        {
            OrderId = o.OrderId,
            CustomerName = o.Customer?.FullName,
            OrderDate = o.OrderDate,
            Phone = o.Customer?.Phone ?? "Chưa cập nhật",
            Address = o.Customer?.Addresses?.FirstOrDefault(a => a.IsDefault)?.AddressDetail
              ?? o.Customer?.Addresses?.FirstOrDefault()?.AddressDetail
              ?? "Chưa có địa chỉ",
            TotalPrice = o.TotalPrice,
            ShippingFee = o.ShippingFee,
            DiscountAmount = o.DiscountAmount,
            FinalPrice = o.TotalPrice - o.DiscountAmount + o.ShippingFee,
            ShippingMethod = o.ShippingMethod,
            PaymentMethod = o.PaymentMethod,
            Status = o.Status,
            HasReviewed = o.HasReviewed,
            Payment = o.Payment == null ? null : new PaymentInfoDto
            {
                PaymentMethod = o.Payment.PaymentMethod,
                PaymentStatus = o.Payment.PaymentStatus,
                TransactionId = o.Payment.TransactionId,
                AmountPaid = o.Payment.AmountPaid
            },
            Details = o.OrderDetails.Select(od => new OrderDetailDto
            {
                OrderDetailId = od.OrderDetailId,
                VariantId = od.VariantId,
                ProductId = od.Variant?.ProductId ?? 0,
                ProductName = od.Variant?.Product?.Name,
                Color = od.Variant?.Color,
                Size = od.Variant?.Size,
                Quantity = od.Quantity,
                ImageUrl = od.Variant?.Product?.MainImage ?? "default.jpg",
                UnitPrice = od.UnitPrice,
                LineTotal = od.LineTotal,
                CanReview = false
            }).ToList(),
            Trackings = o.Trackings
                .OrderByDescending(t => t.UpdatedAt)
                .Select(t => new TrackingDto
                {
                    StatusUpdate = t.StatusUpdate,
                    UpdatedAt = t.UpdatedAt,
                    LocationLatLong = t.LocationLatLong
                }).ToList()
        };




    }
}