using System.ComponentModel.DataAnnotations;

namespace ASI.Basecode.WebApp.Models
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Enter your registered email.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [Display(Name = "Registered email")]
        public string Email { get; set; }
    }
}
