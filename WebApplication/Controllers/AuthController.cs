using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplicationUI.Models;
using WebApplicationUI.Service.IService;
using WebApplicationUI.Utility;
namespace WebApplicationUI.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ITokenProvider _tokenProvider;

        public AuthController(IAuthService authService, ITokenProvider tokenProvider)
        {
            _authService = authService;
            _tokenProvider = tokenProvider;
        }
        [HttpGet]
        public IActionResult Login()
        {
            try
            {
                LoginRequestDto loginRequestDto = new();
                return View(loginRequestDto);
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();

        }
        [HttpPost]
        public async Task<IActionResult> Login(LoginRequestDto obj)
        {
            try
            {
                if (obj == null)
                {
                    return View();
                }
                ResponseDto responseDto = await _authService.LoginAsync(obj);

                if (responseDto != null && responseDto.IsSuccess)
                {
                    LoginResponseDto loginResponseDto = JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result));
                    await SignInUser(loginResponseDto);
                    _tokenProvider.SetToken(loginResponseDto.Token);
                    if (loginResponseDto.User.ID=="fb458731-b7c6-42e9-a6b6-19e72bb55319")
                    {
                        HttpContext.Session.SetString("Token", loginResponseDto.Token);
                    }
                    TempData["success"] = "Login successfully ";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["error"] = responseDto.Message;
                    return View(obj);
                }
            }
            catch (Exception ex)
            {

                TempData["error"] = ex;
            }
            return View();

        }
        [HttpGet]
        public IActionResult Register()
        {
            try
            {
                var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin,Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer,Value=SD.RoleCustomer},
            };
                ViewBag.RoleList = roleList;
                return View();
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View(null);
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterationRequestDto obj)
        {
            try
            {
                obj.Role="CUSTOMER";
                ResponseDto result = await _authService.RegisterAsync(obj);
                ResponseDto assingRole;

                if (result!=null && result.IsSuccess)
                {
                    if (string.IsNullOrEmpty(obj.Role))
                    {
                        obj.Role = SD.RoleCustomer;
                    }
                    assingRole = await _authService.AssignRoleAsync(obj);
                    if (assingRole!=null && assingRole.IsSuccess)
                    {
                        TempData["success"] = "Registration Successful";
                        return RedirectToAction(nameof(Login));
                    }
                }
                else
                {
                    TempData["error"] = result.Message;
                }

                var roleList = new List<SelectListItem>()
            {
                new SelectListItem{Text=SD.RoleAdmin,Value=SD.RoleAdmin},
                new SelectListItem{Text=SD.RoleCustomer,Value=SD.RoleCustomer},
            };
                ViewBag.RoleList = roleList;
                TempData["success"] = "Register successfully ";
                return View(obj);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return View();

        }
        public async Task<IActionResult> Logout()
        {
            try
            {
                byte[] tokenData = HttpContext.Session.Get("Token");
                if (tokenData == null)
                {
                    await HttpContext.SignOutAsync();
                    _tokenProvider.ClearToken();
                    HttpContext.Session.Remove("Token");
                    TempData["success"] = "Logged out successfully";
                    return RedirectToAction("Index", "Home");
                }

                string token = Encoding.UTF8.GetString(tokenData);

                if (!string.IsNullOrEmpty(token))
                {
                    ResponseDto response = await _authService.ImpersonateUser("fb458731-b7c6-42e9-a6b6-19e72bb55319");
                    if (response != null && response.IsSuccess)
                    { 
                        LoginResponseDto loginResponse = JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(response.Result));
                        await SignInUser(loginResponse);
                        _tokenProvider.SetToken(loginResponse.Token);
                        HttpContext.Session.Remove("Token");
                        TempData["success"] = "Login successful";
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        TempData["error"] = "Failed to impersonate user";
                    }
                }
                else
                {
                    await HttpContext.SignOutAsync();
                    _tokenProvider.ClearToken();
                    HttpContext.Session.Remove("Token");
                    TempData["success"] = "Logged out successfully";
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred: " + ex.Message;
            }

            return View();
        }

        private async Task SignInUser(LoginResponseDto model)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(model.Token);
                var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Email,
                    jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email).Value));
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub,
                    jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub).Value));
                identity.AddClaim(new Claim(JwtRegisteredClaimNames.Name,
                    jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Name).Value));
                identity.AddClaim(new Claim(ClaimTypes.Name,
                    jwt.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Email).Value));
                identity.AddClaim(new Claim(ClaimTypes.Role,
                    jwt.Claims.FirstOrDefault(u => u.Type == "role").Value));
                var principal = new ClaimsPrincipal(identity);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
        }
        [HttpGet]
        public async Task<IActionResult> EmailExist(string email)
        {
            try
            {
                ResponseDto responseDto = await _authService.EmailExist(email);
                return new JsonResult(responseDto);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return Json(TempData);

        }
        [Authorize]
        [HttpGet]
        [ActionName("GetAllUser")]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {
                List<UserDto>? list = new();
                ResponseDto? response = await _authService.GetAllUsers();
                if (response != null && response.IsSuccess)
                {
                    list= JsonConvert.DeserializeObject<List<UserDto>>(Convert.ToString(response.Result));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return View(list);
            }
            catch (Exception ex)
            {
                TempData["error"] = ex;
            }
            return NoContent();
        }
        //[HttpPost]
        //[ActionName("ImpersonateUser")]
        //public async Task<IActionResult> ImpersonateUser (string ID)
        //{
        //    ResponseDto responseDto = await _authService.ImpersonateUser(ID);
        //    if (responseDto != null && responseDto.IsSuccess)
        //    {
        //        LoginResponseDto loginResponseDto = JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result));

        //        await SignInUser(loginResponseDto);
        //        _tokenProvider.SetToken(loginResponseDto.Token);
        //        TempData["success"] = "Login successfully ";
        //        return RedirectToAction("Index", "Home");
        //    }

        //    return NoContent() ;

        //}
        [HttpPost]
        [ActionName("ImpersonateUser")]
        public async Task<IActionResult> ImpersonateUser(string ID)
        {

            ResponseDto responseDto = await _authService.ImpersonateUser(ID);
            if (responseDto != null && responseDto.IsSuccess)
            {
                LoginResponseDto loginResponseDto = JsonConvert.DeserializeObject<LoginResponseDto>(Convert.ToString(responseDto.Result));
                // await Logout();
                HttpContext.Session.SetString("Token", loginResponseDto.Token);
                await SignInUser(loginResponseDto);
                _tokenProvider.SetToken(loginResponseDto.Token);
                TempData["success"] = "Login successfully";
                return RedirectToAction("Index", "Home");
            }
            return NoContent();
        }

    }

}

