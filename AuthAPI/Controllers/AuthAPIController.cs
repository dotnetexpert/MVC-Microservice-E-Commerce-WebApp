using AuthAPI.Models;
using AuthAPI.Models.Dto;
using AuthAPI.Service;
using AuthAPI.Service.IService;
using AutoMapper;
using MessageBus;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using UAParser;


namespace AuthAPI.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthAPIController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IMessageBus _messageBus;
        private readonly IConfiguration _configuration;
        protected ResponseDto _response;
        private IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthAPIController(IAuthService authService, IMessageBus messageBus,
            IConfiguration configuration, IMapper mapper,
            IAuditLogService auditLogService, SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IJwtTokenGenerator jwtTokenGenerator)

        {
            _authService = authService;//
            _configuration = configuration;
            _messageBus = messageBus;
            _response = new ResponseDto();
            _mapper = mapper;
            _auditLogService = auditLogService;
            _signInManager=signInManager;
            _userManager=userManager;
            _jwtTokenGenerator=jwtTokenGenerator;   
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterationRequestDto model)
        {
            var errorMessage = await _authService.Register(model);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _response.IsSuccess = false;
                _response.Message= errorMessage;
                return BadRequest(_response);
            }
            //await _messageBus.PublishMessage(model.Email, _configuration.GetValue<string>("TopicAndQueueNames:RegisterUserQueue"));
            return Ok(_response);
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            bool isUIRequest = Request.Headers.ContainsKey("X-UI-Request");

            var loginResponse = await _authService.Login(model);
            if (loginResponse.User == null)
            {
                _response.IsSuccess = false;
                _response.Message = "Username or password is incorrect";
                return BadRequest(_response);
            }
            var userAgentString = Request.Headers["User-Agent"].ToString();

            var parser = Parser.GetDefault();
            ClientInfo clientInfo = parser.Parse(userAgentString);
            var browserName = clientInfo.UA.Family;
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = heserver.AddressList
                .FirstOrDefault(p => p.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            var auditLog = new AuditLog
            {
                Action = "User Login",
                Timestamp = DateTime.UtcNow,
                UserId = loginResponse.User.ID,
                IPAddress = ipAddress,
                BrowserInfo=browserName,
                Counrty=loginResponse.User.Counrty,
                AdditionalInfo = $"User '{loginResponse.User.Name}' logged in successfully."
            };

            await _auditLogService.LogAsync(auditLog);

            _response.Result = loginResponse;
            return Ok(_response);
        }

        [HttpPost("AssignRole")]
        public async Task<IActionResult> AssignRole([FromBody] RegisterationRequestDto model)
        {
            var assignRoleSuccessful = await _authService.AssignRole(model.Email, model.Role.ToUpper());
            if (!assignRoleSuccessful)
            {
                _response.IsSuccess = false;
                _response.Message = "Error encountered";
                return BadRequest(_response);
            }
            return Ok(_response);

        }
        [HttpGet("CheckEmailAvailability/{email}")]
        public async Task<IActionResult> CheckEmailAvailability(string email)
        {
            var reslut = await _authService.IsEmailAvailable(email);
            if (reslut ==true)
            {
                _response.Result = "success";
            }
            else
            {
                _response.Result = "error";
            }
            return Ok(_response);
        }
        [HttpGet("GetUser/{Id}")]
        public async Task<IActionResult> GetUser(string Id)
        {
            var result = await _authService.GetUser(Id);

            if (result == null)
            {
                return NotFound();
            }
            _response.Result = result;
            return Ok(_response);
        }
        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetAllUser()
        {
            var result = await _authService.GetAllUsers();

            if (result == null)
            {
                return NotFound();
            }
            _response.Result = result;
            return Ok(_response);
        }
        [HttpPost("ProfileImage")]
        public async Task<IActionResult> ProfileImage(UserDto userDto)
        {
            try
            {
                UserDto user = _mapper.Map<UserDto>(userDto);

                if (userDto.Image != null)
                {
                    if (!string.IsNullOrEmpty(user.ImageLocalPath))
                    {
                        var oldFilePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), user.ImageLocalPath);
                        FileInfo file = new FileInfo(oldFilePathDirectory);
                        if (file.Exists)
                        {
                            file.Delete();
                        }
                    }
                    string fileName = user.ID + Path.GetExtension(user.Image.FileName);
                    string filePath = @"wwwroot\Profile\" + fileName;
                    var filePathDirectory = Path.Combine(Directory.GetCurrentDirectory(), filePath);
                    using (var fileStream = new FileStream(filePathDirectory, FileMode.Create))
                    {
                        userDto.Image.CopyTo(fileStream);
                    }
                    var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host.Value}{HttpContext.Request.PathBase.Value}";
                    user.ImageUrl = baseUrl + "/Profile/" + fileName;
                    user.ImageLocalPath = filePath;
                }
                else
                {
                    user.ImageUrl = "";
                }


                string updateResult = await _authService.UpdateUser(user);

                if (string.IsNullOrEmpty(updateResult))
                {
                    _response.Result = _mapper.Map<UserDto>(user);
                    _response.IsSuccess = true;
                }
                else
                {
                    _response.IsSuccess = false;
                    _response.Message = updateResult;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.Message = ex.Message;
            }
            return Ok(_response);
        }
          [HttpPost("ImpersonateUser/{ID}")]
        public async Task<IActionResult> ImpersonateUser(string ID)
        {
            try
            {
                var currentUser = await _userManager.FindByIdAsync(ID);

                if (currentUser == null)
                {
                    return NotFound("User not found");
                }
                var roles = await _userManager.GetRolesAsync(currentUser);
                var token = _jwtTokenGenerator.GenerateToken(currentUser, roles);
                UserDto userDTO = new()
                {
                    Email = currentUser.Email,
                    ID = currentUser.Id,
                    Name = currentUser.Name,
                    PhoneNumber = currentUser.PhoneNumber,
                    Counrty =currentUser.Counrty

                };

                LoginResponseDto loginResponseDto = new LoginResponseDto()
                {
                    User = userDTO,
                    Token = token
                };
                //var userPrincipal = await _signInManager.CreateUserPrincipalAsync(currentUser);
                //userPrincipal.Identities.First().AddClaim(new Claim("OriginalUserId", currentUser.ID));
                //userPrincipal.Identities.First().AddClaim(new Claim("IsImpersonating", "true"));
                //await _signInManager.SignOutAsync();

                //await HttpContext.SignInAsync(userPrincipal);

                //return Ok("Impersonation successful");
                _response.Result=loginResponseDto;

            }
            catch (Exception ex)
            {

                throw ex;
            }
            return Ok(_response);

        }



    }
}
