using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class ResendVerificationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}