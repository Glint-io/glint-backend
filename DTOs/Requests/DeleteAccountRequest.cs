using System.ComponentModel.DataAnnotations;

namespace glint_backend.DTOs.Requests;

public class DeleteAccountRequest
{
    [Required(ErrorMessage = "Password is required to delete your account.")]
    public string Password { get; set; } = string.Empty;
}