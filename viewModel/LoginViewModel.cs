using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Leave_Management_system.viewModel
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }
    }
}
