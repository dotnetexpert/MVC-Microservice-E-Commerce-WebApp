using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Models.Dto
{
    public class RegisterationRequestDto
    {
        [Remote("UserAlreadyExistsAsync", "Account", ErrorMessage = "User with this Email already exists")]
        public string Email { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string? Role { get; set; }
        public string Date { get; set; }
        public string Counrty { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}
