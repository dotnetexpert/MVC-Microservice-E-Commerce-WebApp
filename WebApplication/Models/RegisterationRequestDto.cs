using System.ComponentModel.DataAnnotations;

namespace WebApplicationUI.Models
{
    public class RegisterationRequestDto
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string? Role { get; set; }
        [Required]
        public string Date { get; set; }
        [Required]
        public string Counrty { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string Address { get; set; }
        public bool IsActive { get; set; }
    }
}
