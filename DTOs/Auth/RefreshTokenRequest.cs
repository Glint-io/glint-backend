using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
