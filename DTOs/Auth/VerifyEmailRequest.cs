using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class VerifyEmailRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
