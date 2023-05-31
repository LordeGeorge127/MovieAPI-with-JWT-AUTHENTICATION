using System.ComponentModel.DataAnnotations;

namespace MovieApi.Models.DTO
{
    public class ChangePasswordModel
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string CurrentPassword { get; set; }
        [Required]
        public string NewPassword { get; set; }
        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
