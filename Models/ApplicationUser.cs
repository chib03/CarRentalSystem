using Microsoft.AspNetCore.Identity;

namespace CarRentalSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Role { get; set; }
        public bool IsEmailVerified { get; set; }
        public string? EmailOtp { get; set; }
        public DateTime? EmailOtpExpiry { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? LicenseNumber { get; set; }
    }
}