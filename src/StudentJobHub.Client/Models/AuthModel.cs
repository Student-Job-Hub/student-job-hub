using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace StudentJobHub.Client.Models;

public class LoginModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [Phone(ErrorMessage = "Enter a valid phone number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [PasswordStrength]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    public string? University { get; set; }

    public string? ProfilePictureUrl { get; set; }

}

public class PasswordStrengthAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrWhiteSpace(password))
        {
            return ValidationResult.Success;
        }

        if (password.Length < 8)
        {
            return new ValidationResult("Password must be at least 8 characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            return new ValidationResult("Password must contain at least one capital letter.");
        }

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            return new ValidationResult("Password must contain at least one special character.");
        }

        return ValidationResult.Success;
    }
}

public class AuthResponse
{
    public string Message { get; set; } = string.Empty;

    public string? Token { get; set; }

    public string? RefreshToken { get; set; }
}