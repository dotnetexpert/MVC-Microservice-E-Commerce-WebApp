//using BlazorAppUI.Models;
//using BlazorAppUI.Service.IService;
//using Microsoft.AspNetCore.Authentication.Cookies;
//using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Components;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Newtonsoft.Json;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;

//namespace BlazorAppUI.Pages.Auth
//{
//    public class LoginModel : PageModel
//    {
//        [BindProperty]
//        public LoginRequestDto user  {  get; set; } = new LoginRequestDto();
//        private IAuthService _authService;
//        private ITokenProvider  _tokenProvider;
//        private NavigationManager navigationManager;
//        public void OnGet()
//        {
//        }
//        [HttpPost]
//        public async Task Login(LoginRequestDto obj)
//        {
//            ResponseDto responseDto = await _authService.LoginAsync(obj);

//            if (responseDto != null && responseDto.IsSuccess)
//            {
//                LoginResponseDto loginResponseDto =
//                    JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result));

//                await SignInUser(loginResponseDto);
//                _tokenProvider.SetToken(loginResponseDto.Token);
//                navigationManager.NavigateTo("/");
//            }
           
//        }
//        private async Task SignInUser(LoginResponseDto model)
//        {
//            var handler = new JwtSecurityTokenHandler();
//            var jwt = handler.ReadJwtToken(model.Token);
//            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
//            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email,
//                jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email).Value));
//            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub,
//                jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value));
//            identity.AddClaim(new Claim(JwtRegisteredClaimNames.Name,
//                jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Name).Value));
//            identity.AddClaim(new Claim(ClaimTypes.Name,
//                jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email).Value));
//            identity.AddClaim(new Claim(ClaimTypes.Role,
//                jwt.Claims.FirstOrDefault(u => u.Type == "role").Value));
//            var principal = new ClaimsPrincipal(identity);
//            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
//        }
//    }
//}
