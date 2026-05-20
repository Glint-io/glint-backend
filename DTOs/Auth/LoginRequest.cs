using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// When true, the API also issues HttpOnly, Secure (in production) session cookies on the API host.
        /// </summary>
        public bool UseSessionCookies { get; set; }
    }
}
