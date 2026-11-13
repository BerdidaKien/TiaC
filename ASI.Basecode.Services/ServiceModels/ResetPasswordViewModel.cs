using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a new password.")]
        [PasswordRules]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
