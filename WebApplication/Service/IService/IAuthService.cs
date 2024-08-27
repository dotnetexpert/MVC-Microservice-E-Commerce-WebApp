using WebApplicationUI.Models;

namespace WebApplicationUI.Service.IService
{
    public interface IAuthService
    {
        Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> RegisterAsync(RegisterationRequestDto registrationRequestDto);
        Task<ResponseDto?> AssignRoleAsync(RegisterationRequestDto registrationRequestDto);
        Task<ResponseDto?> EmailExist(string email);
        Task<ResponseDto?> GetUser(string Id);
        Task<ResponseDto?> ImpersonateUser(string ID);
        Task<ResponseDto?> GetAllUsers();
        Task<ResponseDto?> UpdateUser(UserDto userDto);
   
    }
}
