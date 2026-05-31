using System.Threading.Tasks;
using ClothingShop.Models;

namespace ClothingShop.Data.Interfaces
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        Task<bool> EmailExistsAsync(string email);
        Task<bool> PhoneExistsAsync(string phone);
        Task<string> GenerateNextUserIdAsync();
        Task<Customer?> GetByEmailOrPhoneAsync(string emailOrPhone);
        Task<Customer?> GetByEmailAsync(string email);

        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Customer customer);
        Task<int> CountAsync();
        Task<IEnumerable<Customer>> GetAllWithPagingAsync(int page, int pageSize);
        Task SaveChangesAsync();
    }
}
