using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class ResetPasswordRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}