using Google.Cloud.Firestore;

namespace Leave_Management_system.Models
{
    [FirestoreData]
    public class LeaveBalance
    {
        [FirestoreProperty] public string EmployeeId { get; set; }
        [FirestoreProperty] public string FullName { get; set; }
        [FirestoreProperty] public string Email { get; set; }
        [FirestoreProperty] public string Phone { get; set; }
        [FirestoreProperty] public string ProfileImageUrl { get; set; } = string.Empty;

        // Annual Leave
        [FirestoreProperty] public double AnnualLeaveEntitlement { get; set; }
        [FirestoreProperty] public double AnnualLeaveTaken { get; set; }
        [FirestoreProperty] public double AnnualLeaveBalance { get; set; }

        // Sick Leave
        [FirestoreProperty] public double SickLeaveEntitlement { get; set; }
        [FirestoreProperty] public double SickLeaveTaken { get; set; }
        [FirestoreProperty] public double SickLeaveBalance { get; set; }

        // Family Leave
        [FirestoreProperty] public double FamilyLeaveEntitlement { get; set; }
        [FirestoreProperty] public double FamilyLeaveTaken { get; set; }
        [FirestoreProperty] public double FamilyLeaveBalance { get; set; }

        // Study Leave
        [FirestoreProperty] public double StudyLeaveEntitlement { get; set; }
        [FirestoreProperty] public double StudyLeaveTaken { get; set; }
        [FirestoreProperty] public double StudyLeaveBalance { get; set; }

        // Leave period
        [FirestoreProperty] public Timestamp StartDate { get; set; }
        [FirestoreProperty] public Timestamp EndDate { get; set; }
    }

}

