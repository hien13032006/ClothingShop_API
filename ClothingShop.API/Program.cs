using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ClothingShop.API.Infrastructure;
using ClothingShop.API.Middleware;
using ClothingShop.Business.Services;
using ClothingShop.Data;
using ClothingShop.Data.Interfaces;
using ClothingShop.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ── 1. Database ───────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

// ── 2. JWT Authentication ─────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key chưa cấu hình trong appsettings.json");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };
        o.Events = new JwtBearerEvents
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode  = 401;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Bạn chưa đăng nhập hoặc token đã hết hạn\"}");
            },
            OnForbidden = ctx =>
            {
                ctx.Response.StatusCode  = 403;
                ctx.Response.ContentType = "application/json";
                return ctx.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Bạn không có quyền thực hiện thao tác này\"}");
            }
        };
    });

builder.Services.AddAuthorization();

// ── 3. CORS ────────────────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("AllowAll", p =>
        p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ── 4. Repositories ────────────────────────────────────────────────
builder.Services.AddScoped<ICustomerRepository,  CustomerRepository>();
builder.Services.AddScoped<IAddressRepository,   AddressRepository>();
builder.Services.AddScoped<IProductRepository,   ProductRepository>();
builder.Services.AddScoped<IOrderRepository,     OrderRepository>();
builder.Services.AddScoped<ICartRepository,      CartRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IReviewRepository,    ReviewRepository>();

// ── 5. Services ────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService,          AuthService>();
builder.Services.AddScoped<ICustomerService,      CustomerService>();
builder.Services.AddScoped<IProductService,       ProductService>();
builder.Services.AddScoped<ICartService,          CartService>();
builder.Services.AddScoped<IOrderService,         OrderService>();
builder.Services.AddScoped<IPromotionService,     PromotionService>();
builder.Services.AddScoped<IPaymentService,       PaymentService>();
builder.Services.AddScoped<IReviewService,        ReviewService>();
builder.Services.AddScoped<IStatisticsService,    StatisticsService>();
builder.Services.AddScoped<IWishlistService,      WishlistService>();
builder.Services.AddScoped<IMembershipService,    MembershipService>();
builder.Services.AddScoped<IForgotPasswordService,ForgotPasswordService>();
builder.Services.AddScoped<IEmailService,         EmailService>();
builder.Services.AddScoped<IShippingService,      ShippingService>();
builder.Services.AddScoped<IExportService,        ExportService>();
builder.Services.AddScoped<IVariantService,       VariantService>();
builder.Services.AddScoped<INotificationService,  NotificationService>();

// ── 6. Static Files (wwwroot cho ảnh upload) ───────────────────────
// (No explicit registration needed for IWebHostEnvironment)

// ── 7. Controllers + Validation Response ──────────────────────────
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var errors = ctx.ModelState
                .Where(x => x.Value?.Errors.Any() == true)
                .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                .ToList();

            return new Microsoft.AspNetCore.Mvc.ObjectResult(new
            {
                success = false,
                message = "Dữ liệu đầu vào không hợp lệ",
                errors
            })
            { StatusCode = 422 };
        };
    });

// ── 8. Swagger + JWT ──────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "ClothingShop API",
        Version     = "v1",
        Description = "Backend TMĐT bán quần áo — .NET 10.0"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Nhập: Bearer {jwt_token}",
        Name        = "Authorization",
        In          = ParameterLocation.Header,
        Type        = SecuritySchemeType.ApiKey,
        Scheme      = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        },
        Array.Empty<string>()
    }});
});

// ── 9. Logging ────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Build & Pipeline ──────────────────────────────────────────────
var app = builder.Build();

app.UseGlobalExceptionHandler();   // 1. Bắt mọi exception chưa xử lý
app.UseRateLimiting();             // 2. Rate limiting (chống abuse)

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ClothingShop API v1");
       // c.RoutePrefix = string.Empty;  // Swagger tại http://localhost:PORT/
    });
}

app.UseStaticFiles();              // 3. Phục vụ ảnh từ wwwroot/
app.UseCors("AllowAll");           // 4. CORS
app.UseHttpsRedirection();         // 5. HTTPS
app.UseAuthentication();           // 6. Xác thực JWT — PHẢI trước Authorization
app.UseAuthorization();            // 7. Phân quyền
app.MapControllers();              // 8. Route đến Controllers

await SeedDataService.SeedAsync(app);  // Seed DB lần đầu

app.Run();
