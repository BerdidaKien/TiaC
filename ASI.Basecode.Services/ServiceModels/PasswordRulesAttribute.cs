using System.ComponentModel.DataAnnotations;
using ASI.Basecode.Services.Services;

namespace ASI.Basecode.Services.ServiceModels
{
    /// <summary>The one password rule for the whole system (see UserFieldRules.CheckPassword)</summary>
    public class PasswordRulesAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var password = value as string;
            if (string.IsNullOrEmpty(password)) return ValidationResult.Success;   // [Required] reports an empty field

            var error = UserFieldRules.CheckPassword(password);
            return error == null ? ValidationResult.Success : new ValidationResult(error);
        }
    }
}
