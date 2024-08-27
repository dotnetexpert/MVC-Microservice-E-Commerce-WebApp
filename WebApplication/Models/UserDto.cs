using WebApplicationUI.Utility;

namespace WebApplicationUI.Models
{
    public class UserDto
    {
        public string ID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Date { get; set; }
        public string Counrty { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }
        [MaxFileSize(1)]
        public IFormFile? Image { get; set; }
        public bool IsActive { get; set; }
    }
}
