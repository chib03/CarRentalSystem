using System.ComponentModel.DataAnnotations;

namespace CarRentalSystem.Models
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}