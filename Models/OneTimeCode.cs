using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace glint_backend.Models;

public enum OneTimeCodeType
{
    EmailVerification,
    Login,
    PasswordReset
}

public class OneTimeCode
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string Code { get; set; } = null!;

    [Required]
    public OneTimeCodeType Type { get; set; }

    [Required]
    public DateTime ExpiresAt { get; set; }

    public bool IsUsed { get; set; } = false;

    // Navigation
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}