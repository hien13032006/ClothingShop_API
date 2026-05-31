using ClothingShop.Data;
using Microsoft.EntityFrameworkCore;

namespace ClothingShop.API.Infrastructure
{
    public static class SeedDataService
    {
        public static async Task SeedAsync(WebApplication app)
        {
            // Minimal seeding: ensure database can be created/migrated if provider supports it.
            try
            {
                using var scope = app.Services.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await ctx.Database.EnsureCreatedAsync();
            }
            catch
            {
                // Ignore seeding errors in this minimal implementation
            }
        }
    }
}
