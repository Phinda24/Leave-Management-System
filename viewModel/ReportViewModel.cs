//using Google.Cloud.Firestore;
//using Leave_Management_system.Models;

//namespace Leave_Management_system.Models
//{
//    [FirestoreData]
//    public class ReportViewModel
//    {
//        // Employee Information
//        [FirestoreProperty] public string EmployeeId { get; set; } = Guid.NewGuid().ToString();
//        [FirestoreProperty] public string Department { get; set; }
//        [FirestoreProperty] public string EmployeeName { get; set; }
//        [FirestoreProperty] public string Email { get; set; }
//        [FirestoreProperty] public string Phone { get; set; }


//        // Leave Request Details
//        [FirestoreProperty] public string LeaveType { get; set; }
//        [FirestoreProperty] public Timestamp StartDate { get; set; }
//        [FirestoreProperty] public Timestamp EndDate { get; set; }

//        [FirestoreProperty]
//        public Timestamp SubmittedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
//        [FirestoreProperty] public string Status { get; set; } = StatusValues.Pending;
//        [FirestoreProperty] public string ApproverDecliner { get; set; }
//        [FirestoreProperty] public Timestamp DateActioned { get; set; }
//        [FirestoreProperty] public DateTime DateApplied { get; internal set; }

//        [FirestoreProperty] public string Reason { get; set; }

//        [FirestoreProperty] public double DaysOnLeave { get; set; }


//        // For filtering
//        public DateTime? FilterStartDate { get; set; }
//        public DateTime? FilterEndDate { get; set; }
//        public string FilterLeaveType { get; set; }
//        public string FilterStatus { get; set; }
//        public string FilterEmployeeId { get; set; }


//        // Collections for dropdowns
//        public List<string> LeaveTypes { get; set; } = new List<string>();
//        public List<string> Statuses { get; set; } = new List<string>();
//        public List<Employee> Employees { get; set; } = new List<Employee>();

//        // Report data
//        public List<ReportViewModel> ReportData { get; set; } = new List<ReportViewModel>();

//        // Summary statistics
//        public int TotalRequests { get; set; }
//        public int ApprovedCount { get; set; }
//        public int RejectedCount { get; set; }
//        public int PendingCount { get; set; }
//        public double TotalLeaveDays { get; set; }
//    }


//}


using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;

namespace Leave_Management_system.Models
{
    [FirestoreData]
    public class ReportViewModel
    {
        // Employee Information
        [FirestoreProperty] public string EmployeeId { get; set; } = string.Empty;
        [FirestoreProperty] public string Department { get; set; }
        [FirestoreProperty] public string EmployeeName { get; set; }
        [FirestoreProperty] public string Email { get; set; }
        [FirestoreProperty] public string Phone { get; set; }

        // Leave Request Details
        [FirestoreProperty] public string LeaveType { get; set; }

        // Nullable timestamps to avoid conversion errors when Firestore fields are missing/null
        [FirestoreProperty] public Timestamp? StartDate { get; set; }
        [FirestoreProperty] public Timestamp? EndDate { get; set; }

        [FirestoreProperty] public Timestamp? SubmittedAt { get; set; }
        [FirestoreProperty] public string Status { get; set; } = StatusValues.Pending;
        [FirestoreProperty] public string ApproverDecliner { get; set; }

        // Nullable action date
        [FirestoreProperty] public Timestamp? DateActioned { get; set; }

        // Nullable - not serialized unless you add FirestoreProperty
        public DateTime? DateApplied { get; internal set; }

        [FirestoreProperty] public string Reason { get; set; }

        [FirestoreProperty] public double DaysOnLeave { get; set; }

        // For filtering (view-only)
        public DateTime? FilterStartDate { get; set; }
        public DateTime? FilterEndDate { get; set; }
        public string FilterLeaveType { get; set; }
        public string FilterStatus { get; set; }
        public string FilterEmployeeId { get; set; }

        // Collections for dropdowns
        public List<string> LeaveTypes { get; set; } = new List<string>();
        public List<string> Statuses { get; set; } = new List<string>();
        public List<Employee> Employees { get; set; } = new List<Employee>();

        // Report data (list of rows)
        public List<ReportViewModel> ReportData { get; set; } = new List<ReportViewModel>();

        // Summary statistics
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int PendingCount { get; set; }
        public double TotalLeaveDays { get; set; }
    }
}
