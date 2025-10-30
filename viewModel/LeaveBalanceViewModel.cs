
namespace Leave_Management_system.Models
{
    public class LeaveBalanceViewModel
    {
        public string EmployeeId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string ProfileImageUrl { get; set; } = "";

        // Annual
        public double AnnualLeaveEntitlement { get; set; } = 0;
        public double AnnualLeaveTaken { get; set; } = 0;
        public double AnnualLeaveBalance => Math.Max(AnnualLeaveEntitlement - AnnualLeaveTaken, 0);

        // Sick
        public double SickLeaveEntitlement { get; set; } = 0;
        public double SickLeaveTaken { get; set; } = 0;
        public double SickLeaveBalance => Math.Max(SickLeaveEntitlement - SickLeaveTaken, 0);

        // Family
        public double FamilyLeaveEntitlement { get; set; } = 0;
        public double FamilyLeaveTaken { get; set; } = 0;
        public double FamilyLeaveBalance => Math.Max(FamilyLeaveEntitlement - FamilyLeaveTaken, 0);

        // Study
        public double StudyLeaveEntitlement { get; set; } = 0;
        public double StudyLeaveTaken { get; set; } = 0;
        public double StudyLeaveBalance => Math.Max(StudyLeaveEntitlement - StudyLeaveTaken, 0);

        // Dates
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Percent helpers (safe against divide-by-zero)
        private static double SafePercent(double part, double whole)
        {
            if (whole <= 0) return 0;
            var p = (part / whole) * 100.0;
            if (double.IsNaN(p) || double.IsInfinity(p)) return 0;
            return Math.Max(0, Math.Min(100, p));
        }

        public double AnnualUsedPercent => SafePercent(AnnualLeaveTaken, AnnualLeaveEntitlement);
        public double AnnualAvailablePercent => SafePercent(AnnualLeaveBalance, AnnualLeaveEntitlement);

        public double SickUsedPercent => SafePercent(SickLeaveTaken, SickLeaveEntitlement);
        public double SickAvailablePercent => SafePercent(SickLeaveBalance, SickLeaveEntitlement);

        public double FamilyUsedPercent => SafePercent(FamilyLeaveTaken, FamilyLeaveEntitlement);
        public double FamilyAvailablePercent => SafePercent(FamilyLeaveBalance, FamilyLeaveEntitlement);

        public double StudyUsedPercent => SafePercent(StudyLeaveTaken, StudyLeaveEntitlement);
        public double StudyAvailablePercent => SafePercent(StudyLeaveBalance, StudyLeaveEntitlement);
    }
}
