using AuthAPI.Models.Dto;
using AuthAPI.Models;
using AuthAPI.Service.IService;
using Microsoft.AspNetCore.Identity;
using System;
using AuthAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Amqp.Framing;

namespace AuthAPI.Service
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(ApplicationDbContext db, IJwtTokenGenerator jwtTokenGenerator,
            UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _jwtTokenGenerator = jwtTokenGenerator;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<bool> AssignRole(string email, string roleName)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Email.ToLower() == email.ToLower());
            if (user != null)
            {
                if (!_roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult())
                {
                  
                    _roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
                }
                await _userManager.AddToRoleAsync(user, roleName);
                return true;
            }
            return false;

        }

        public async Task<List<UserDto>> GetAllUsers()
        {
            List<ApplicationUser> users = await _userManager.Users.ToListAsync();
            List<UserDto> userDtos = users.Select(user => MapUserToDto(user)).ToList();

            return userDtos;
        }
        private UserDto MapUserToDto(ApplicationUser user)
        {
            return new UserDto
            {
                ID = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                Date = user.Date,
                Counrty = user.Counrty,
                State = user.State,
                Address = user.Address,
                ImageUrl = user.ImageUrl,
                ImageLocalPath = user.ImageLocalPath,
                IsActive = user.IsActive

            };
        }
        public async Task<UserDto> GetUser(string Id)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.Id == Id);
            if (user == null)
            {
                return null;
            }
            var userDto = new UserDto
            {

                ID = user.Id,
                Name = user.Name,
                Email=user.Email,
                Address = user.Address,
                PhoneNumber= user.PhoneNumber,
                Date =user.Date,
                Counrty=user.Counrty,
                State =user.State,
                UserName=user.UserName,
                ImageLocalPath=user.ImageLocalPath,
                ImageUrl=user.ImageUrl,
                IsActive = user.IsActive
            };
            return userDto;
        }
        public async Task<bool> IsEmailAvailable(string email)
        {
            var emailexist = await _db.Users.FirstOrDefaultAsync(u => u.Email==email);
            if (emailexist != null)
            {
                return true;
            }
            else
            { return false; }
        }

        public async Task<LoginResponseDto> Login(LoginRequestDto loginRequestDto)
        {
            var user = _db.ApplicationUsers.FirstOrDefault(u => u.UserName.ToLower() == loginRequestDto.UserName.ToLower());

            bool isValid = await _userManager.CheckPasswordAsync(user, loginRequestDto.Password);

            if (user==null || isValid == false)
            {
                return new LoginResponseDto() { User = null, Token="" };
            }

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtTokenGenerator.GenerateToken(user, roles);

            UserDto userDTO = new()
            {
                Email = user.Email,
                ID = user.Id,
                Name = user.Name,
                PhoneNumber = user.PhoneNumber,
                Counrty =user.Counrty

            };

            LoginResponseDto loginResponseDto = new LoginResponseDto()
            {
                User = userDTO,
                Token = token
            };

            return loginResponseDto;
        }

        public async Task<string> Register(RegisterationRequestDto registrationRequestDto)
        {
            ApplicationUser user = new()
            {
                UserName = registrationRequestDto.Email,
                Email = registrationRequestDto.Email,
                NormalizedEmail = registrationRequestDto.Email.ToUpper(),
                Name = registrationRequestDto.Name,
                PhoneNumber = registrationRequestDto.PhoneNumber,
                Date =registrationRequestDto.Date,
                Counrty = registrationRequestDto.Counrty,
                State = registrationRequestDto.State,
                Address = registrationRequestDto.Address,
                IsActive = registrationRequestDto.IsActive,
            };

            try
            {
                var result = await _userManager.CreateAsync(user, registrationRequestDto.Password);
                if (result.Succeeded)
                {
                    var userToReturn = _db.ApplicationUsers.First(u => u.UserName == registrationRequestDto.Email);

                    UserDto userDto = new()
                    {
                        Email = userToReturn.Email,
                        ID = userToReturn.Id,
                        Name = userToReturn.Name,
                        PhoneNumber = userToReturn.PhoneNumber,
                        Date = userToReturn.Date,
                        Counrty=userToReturn.Counrty,
                        State   =userToReturn.State,
                        Address = userToReturn.Address,
                        IsActive = userToReturn.IsActive,
                    };

                    return "";

                }
                else
                {
                    return result.Errors.FirstOrDefault().Description;
                }

            }
            catch (Exception ex)
            {

            }
            return "Error Encountered";
        }

        public async Task<string> UpdateUser(UserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(userDto.ID);
            if (user == null)
            {
                return "User not found.";
            }

            user.UserName = userDto.Email;
            user.Email = userDto.Email;
            user.Name = userDto.Name;
            user.PhoneNumber = userDto.PhoneNumber;
            user.Date = userDto.Date;
            user.Counrty = userDto.Counrty;
            user.State = userDto.State;
            user.Address = userDto.Address;
            user.ImageLocalPath = userDto.ImageLocalPath;
            user.ImageUrl = userDto.ImageUrl;
            user.IsActive = userDto.IsActive;

            try
            {
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return "";
                }
                else
                {
                    return result.Errors.FirstOrDefault()?.Description ?? "Unknown error occurred.";
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


    }
}



