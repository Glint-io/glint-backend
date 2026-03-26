using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Auth
{
    public class OtcLoginRequest
    {
        [Required]
        public string Code { get; set; } = string.Empty;
    }
}
