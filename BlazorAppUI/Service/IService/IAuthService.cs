using BlazorAppUI.Models;

namespace BlazorAppUI.Service.IService
{
    public interface IAuthService
    {
        Task<ResponseDto?> LoginAsync(LoginRequestDto loginRequestDto);
        Task<ResponseDto?> RegisterAsync(RegisterationRequestDto registrationRequestDto);
        Task<ResponseDto?> AssignRoleAsync(RegisterationRequestDto registrationRequestDto);
    }
}
