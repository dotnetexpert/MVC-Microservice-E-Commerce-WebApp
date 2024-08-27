using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using WebApplicationUI.Models;
using WebApplicationUI.Service.IService;

namespace WebApplicationUI.Controllers
{
    public class ProfileController : Controller
    {
        private readonly IAuthService _authService;

        public ProfileController(IAuthService authService)
        {
            _authService = authService;
        }
        public async Task<ActionResult<UserDto>> Index()
        {
            try
            {
                var userId = User.Claims.FirstOrDefault(u => u.Type == JwtRegisteredClaimNames.Sub)?.Value;
                if (userId == null)
                {
                    return Unauthorized();
                }

                ResponseDto response = await _authService.GetUser(userId);
                if (response != null && response.IsSuccess)
                {
                    UserDto userProfile = JsonConvert.DeserializeObject<UserDto>(Convert.ToString(response.Result));
                    return View(userProfile);
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                TempData["error"] =ex;
            }
            return NotFound();

        }
        [HttpPost]
        [ActionName("Update")]
        public async Task<IActionResult> Index(UserDto userDto)
        {
            try
            {
                ResponseDto? response = await _authService.UpdateUser(userDto);
                if (response != null && response.IsSuccess)
                {
                    TempData["success"] = "Product updated successfully";
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    TempData["error"] = response?.Message;
                }
                return View(userDto);
            }
            catch (Exception ex)
            {
                TempData["error"] =ex;
            }
            return NotFound();

        }
    }
}
