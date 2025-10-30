using System.ComponentModel.DataAnnotations;

namespace WarehouseAPI.ModelView.Auth
{
    public class AuthForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "New password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 255 characters")]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Repeat password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Repeat password must be between 6 and 255 characters")]
        public string RepeatPassword { get; set; } = null!;
    }
}
