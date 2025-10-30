using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Leave_Management_system.viewModel
{
    public class EmployeeLeaveViewModel
    {

        public string leaveRequestId { get; set; }
        public string LeaveType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public string DocumentUrl { get; set; }
        //public bool SupervisorApproved { get; set; }
        public bool SupervisorApproved { get; set; } = false;
    }
    public class EmployeeProfileViewModel
    {
        public string EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Department { get; set; }
             public string? PhotoUrl { get; set; }
    }
    public class EmployeeHomeViewModel
    {

        public EmployeeProfileViewModel Profile { get; set; } = new();
        public List<EmployeeLeaveViewModel> MyLeaves { get; set; } = new();
        public int AnnualCount { get; set; }
        public int SickCount { get; set; }
        public int StudyCount { get; set; }
        public int PendingCount { get; set; }
        public int FamilyCount { get; set; }

    }
}
