using Microsoft.EntityFrameworkCore;
using ClothingShop.Data.Interfaces;
using ClothingShop.Models;

namespace ClothingShop.Data.Repositories
{
    public class CustomerRepository : BaseRepository<Customer>, ICustomerRepository
    {
        public CustomerRepository(AppDbContext context) : base(context) { }

        public async Task<bool> EmailExistsAsync(string email)
            => await _context.Customers.AnyAsync(c => c.Email == email);

        public async Task<bool> PhoneExistsAsync(string phone)
            => await _context.Customers.AnyAsync(c => c.Phone == phone);

        public async Task<string> GenerateNextUserIdAsync()
        {
            // Simple generation: KH0001, KH0002 or AD0001 for admin not needed here
            var count = await _context.Customers.CountAsync();
            return $"KH{(count + 1):D4}";
        }

        public async Task<Customer?> GetByEmailOrPhoneAsync(string emailOrPhone)
            => await _context.Customers.FirstOrDefaultAsync(c => c.Email == emailOrPhone || c.Phone == emailOrPhone);

        public async Task<Customer?> GetByEmailAsync(string email)
            => await _context.Customers.FirstOrDefaultAsync(c => c.Email == email);
    }
}
