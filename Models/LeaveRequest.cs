//using Google.Cloud.Firestore;
//using System;

//namespace Leave_Management_system.Models
//{
//    [FirestoreData]
//    public class LeaveRequest
//    {
//        [FirestoreDocumentId]
//        public string Id { get; set; }

//        [FirestoreProperty]
//        public string EmployeeId { get; set; }

//        [FirestoreProperty]
//        public string LeaveType { get; set; }
//        [FirestoreProperty]
//        public string EmployeeEmail { get; set; }

//        [FirestoreProperty]
//        public string EmployeeName { get; set; }



//        [FirestoreProperty]
//        public string Reason { get; set; }
//        [FirestoreProperty]
//        public string Comment { get; set; }
//        [FirestoreProperty]
//        public string Status { get; set; } = StatusValues.Pending;
//        [FirestoreProperty]
//        public string DocumentUrl { get; set; }
//        [FirestoreProperty] public string Type { get; set; }
//        [FirestoreProperty] public string ReasonForStatus { get; set; }
//        [FirestoreProperty] public Timestamp StartDate { get; set; }
//        [FirestoreProperty] public Timestamp EndDate { get; set; }

//        [FirestoreProperty]
//        public bool IsHalfDay
//        {
//            get; set;
//        }
//        [FirestoreProperty]
//        public Timestamp SubmittedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
//        public DateTime DateApplied { get; internal set; }

//        //
//        // Multi-level approval fields
//        //
//        [FirestoreProperty]
//        public bool SupervisorApproved { get; set; } = false;

//        [FirestoreProperty]
//        public string SupervisorId { get; set; }

//        [FirestoreProperty]
//        public Timestamp SupervisorApprovedAt { get; set; }

//        [FirestoreProperty]
//        public string SupervisorReason { get; set; }

//        [FirestoreProperty]
//        public bool HodApproved { get; set; } = false;

//        [FirestoreProperty]
//        public string HodId { get; set; }

//        [FirestoreProperty]
//        public Timestamp HodApprovedAt { get; set; }

//        [FirestoreProperty]
//        public string HodReason { get; set; }

//        // Audit / history of actions (array of small objects)
//        [FirestoreProperty]
//        public List<ApprovalRecord> ApprovalHistory { get; set; } = new List<ApprovalRecord>();

//        // Helper to produce a friendly display string (employee portal)
//        public string GetDisplayStatus()
//        {
//            // Keep strings simple so they can be localized later if needed
//            switch (Status)
//            {
//                case StatusValues.Pending: return "Pending (waiting for Supervisor)";
//                case StatusValues.PendingHod: return SupervisorApproved ? "Pending (approved by Supervisor, waiting for HOD)" : "Pending (waiting for HOD)";
//                case StatusValues.Approved: return "Approved";
//                case StatusValues.Rejected: return "Rejected";
//                default: return Status; // fallback
//            }
//        }
//    }

//    [FirestoreData]
//    public class ApprovalRecord
//    {
//        [FirestoreProperty]
//        public string Level { get; set; }            // "Supervisor" or "HOD"

//        [FirestoreProperty]
//        public string Action { get; set; }           // "Approved" or "Rejected"

//        [FirestoreProperty]
//        public string By { get; set; }               // user id or name

//        [FirestoreProperty]
//        public string Reason { get; set; }           // optional

//        [FirestoreProperty]
//        public Timestamp At { get; set; }            // when
//    }

//    // Simple status constants to avoid magic strings throughout your app
//    public static class StatusValues
//    {
//        public const string Pending = "Pending";           // waiting for supervisor
//        public const string PendingHod = "PendingHOD";     // supervisor approved, waiting HOD
//        public const string Approved = "Approved";         // final approved
//        public const string Rejected = "Rejected";         // final rejected
//    }
//}





using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace Leave_Management_system.Models
{
    [FirestoreData]
    public class LeaveRequest
    {
        [FirestoreDocumentId]
        public string Id { get; set; }

        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [FirestoreProperty]
        public string LeaveType { get; set; }
        [FirestoreProperty]
        public string EmployeeEmail { get; set; }

        [FirestoreProperty]
        public string EmployeeName { get; set; }

        [FirestoreProperty]
        public string Reason { get; set; }
        [FirestoreProperty]
        public string Comment { get; set; }
        [FirestoreProperty]
        public string Status { get; set; } = StatusValues.Pending;
        [FirestoreProperty]
        public string DocumentUrl { get; set; }
        [FirestoreProperty] public string Type { get; set; }
        [FirestoreProperty] public string ReasonForStatus { get; set; }

        // Made nullable to avoid conversion errors when Firestore has null/missing values
        [FirestoreProperty] public Timestamp? StartDate { get; set; }
        [FirestoreProperty] public Timestamp? EndDate { get; set; }

        [FirestoreProperty]
        public bool IsHalfDay { get; set; }

        // Nullable submitted timestamp; default to now for newly created instances
        [FirestoreProperty]
        public Timestamp? SubmittedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);

        // This property isn't serialized to Firestore (no FirestoreProperty attribute)
        public DateTime DateApplied { get; internal set; }

        //
        // Multi-level approval fields
        //
        [FirestoreProperty]
        public bool SupervisorApproved { get; set; } = false;

        [FirestoreProperty]
        public string SupervisorId { get; set; }

        [FirestoreProperty]
        public Timestamp? SupervisorApprovedAt { get; set; }

        [FirestoreProperty]
        public string SupervisorReason { get; set; }

        [FirestoreProperty]
        public bool HodApproved { get; set; } = false;

        [FirestoreProperty]
        public string HodId { get; set; }

        [FirestoreProperty]
        public Timestamp? HodApprovedAt { get; set; }

        [FirestoreProperty]
        public string HodReason { get; set; }

        // Audit / history of actions (array of small objects)
        [FirestoreProperty]
        public List<ApprovalRecord> ApprovalHistory { get; set; } = new List<ApprovalRecord>();

        // Helper to produce a friendly display string (employee portal)
        public string GetDisplayStatus()
        {
            // Keep strings simple so they can be localized later if needed
            switch (Status)
            {
                case StatusValues.Pending: return "Pending (waiting for Supervisor)";
                case StatusValues.PendingHod: return SupervisorApproved ? "Pending (approved by Supervisor, waiting for HOD)" : "Pending (waiting for HOD)";
                case StatusValues.Approved: return "Approved";
                case StatusValues.Rejected: return "Rejected";
                default: return Status; // fallback
            }
        }
    }

    [FirestoreData]
    public class ApprovalRecord
    {
        [FirestoreProperty]
        public string Level { get; set; }            // "Supervisor" or "HOD"

        [FirestoreProperty]
        public string Action { get; set; }           // "Approved" or "Rejected"

        [FirestoreProperty]
        public string By { get; set; }               // user id or name

        [FirestoreProperty]
        public string Reason { get; set; }           // optional

        // Nullable timestamp for cases where At may be missing
        [FirestoreProperty]
        public Timestamp? At { get; set; }
    }

    // Simple status constants to avoid magic strings throughout your app
    public static class StatusValues
    {
        public const string Pending = "Pending";           // waiting for supervisor
        public const string PendingHod = "PendingHOD";     // supervisor approved, waiting HOD
        public const string Approved = "Approved";         // final approved
        public const string Rejected = "Rejected";         // final rejected
    }
}
