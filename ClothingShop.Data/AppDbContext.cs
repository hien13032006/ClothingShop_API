using Microsoft.EntityFrameworkCore;
using ClothingShop.Models;

namespace ClothingShop.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // ── Core ──
        public DbSet<Customer> Customers { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<CartItem> Cart { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<OrderTracking> OrderTracking { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // ── Product (Đã quy hoạch: Bỏ DbSet Categories và ProductImages cũ) ──
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }

        // ── Promo / Shipping ──
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ShippingFee> ShippingFees { get; set; }

        // ── Membership ──
        public DbSet<PointHistory> PointHistories { get; set; }

        // ── Auth ──
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // ── Social ──
        public DbSet<WishlistItem> Wishlist { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder m)
        {
            // ── Customers ────────────────────────────────────────────
            m.Entity<Customer>(e =>
            {
                e.ToTable("Customers");
                e.HasKey(c => c.UserId);
                e.Property(c => c.UserId).HasColumnName("user_id").HasMaxLength(20);
                e.Property(c => c.FullName).HasColumnName("full_name").HasMaxLength(255);
                e.Property(c => c.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
                e.Property(c => c.Phone).HasColumnName("phone").HasMaxLength(15);
                e.Property(c => c.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
                e.Property(c => c.Provider).HasColumnName("provider").HasMaxLength(50).HasDefaultValue("Local");
                e.Property(c => c.MembershipLevel).HasColumnName("membership_level").HasMaxLength(50).HasDefaultValue("Bạc");
                e.Property(c => c.TotalPoints).HasColumnName("total_points").HasDefaultValue(0);
                e.Property(c => c.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.HasIndex(c => c.Email).IsUnique();
                e.HasIndex(c => c.Phone).IsUnique();
            });

            // ── Addresses ────────────────────────────────────────────
            m.Entity<Address>(e =>
            {
                e.ToTable("Addresses");
                e.HasKey(a => a.AddressId);
                e.Property(a => a.AddressId).HasColumnName("address_id").ValueGeneratedOnAdd();
                e.Property(a => a.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(a => a.ReceiverName).HasColumnName("receiver_name").HasMaxLength(255).IsRequired();
                e.Property(a => a.ReceiverPhone).HasColumnName("receiver_phone").HasMaxLength(15).IsRequired();
                e.Property(a => a.AddressDetail).HasColumnName("address_detail").IsRequired();
                e.Property(a => a.AddressType).HasColumnName("address_type").HasMaxLength(50).HasDefaultValue("Nhà riêng");
                e.Property(a => a.IsDefault).HasColumnName("is_default").HasDefaultValue(false);
                e.HasOne(a => a.Customer).WithMany(c => c.Addresses)
                 .HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Cart ──────────────────────────────────────────────────
            m.Entity<CartItem>(e =>
            {
                e.ToTable("Cart");
                e.HasKey(ci => ci.Id);
                e.Property(ci => ci.Id).HasColumnName("cart_id").ValueGeneratedOnAdd();
                e.Property(ci => ci.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(ci => ci.VariantId).HasColumnName("variant_id").IsRequired();
                e.Property(ci => ci.Quantity).HasColumnName("quantity").IsRequired();
                e.Property(ci => ci.AddedAt).HasColumnName("added_at").HasDefaultValueSql("GETDATE()");
                e.HasOne(ci => ci.Customer).WithMany(c => c.CartItems)
                 .HasForeignKey(ci => ci.UserId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(ci => ci.Variant).WithMany()
                 .HasForeignKey(ci => ci.VariantId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Orders ────────────────────────────────────────────────
            m.Entity<Order>(e =>
            {
                e.ToTable("Orders");
                e.HasKey(o => o.OrderId);
                e.Property(o => o.OrderId).HasColumnName("order_id").HasMaxLength(20);
                e.Property(o => o.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(o => o.OrderDate).HasColumnName("order_date").HasDefaultValueSql("GETDATE()");
                e.Property(o => o.TotalPrice).HasColumnName("total_price").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(o => o.DiscountAmount).HasColumnName("discount_amount").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(o => o.FinalPrice).HasColumnName("final_price").HasColumnType("decimal(18,2)").ValueGeneratedOnAddOrUpdate();
                e.Property(o => o.ShippingMethod).HasColumnName("shipping_method").HasMaxLength(100);
                e.Property(o => o.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100);
                e.Property(o => o.Status).HasColumnName("status").HasMaxLength(50).HasDefaultValue("Chờ xác nhận");
                e.HasOne(o => o.Customer).WithMany(c => c.Orders)
                 .HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── OrderDetails ──────────────────────────────────────────
            m.Entity<OrderDetail>(e =>
            {
                e.ToTable("OrderDetails");
                e.HasKey(od => od.OrderDetailId);
                e.Property(od => od.OrderDetailId).HasColumnName("order_detail_id").ValueGeneratedOnAdd();
                e.Property(od => od.OrderId).HasColumnName("order_id").HasMaxLength(20).IsRequired();
                e.Property(od => od.VariantId).HasColumnName("variant_id").IsRequired();
                e.Property(od => od.Quantity).HasColumnName("quantity").IsRequired();
                e.Property(od => od.UnitPrice).HasColumnName("unit_price").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(od => od.LineTotal).HasColumnName("line_total").HasColumnType("decimal(18,2)").ValueGeneratedOnAddOrUpdate();
                e.HasOne(od => od.Order).WithMany(o => o.OrderDetails)
                 .HasForeignKey(od => od.OrderId).OnDelete(DeleteBehavior.Cascade);
                e.HasOne(od => od.Variant).WithMany()
                 .HasForeignKey(od => od.VariantId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── OrderTracking ─────────────────────────────────────────
            m.Entity<OrderTracking>(e =>
            {
                e.ToTable("OrderTracking");
                e.HasKey(ot => ot.TrackingId);
                e.Property(ot => ot.TrackingId).HasColumnName("tracking_id").ValueGeneratedOnAdd();
                e.Property(ot => ot.OrderId).HasColumnName("order_id").HasMaxLength(20).IsRequired();
                e.Property(ot => ot.StatusUpdate).HasColumnName("status_update").HasMaxLength(255);
                e.Property(ot => ot.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("GETDATE()");
                e.Property(ot => ot.LocationLatLong).HasColumnName("location_lat_long").HasMaxLength(100);
                e.HasOne(ot => ot.Order).WithMany(o => o.Trackings)
                 .HasForeignKey(ot => ot.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Payments ──────────────────────────────────────────────
            m.Entity<Payment>(e =>
            {
                e.ToTable("Payments");
                e.HasKey(p => p.PaymentId);
                e.Property(p => p.PaymentId).HasColumnName("payment_id").ValueGeneratedOnAdd();
                e.Property(p => p.OrderId).HasColumnName("order_id").HasMaxLength(20).IsRequired();
                e.Property(p => p.PaymentMethod).HasColumnName("payment_method").HasMaxLength(100).HasDefaultValue("Thanh toán khi nhận hàng");
                e.Property(p => p.PaymentStatus).HasColumnName("payment_status").HasMaxLength(50).HasDefaultValue("Chưa thanh toán");
                e.Property(p => p.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
                e.Property(p => p.PaymentDate).HasColumnName("payment_date").HasDefaultValueSql("GETDATE()");
                e.Property(p => p.AmountPaid).HasColumnName("amount_paid").HasColumnType("decimal(18,2)").IsRequired();
                e.HasOne(p => p.Order).WithOne(o => o.Payment)
                 .HasForeignKey<Payment>(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Products (Cập nhật chuẩn theo cấu trúc thuộc tính mới trong Models) ──
            m.Entity<Product>(e =>
            {
                e.ToTable("Products");
                e.HasKey(p => p.ProductId); // Đặt chính xác ProductId làm khóa chính
                e.Property(p => p.ProductId).HasColumnName("product_id").ValueGeneratedOnAdd();
                e.Property(p => p.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
                e.Property(p => p.Description).HasColumnName("description");
                e.Property(p => p.Price).HasColumnName("price").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(p => p.OldPrice).HasColumnName("old_price").HasColumnType("decimal(18,2)");
                e.Property(p => p.Discount).HasColumnName("discount").HasDefaultValue(0); // Kiểu int? theo đúng Model
                e.Property(p => p.MainImage).HasColumnName("main_image").HasMaxLength(500);

                // Thuộc tính Thumbnails là một List<string>, EF Core không tự lưu trực tiếp xuống một cột đơn được.
                // Chúng ta sẽ cấu hình nó tự động chuyển đổi sang chuỗi JSON khi lưu và giải nén lại khi đọc.
                e.Property(p => p.Thumbnails)
                 .HasColumnName("thumbnails")
                 .HasConversion(
                     v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null),
                     v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions)null) ?? new List<string>()
                 );

                e.Property(p => p.Gender).HasColumnName("gender").HasMaxLength(50);
                e.Property(p => p.Category).HasColumnName("category").HasMaxLength(100);
                e.Property(p => p.SoldCount).HasColumnName("sold_count").HasDefaultValue(0);
                e.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
            });

            // ── ProductVariants ───────────────────────────────────────
            m.Entity<ProductVariant>(e =>
            {
                e.ToTable("ProductVariants");
                e.HasKey(v => v.VariantId);
                e.Property(v => v.VariantId).HasColumnName("variant_id").ValueGeneratedOnAdd();
                e.Property(v => v.ProductId).HasColumnName("product_id").IsRequired();
                e.Property(v => v.Color).HasColumnName("color").HasMaxLength(50);
                e.Property(v => v.Size).HasColumnName("size").HasMaxLength(20);
                e.Property(v => v.StockQuantity).HasColumnName("stock_quantity").HasDefaultValue(0);

                // ĐÃ XÓA DÒNG e.Property(v => v.PriceAdjustment) VÀ e.Ignore(v => v.ActualPrice) VÌ MODEL KHÔNG CÓ

                // Mối quan hệ: Một sản phẩm có nhiều biến thể
                e.HasOne(v => v.Product).WithMany(p => p.Variants)
                 .HasForeignKey(v => v.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── ProductReviews ────────────────────────────────────────
            m.Entity<ProductReview>(e =>
            {
                e.ToTable("ProductReviews");
                e.HasKey(r => r.ReviewId);
                e.Property(r => r.ReviewId).HasColumnName("review_id").ValueGeneratedOnAdd();
                e.Property(r => r.ProductId).HasColumnName("product_id").IsRequired();

                // Cấu hình thêm 2 cột mới
                e.Property(r => r.UserId).HasColumnName("user_id").HasMaxLength(450).IsRequired();
                e.Property(r => r.OrderId).HasColumnName("order_id").HasMaxLength(50).IsRequired();

                e.Property(r => r.CustomerName).HasColumnName("customer_name").HasMaxLength(255).IsRequired();
                e.Property(r => r.Rating).HasColumnName("rating").IsRequired();
                e.Property(r => r.Comment).HasColumnName("comment");
                e.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");

                // Đảm bảo tính logic: Một đơn hàng ứng với một sản phẩm cụ thể chỉ được đánh giá 1 lần duy nhất (Chống spam)
                e.HasIndex(r => new { r.OrderId, r.ProductId }).IsUnique();

                // Mối quan hệ với thực thể Product
                e.HasOne(r => r.Product).WithMany(p => p.Reviews)
                 .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
            });
            // ── Promotions ────────────────────────────────────────────
            m.Entity<Promotion>(e =>
            {
                e.ToTable("Promotions");
                e.HasKey(p => p.PromotionId);
                e.Property(p => p.PromotionId).HasColumnName("promotion_id").ValueGeneratedOnAdd();
                e.Property(p => p.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
                e.Property(p => p.DiscountType).HasColumnName("discount_type").HasMaxLength(20).HasDefaultValue("Percent");
                e.Property(p => p.DiscountValue).HasColumnName("discount_value").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(p => p.MinOrderAmount).HasColumnName("min_order_amount").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(p => p.StartDate).HasColumnName("start_date");
                e.Property(p => p.EndDate).HasColumnName("end_date");
                e.Property(p => p.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                e.HasIndex(p => p.Code).IsUnique();
            });

            // ── ShippingFees ──────────────────────────────────────────
            m.Entity<ShippingFee>(e =>
            {
                e.ToTable("ShippingFees");
                e.HasKey(s => s.Id);
                e.Property(s => s.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(s => s.Method).HasColumnName("method").HasMaxLength(100).IsRequired();
                e.Property(s => s.Fee).HasColumnName("fee").HasColumnType("decimal(18,2)").IsRequired();
                e.Property(s => s.FreeShippingFrom).HasColumnName("free_shipping_from").HasColumnType("decimal(18,2)").HasDefaultValue(0);
                e.Property(s => s.EstimatedDays).HasColumnName("estimated_days").HasMaxLength(50);
                e.HasIndex(s => s.Method).IsUnique();
            });

            // ── PointHistories ────────────────────────────────────────
            m.Entity<PointHistory>(e =>
            {
                e.ToTable("PointHistories");
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(p => p.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(p => p.OrderId).HasColumnName("order_id").HasMaxLength(20);
                e.Property(p => p.Points).HasColumnName("points").IsRequired();
                e.Property(p => p.Reason).HasColumnName("reason").HasMaxLength(255);
                e.Property(p => p.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.HasOne(p => p.Customer).WithMany()
                 .HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── PasswordResetTokens ───────────────────────────────────
            m.Entity<PasswordResetToken>(e =>
            {
                e.ToTable("PasswordResetTokens");
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(t => t.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(t => t.Token).HasColumnName("token").HasMaxLength(100).IsRequired();
                e.Property(t => t.ExpiresAt).HasColumnName("expires_at").IsRequired();
                e.Property(t => t.IsUsed).HasColumnName("is_used").HasDefaultValue(false);
                e.HasOne(t => t.Customer).WithMany()
                 .HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── RefreshTokens ─────────────────────────────────────────
            m.Entity<RefreshToken>(e =>
            {
                e.ToTable("RefreshTokens");
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).HasColumnName("id").ValueGeneratedOnAdd();
                e.Property(r => r.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(r => r.Token).HasColumnName("token").HasMaxLength(200).IsRequired();
                e.Property(r => r.ExpiresAt).HasColumnName("expires_at").IsRequired();
                e.Property(r => r.IsRevoked).HasColumnName("is_revoked").HasDefaultValue(false);
                e.Property(r => r.ReplacedByToken).HasColumnName("replaced_by_token").HasMaxLength(200);
                e.Property(r => r.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.HasIndex(r => r.Token).IsUnique();
                e.HasOne(r => r.Customer).WithMany()
                 .HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Wishlist ──────────────────────────────────────────────
            m.Entity<WishlistItem>(e =>
            {
                e.ToTable("Wishlist");
                e.HasKey(w => w.WishlistId);
                e.Property(w => w.WishlistId).HasColumnName("wishlist_id").ValueGeneratedOnAdd();
                e.Property(w => w.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(w => w.ProductId).HasColumnName("product_id").IsRequired();
                e.Property(w => w.AddedAt).HasColumnName("added_at").HasDefaultValueSql("GETDATE()");

                e.HasIndex(w => new { w.UserId, w.ProductId }).IsUnique();
                e.HasOne(w => w.Customer).WithMany()
                 .HasForeignKey(w => w.UserId).OnDelete(DeleteBehavior.Cascade);

                // Khớp liên kết khóa ngoại với Product.Id mới
                e.HasOne(w => w.Product).WithMany()
                 .HasForeignKey(w => w.ProductId).OnDelete(DeleteBehavior.Cascade);
            });

            // ── Notifications ─────────────────────────────────────────
            m.Entity<Notification>(e =>
            {
                e.ToTable("Notifications");
                e.HasKey(n => n.NotificationId);
                e.Property(n => n.NotificationId).HasColumnName("notification_id").ValueGeneratedOnAdd();
                e.Property(n => n.UserId).HasColumnName("user_id").HasMaxLength(20).IsRequired();
                e.Property(n => n.Type).HasColumnName("type").HasMaxLength(50).HasDefaultValue("system");
                e.Property(n => n.Title).HasColumnName("title").HasMaxLength(255).IsRequired();
                e.Property(n => n.Body).HasColumnName("body").IsRequired();
                e.Property(n => n.RelatedId).HasColumnName("related_id").HasMaxLength(50);
                e.Property(n => n.IsRead).HasColumnName("is_read").HasDefaultValue(false);
                e.Property(n => n.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("GETDATE()");
                e.HasOne(n => n.Customer).WithMany()
                 .HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}