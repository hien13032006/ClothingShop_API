using ClothingShop.Data.Interfaces;
using ClothingShop.Models;
using ClothingShop.Models.DTOs;

namespace ClothingShop.Business.Services
{
    public interface ICustomerService
    {
        Task<ApiResponse<CustomerDto>> GetProfileAsync(string userId);
        Task<ApiResponse<CustomerDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto);
        Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId);
        Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto);
        Task<ApiResponse<string>> DeleteAddressAsync(string userId, int addressId);
        Task<ApiResponse<string>> SetDefaultAddressAsync(string userId, int addressId);
        Task<ApiResponse<PagedResult<CustomerDto>>> GetAllCustomersAsync(int page, int pageSize);
        Task<ApiResponse<string>> DeleteCustomerAsync(string userId);
    }

    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;
        private readonly IAddressRepository _addressRepo;
        private readonly IAuthService _authService;

        public CustomerService(
            ICustomerRepository customerRepo,
            IAddressRepository addressRepo,
            IAuthService authService)
        {
            _customerRepo = customerRepo;
            _addressRepo = addressRepo;
            _authService = authService;
        }

        public async Task<ApiResponse<CustomerDto>> GetProfileAsync(string userId)
        {
            var c = await _customerRepo.GetByIdAsync(userId);
            return c == null
                ? ApiResponse<CustomerDto>.Fail("Không tìm thấy khách hàng")
                : ApiResponse<CustomerDto>.Ok(MapToDto(c));
        }

        public async Task<ApiResponse<CustomerDto>> UpdateProfileAsync(string userId, UpdateProfileDto dto)
        {
            var c = await _customerRepo.GetByIdAsync(userId);
            if (c == null) return ApiResponse<CustomerDto>.Fail("Không tìm thấy khách hàng");

            if (!string.IsNullOrWhiteSpace(dto.Phone) && dto.Phone != c.Phone)
            {
                if (await _customerRepo.PhoneExistsAsync(dto.Phone))
                    return ApiResponse<CustomerDto>.Fail("Số điện thoại đã được dùng bởi tài khoản khác");
                c.Phone = dto.Phone.Trim();
            }
            if (!string.IsNullOrWhiteSpace(dto.FullName))
                c.FullName = dto.FullName.Trim();

            await _customerRepo.UpdateAsync(c);
            await _customerRepo.SaveChangesAsync();
            return ApiResponse<CustomerDto>.Ok(MapToDto(c), "Cập nhật hồ sơ thành công");
        }

        public async Task<ApiResponse<string>> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var c = await _customerRepo.GetByIdAsync(userId);
            if (c == null) return ApiResponse<string>.Fail("Không tìm thấy khách hàng");
            if (!_authService.VerifyPassword(dto.CurrentPassword, c.Password))
                return ApiResponse<string>.Fail("Mật khẩu hiện tại không đúng");
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return ApiResponse<string>.Fail("Mật khẩu xác nhận không khớp");
            if (dto.NewPassword.Length < 8)
                return ApiResponse<string>.Fail("Mật khẩu mới phải có ít nhất 8 ký tự");

            c.Password = _authService.HashPassword(dto.NewPassword);
            await _customerRepo.UpdateAsync(c);
            await _customerRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đổi mật khẩu thành công");
        }

        public async Task<ApiResponse<List<AddressDto>>> GetAddressesAsync(string userId)
        {
            var list = await _addressRepo.GetByUserAsync(userId);
            return ApiResponse<List<AddressDto>>.Ok(list.Select(MapAddressToDto).ToList());
        }

        public async Task<ApiResponse<AddressDto>> AddAddressAsync(string userId, CreateAddressDto dto)
        {
            if (dto.IsDefault)
                await _addressRepo.ClearDefaultAsync(userId);

            var address = new Address
            {
                UserId = userId,
                ReceiverName = dto.ReceiverName.Trim(),
                ReceiverPhone = dto.ReceiverPhone.Trim(),
                AddressDetail = dto.AddressDetail.Trim(),
                AddressType = dto.AddressType,
                IsDefault = dto.IsDefault
            };

            await _addressRepo.AddAsync(address);
            await _addressRepo.SaveChangesAsync();
            return ApiResponse<AddressDto>.Ok(MapAddressToDto(address), "Thêm địa chỉ thành công");
        }

        public async Task<ApiResponse<string>> DeleteAddressAsync(string userId, int addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                return ApiResponse<string>.Fail("Địa chỉ không tồn tại hoặc không có quyền xóa");

            await _addressRepo.DeleteAsync(address);
            await _addressRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Xóa địa chỉ thành công");
        }

        public async Task<ApiResponse<string>> SetDefaultAddressAsync(string userId, int addressId)
        {
            var address = await _addressRepo.GetByIdAsync(addressId);
            if (address == null || address.UserId != userId)
                return ApiResponse<string>.Fail("Không tìm thấy địa chỉ");

            await _addressRepo.ClearDefaultAsync(userId);
            address.IsDefault = true;
            await _addressRepo.UpdateAsync(address);
            await _addressRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đặt địa chỉ mặc định thành công");
        }

        public async Task<ApiResponse<PagedResult<CustomerDto>>> GetAllCustomersAsync(int page, int pageSize)
        {
            var total = await _customerRepo.CountAsync();
            var items = await _customerRepo.GetAllWithPagingAsync(page, pageSize);
            return ApiResponse<PagedResult<CustomerDto>>.Ok(new PagedResult<CustomerDto>
            {
                Items = items.Select(MapToDto).ToList(),
                TotalCount = total,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ApiResponse<string>> DeleteCustomerAsync(string userId)
        {
            var c = await _customerRepo.GetByIdAsync(userId);
            if (c == null) return ApiResponse<string>.Fail("Không tìm thấy khách hàng");
            await _customerRepo.DeleteAsync(c);
            await _customerRepo.SaveChangesAsync();
            return ApiResponse<string>.Ok("OK", "Đã xóa khách hàng");
        }

        private static CustomerDto MapToDto(Customer c) => new()
        {
            UserId = c.UserId,
            FullName = c.FullName,
            Email = c.Email,
            Phone = c.Phone,
            MembershipLevel = c.MembershipLevel,
            TotalPoints = c.TotalPoints,
            CreatedAt = c.CreatedAt
        };

        private static AddressDto MapAddressToDto(Address a) => new()
        {
            AddressId = a.AddressId,
            ReceiverName = a.ReceiverName,
            ReceiverPhone = a.ReceiverPhone,
            AddressDetail = a.AddressDetail,
            AddressType = a.AddressType,
            IsDefault = a.IsDefault
        };
    }
}
