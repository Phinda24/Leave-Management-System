using Google.Cloud.Firestore;
using System.ComponentModel.DataAnnotations;

namespace Leave_Management_system.Models
{
    [FirestoreData]
    public class Employee
    {
        [Key]
        [FirestoreProperty]
        public string EmployeeId { get; set; } = Guid.NewGuid().ToString();

        [FirestoreProperty]
        [Required]
        public string FullName { get; set; } = string.Empty;

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
        public string Phone { get; set; } // Add this property to fix CS1061
        [FirestoreProperty]
        public string ProfileImageUrl { get; set; } // Add this if needed for consistency

        [FirestoreProperty] public string EmployeeNumber { get; set; }

    }
    [FirestoreData]
    public class LeaveCategory
    {
        [FirestoreProperty]
        public double Entitlement { get; set; }
        [FirestoreProperty]
        public double Taken { get; set; }
    }
    public class FirebaseLoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool ReturnSecureToken { get; set; }
    }
}
