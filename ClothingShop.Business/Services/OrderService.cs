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
        Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId);
        Task<ApiResponse<string>> CancelOrderAsync(string orderId, string userId);
        Task<ApiResponse<PagedResult<OrderDto>>> GetAllOrdersAsync(OrderFilterDto filter);
        Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(string orderId, UpdateOrderStatusDto dto);
        Task<ApiResponse<OrderCalculationDto>> CalculateOrderAsync(List<CartItemDto> items);
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

        public OrderService(
            IOrderRepository orderRepo,
            IProductRepository productRepo,
            ICartRepository cartRepo,
            ICustomerRepository customerRepo,
            IPromotionRepository promoRepo,
            INotificationService notifService,
            AppDbContext context)
        {
            _orderRepo = orderRepo;
            _productRepo = productRepo;
            _cartRepo = cartRepo;
            _customerRepo = customerRepo;
            _promoRepo = promoRepo;
            _notifService = notifService;
            _context = context;
        }

        public async Task<ApiResponse<OrderDto>> CreateOrderAsync(string userId, CreateOrderDto dto)
        {
            if (!dto.Items.Any())
                return ApiResponse<OrderDto>.Fail("Đơn hàng không có sản phẩm");

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == dto.AddressId && a.UserId == userId);
            if (address == null)
                return ApiResponse<OrderDto>.Fail("Địa chỉ giao hàng không hợp lệ");

            decimal totalPrice = 0;
            var detailList = new List<OrderDetail>();

            foreach (var item in dto.Items)
            {
                var variant = await _productRepo.GetVariantAsync(item.VariantId);
                if (variant == null)
                    return ApiResponse<OrderDto>.Fail($"Không tìm thấy sản phẩm (variant_id={item.VariantId})");

                // ĐmanagerẮ SỬA: Loại bỏ kiểm tra !variant.Product.IsActive vì trường này không còn trong bảng Product
                if (variant.Product == null)
                    return ApiResponse<OrderDto>.Fail($"Sản phẩm liên kết với biến thể không tồn tại");

                if (variant.StockQuantity < item.Quantity)
                    return ApiResponse<OrderDto>.Fail(
                        $"'{variant.Product.Name}' ({variant.Color}/{variant.Size}) chỉ còn {variant.StockQuantity}");

                // ĐC SỬA: Thay variant.ActualPrice bằng giá gốc variant.Product.Price
                decimal itemPrice = variant.Product.Price;

                totalPrice += itemPrice * item.Quantity;
                detailList.Add(new OrderDetail
                {
                    VariantId = item.VariantId,
                    Quantity = item.Quantity,
                    UnitPrice = itemPrice
                });
            }

            var customer = await _customerRepo.GetByIdAsync(userId);
            if (customer == null) return ApiResponse<OrderDto>.Fail("Không tìm thấy tài khoản");

            decimal memberDiscount = CalcMemberDiscount(customer.MembershipLevel, totalPrice);
            decimal promoDiscount = 0;

            if (!string.IsNullOrWhiteSpace(dto.PromotionCode))
            {
                var promo = await _promoRepo.GetByCodeAsync(dto.PromotionCode.ToUpper());
                if (promo == null)
                    return ApiResponse<OrderDto>.Fail("Mã khuyến mãi không hợp lệ hoặc đã hết hạn");
                promoDiscount = promo.CalcDiscount(totalPrice);
            }

            var orderId = await _orderRepo.GenerateNextOrderIdAsync();
            var order = new Order
            {
                OrderId = orderId,
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalPrice = totalPrice,
                DiscountAmount = memberDiscount + promoDiscount,
                FinalPrice = totalPrice - (memberDiscount + promoDiscount),
                ShippingMethod = dto.ShippingMethod,
                PaymentMethod = dto.PaymentMethod,
                Status = "Chờ xác nhận",
                OrderDetails = detailList
            };

            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = dto.PaymentMethod == "Thanh toán online" ? "Đang xử lý" : "Chưa thanh toán",
                AmountPaid = totalPrice - order.DiscountAmount,
                PaymentDate = DateTime.Now
            };

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                await _orderRepo.AddAsync(order);
                await _context.Payments.AddAsync(payment);

                foreach (var item in dto.Items)
                {
                    await _productRepo.UpdateStockAsync(item.VariantId, -item.Quantity);

                    // Tăng SoldCount
                    var v = await _context.ProductVariants
                        .Include(x => x.Product)
                        .FirstAsync(x => x.VariantId == item.VariantId);
                    if (v.Product != null) v.Product.SoldCount += item.Quantity;
                }

                await _cartRepo.ClearCartAsync(userId);

                await _orderRepo.AddTrackingAsync(new OrderTracking
                {
                    OrderId = orderId,
                    StatusUpdate = "Đơn hàng đã đặt, đang chờ xác nhận",
                    UpdatedAt = DateTime.Now
                });

                // Cộng điểm
                int pointsEarned = (int)((totalPrice - order.DiscountAmount) / 10_000);
                if (pointsEarned > 0)
                {
                    customer.TotalPoints += pointsEarned;
                    UpdateMembershipLevel(customer);
                    await _context.PointHistories.AddAsync(new PointHistory
                    {
                        UserId = userId,
                        OrderId = orderId,
                        Points = pointsEarned,
                        Reason = $"Tích điểm từ đơn hàng {orderId}",
                        CreatedAt = DateTime.Now
                    });
                }

                await _customerRepo.UpdateAsync(customer);
                await _customerRepo.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResponse<OrderDto>.Fail($"Lỗi khi đặt hàng: {ex.Message}");
            }

            await _notifService.CreateAsync(userId, "order_update",
                "Đặt hàng thành công! 🎉",
                $"Đơn hàng #{orderId} đã được đặt. Chúng tôi sẽ xử lý sớm nhất.",
                orderId);

            var created = await _orderRepo.GetWithDetailsAsync(orderId);
            return ApiResponse<OrderDto>.Ok(MapToDto(created!), "Đặt hàng thành công");
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

        public async Task<ApiResponse<List<OrderDto>>> GetMyOrdersAsync(string userId)
        {
            var orders = await _orderRepo.GetByCustomerAsync(userId);
            return ApiResponse<List<OrderDto>>.Ok(orders.Select(MapToDto).ToList());
        }

        public async Task<ApiResponse<string>> CancelOrderAsync(string orderId, string userId)
        {
            var order = await _orderRepo.GetWithDetailsAsync(orderId);
            if (order == null) return ApiResponse<string>.Fail("Không tìm thấy đơn hàng");
            if (order.UserId != userId) return ApiResponse<string>.Fail("Không có quyền hủy đơn hàng này");
            if (!new[] { "Chờ xác nhận", "Đang chuẩn bị" }.Contains(order.Status))
                return ApiResponse<string>.Fail($"Không thể hủy khi đơn đang ở trạng thái '{order.Status}'");

            order.Status = "Hủy";
            foreach (var d in order.OrderDetails)
            {
                await _productRepo.UpdateStockAsync(d.VariantId, d.Quantity);
                var v = await _context.ProductVariants.Include(x => x.Product)
                    .FirstOrDefaultAsync(x => x.VariantId == d.VariantId);
                if (v?.Product != null)
                    v.Product.SoldCount = Math.Max(0, v.Product.SoldCount - d.Quantity);
            }

            // Hoàn điểm
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
                        Reason = $"Hoàn điểm do hủy đơn {orderId}",
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _orderRepo.AddTrackingAsync(new OrderTracking
            {
                OrderId = orderId,
                StatusUpdate = "Đơn hàng đã bị hủy bởi khách hàng",
                UpdatedAt = DateTime.Now
            });

            await _orderRepo.UpdateAsync(order);
            await _orderRepo.SaveChangesAsync();

            await _notifService.CreateAsync(userId, "order_update",
                "Đơn hàng đã hủy", $"Đơn #{orderId} đã hủy thành công.", orderId);

            return ApiResponse<string>.Ok("OK", "Hủy đơn hàng thành công");
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

        public async Task<ApiResponse<OrderCalculationDto>> CalculateOrderAsync(List<CartItemDto> items)
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
            TotalPrice = o.TotalPrice,
            DiscountAmount = o.DiscountAmount,
            FinalPrice = o.TotalPrice - o.DiscountAmount,
            ShippingMethod = o.ShippingMethod,
            PaymentMethod = o.PaymentMethod,
            Status = o.Status,
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
                ProductName = od.Variant?.Product?.Name,
                Color = od.Variant?.Color,
                Size = od.Variant?.Size,
                Quantity = od.Quantity,
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