using Microsoft.AspNetCore.Identity;

namespace AuthAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Name { get; set; }
        public string Date { get; set; } 
        public string Counrty { get; set; }
        public string State { get; set; }
        public string Address { get; set; }
        public string? ImageUrl { get; set; }
        public string? ImageLocalPath { get; set; }
        public bool IsActive { get; set; }
    }
}
