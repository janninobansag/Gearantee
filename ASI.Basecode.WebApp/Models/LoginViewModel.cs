using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ASI.Basecode.WebApp.Models
{
    public class LoginViewModel
    {
        [JsonPropertyName("userId")]
        [Required(ErrorMessage = "Enter your user code, school ID, or email.")]
        [Display(Name = "User code or email")]
        public string UserId { get; set; }

        [JsonPropertyName("password")]
        [Required(ErrorMessage = "Enter your password.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }
}
