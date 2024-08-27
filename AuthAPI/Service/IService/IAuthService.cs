using AuthAPI.Models.Dto;

namespace AuthAPI.Service.IService
{
    public interface IAuthService
    {
        Task<string> Register(RegisterationRequestDto registrationRequestDto);
        Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto);
        Task<bool> AssignRole(string email, string roleName);
        Task<bool> IsEmailAvailable(string email);
        Task<UserDto> GetUser(string Id);
        Task<List<UserDto>> GetAllUsers();
        Task<string> UpdateUser(UserDto userDto);
    }
}
