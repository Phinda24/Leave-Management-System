using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Leave_Management_system.Models
{
    [FirestoreData]
    public class Supervisor
    {
        [Key]
        [FirestoreProperty]
        public string EmployeeId { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty]
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [FirestoreProperty]
        public string Email { get; set; } = string.Empty;

        [Required]
        [FirestoreProperty]
        public string Password { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Department { get; set; } = string.Empty;

        [FirestoreProperty]
        public string Phone { get; set; }
    }
}
