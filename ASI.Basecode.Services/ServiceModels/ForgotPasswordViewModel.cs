using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.Services.ServiceModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Please enter your User ID.")]
        [Display(Name = "User ID")]
        public string UserId { get; set; } = string.Empty;
    }
}