

//using Microsoft.AspNetCore.Mvc;
//using Google.Cloud.Firestore;
//using Leave_Management_system.Models;
//using Microsoft.AspNetCore.Http;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Leave_Management_system.Controllers
//{
//    public class ReportsController : Controller
//    {
//        private readonly FirestoreDb _db;
//        private readonly IHttpContextAccessor _httpContextAccessor;

//        public ReportsController(FirestoreDb db, IHttpContextAccessor httpContextAccessor)
//        {
//            _db = db;
//            _httpContextAccessor = httpContextAccessor;
//        }

//        // Helper method for session access
//        private string? GetSessionValue(string key)
//        {
//            return _httpContextAccessor.HttpContext?.Session?.GetString(key);
//        }

//        // Safer GetSafeFieldValue that handles non-string fields gracefully
//        private string? GetSafeFieldValue(DocumentSnapshot doc, params string[] possibleFieldNames)
//        {
//            foreach (var field in possibleFieldNames)
//            {
//                if (doc.ContainsField(field))
//                {
//                    try
//                    {
//                        var o = doc.GetValue<object>(field);
//                        if (o == null) continue;
//                        // If it's a string-like value, return string
//                        if (o is string s && !string.IsNullOrEmpty(s)) return s;
//                        // If it's a Timestamp and we want a date string, return ISO date
//                        if (o is Timestamp ts) return ts.ToDateTime().ToString("yyyy-MM-dd");
//                        // Fallback to ToString()
//                        var str = o.ToString();
//                        if (!string.IsNullOrEmpty(str)) return str;
//                    }
//                    catch
//                    {
//                        // ignore parsing errors and try next candidate
//                    }
//                }
//            }
//            return null;
//        }

//        // -----------------------
//        // INDEX (reports landing)
//        // -----------------------
//        [HttpGet]
//        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
//        {
//            try
//            {
//                var model = new ReportViewModel();

//                // Set filter values
//                model.FilterStartDate = startDate;
//                model.FilterEndDate = endDate;
//                model.FilterLeaveType = leaveType;
//                model.FilterStatus = status;
//                model.FilterEmployeeId = employeeId;

//                // Get current user info for the header - WITH SAFE FIELD ACCESS
//                var currentUserId = GetSessionValue("EmployeeId") ?? GetSessionValue("SupID") ?? GetSessionValue("HodID");
//                if (!string.IsNullOrEmpty(currentUserId))
//                {
//                    var userDoc = await GetUserDocumentAsync(currentUserId);
//                    if (userDoc != null && userDoc.Exists)
//                    {
//                        model.EmployeeName = GetSafeFieldValue(userDoc, "FullName", "Name", "DisplayName", "fullName", "name") ?? "Unknown";
//                        model.Email = GetSafeFieldValue(userDoc, "Email", "email", "EmailAddress", "emailAddress") ?? "Unknown";
//                        model.Phone = GetSafeFieldValue(userDoc, "Phone", "phone", "PhoneNumber", "phoneNumber", "ContactNumber") ?? "Unknown";
//                    }
//                    else
//                    {
//                        model.EmployeeName = "Unknown";
//                        model.Email = "Unknown";
//                        model.Phone = "Unknown";
//                    }
//                }
//                else
//                {
//                    model.EmployeeName = "Unknown";
//                    model.Email = "Unknown";
//                    model.Phone = "Unknown";
//                }

//                // Populate dropdown data
//                await PopulateDropdownData(model);

//                // Get report data based on filters
//                model.ReportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);

//                // Calculate summary statistics
//                CalculateSummaryStatistics(model);

//                return View(model);
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error loading reports: {ex.Message}";
//                return View(new ReportViewModel());
//            }
//        }

//        // -----------------------
//        // HOD Reports
//        // -----------------------
//        [HttpGet]
//        public async Task<IActionResult> HodReports(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
//        {
//            try
//            {
//                var model = new ReportViewModel();

//                model.FilterStartDate = startDate;
//                model.FilterEndDate = endDate;
//                model.FilterLeaveType = leaveType;
//                model.FilterStatus = status;
//                model.FilterEmployeeId = employeeId;

//                var hodId = GetSessionValue("HodID");
//                if (!string.IsNullOrEmpty(hodId))
//                {
//                    var hodDoc = await GetUserDocumentAsync(hodId);
//                    if (hodDoc != null && hodDoc.Exists)
//                    {
//                        model.EmployeeName = GetSafeFieldValue(hodDoc, "FullName", "Name") ?? "HOD User";
//                        model.Email = GetSafeFieldValue(hodDoc, "Email", "email") ?? "Unknown";
//                        model.Phone = GetSafeFieldValue(hodDoc, "Phone", "phone") ?? "Unknown";
//                        model.Department = GetSafeFieldValue(hodDoc, "Department", "department") ?? "All Departments";
//                    }
//                }

//                await PopulateHodDropdownData(model, hodId);
//                model.ReportData = await GetHodReportData(hodId, startDate, endDate, leaveType, status, employeeId);
//                CalculateSummaryStatistics(model);
//                await CalculateHodStatistics(model, hodId);

//                ViewBag.ReportType = "HOD";
//                return View("HodReports", model);
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error loading HOD reports: {ex.Message}";
//                return View("HodReports", new ReportViewModel());
//            }
//        }

//        // -----------------------
//        // Supervisor Reports
//        // -----------------------
//        [HttpGet]
//        public async Task<IActionResult> SupervisorReports(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
//        {
//            try
//            {
//                var model = new ReportViewModel();

//                model.FilterStartDate = startDate;
//                model.FilterEndDate = endDate;
//                model.FilterLeaveType = leaveType;
//                model.FilterStatus = status;
//                model.FilterEmployeeId = employeeId;

//                var supervisorId = GetSessionValue("SupID");
//                if (!string.IsNullOrEmpty(supervisorId))
//                {
//                    var supervisorDoc = await GetUserDocumentAsync(supervisorId);
//                    if (supervisorDoc != null && supervisorDoc.Exists)
//                    {
//                        model.EmployeeName = GetSafeFieldValue(supervisorDoc, "FullName", "Name") ?? "Supervisor User";
//                        model.Email = GetSafeFieldValue(supervisorDoc, "Email", "email") ?? "Unknown";
//                        model.Phone = GetSafeFieldValue(supervisorDoc, "Phone", "phone") ?? "Unknown";
//                    }
//                }

//                await PopulateSupervisorDropdownData(model, supervisorId);
//                model.ReportData = await GetSupervisorReportData(supervisorId, startDate, endDate, leaveType, status, employeeId);
//                CalculateSummaryStatistics(model);
//                await CalculateSupervisorStatistics(model, supervisorId);

//                ViewBag.ReportType = "Supervisor";
//                return View("SupervisorReports", model);
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error loading Supervisor reports: {ex.Message}";
//                return View("SupervisorReports", new ReportViewModel());
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> GenerateReport(ReportViewModel model)
//        {
//            // Redirect with filter parameters (use ISO date strings)
//            return RedirectToAction("Index", new
//            {
//                startDate = model.FilterStartDate?.ToString("yyyy-MM-dd"),
//                endDate = model.FilterEndDate?.ToString("yyyy-MM-dd"),
//                leaveType = model.FilterLeaveType,
//                status = model.FilterStatus,
//                employeeId = model.FilterEmployeeId
//            });
//        }

//        // -----------------------
//        // Export CSV
//        // -----------------------
//        [HttpGet]
//        public async Task<IActionResult> ExportToCsv(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
//        {
//            try
//            {
//                var reportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);

//                var csv = "Employee Name,Leave Type,Start Date,End Date,Date Submitted,Status,Approver/Decliner,Date Actioned,Days on Leave,Reason for Leave\n";

//                foreach (var item in reportData)
//                {
//                    csv += $"\"{EscapeCsv(item.EmployeeName)}\","
//                         + $"\"{EscapeCsv(item.LeaveType)}\","
//                         + $"\"{FormatTimestamp(item.StartDate)}\","
//                         + $"\"{FormatTimestamp(item.EndDate)}\","
//                         + $"\"{FormatTimestamp(item.SubmittedAt)}\","
//                         + $"\"{EscapeCsv(item.Status)}\","
//                         + $"\"{EscapeCsv(item.ApproverDecliner)}\","
//                         + $"\"{FormatTimestamp(item.DateActioned)}\","
//                         + $"\"{item.DaysOnLeave}\","
//                         + $"\"{EscapeCsv(item.Reason)}\"\n";
//                }

//                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
//                return File(bytes, "text/csv", $"LeaveReports_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error exporting report: {ex.Message}";
//                return RedirectToAction("Index");
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> PrintReport(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
//        {
//            try
//            {
//                var model = new ReportViewModel();

//                model.FilterStartDate = startDate;
//                model.FilterEndDate = endDate;
//                model.FilterLeaveType = leaveType;
//                model.FilterStatus = status;
//                model.FilterEmployeeId = employeeId;

//                var currentUserId = GetSessionValue("EmployeeId") ?? GetSessionValue("SupID") ?? GetSessionValue("HodID");
//                if (!string.IsNullOrEmpty(currentUserId))
//                {
//                    var userDoc = await GetUserDocumentAsync(currentUserId);
//                    if (userDoc != null && userDoc.Exists)
//                    {
//                        model.EmployeeName = GetSafeFieldValue(userDoc, "FullName") ?? "Unknown";
//                        model.Email = GetSafeFieldValue(userDoc, "Email") ?? "Unknown";
//                        model.Phone = GetSafeFieldValue(userDoc, "Phone") ?? "Unknown";
//                    }
//                }

//                model.ReportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);
//                CalculateSummaryStatistics(model);

//                ViewBag.IsPrintView = true;
//                ViewBag.PrintDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

//                return View("Print", model);
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error generating print report: {ex.Message}";
//                return RedirectToAction("Index");
//            }
//        }


//        private async Task<List<ReportViewModel>> GetReportData(DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
//        {
//            // Build base query with filters except employeeId (we handle employee filtering separately for session-aware logic)
//            Query baseQuery = _db.Collection("LeaveRequests");
//            baseQuery = ApplyReportFilters(baseQuery, startDate, endDate, leaveType, status, null);

//            var snapshots = new List<DocumentSnapshot>();

//            // If an explicit employeeId was provided via UI/params, use that (keeps previous behavior)
//            if (!string.IsNullOrEmpty(employeeId) && employeeId != "All")
//            {
//                var q = baseQuery.WhereEqualTo("EmployeeId", employeeId);
//                var snap = await q.GetSnapshotAsync();
//                snapshots.AddRange(snap.Documents);
//            }
//            else
//            {
//                // No explicit employeeId -> restrict to the currently logged-in user's records
//                var sessionId = GetSessionValue("EmployeeId")
//                                ?? GetSessionValue("Uid")
//                                ?? GetSessionValue("SupID")
//                                ?? GetSessionValue("HodID");

//                var sessionEmail = GetSessionValue("Email") ?? GetSessionValue("UserEmail");
//                sessionEmail = string.IsNullOrWhiteSpace(sessionEmail) ? null : sessionEmail.Trim().ToLower();

//                // If we have both an id and an email, query both and merge (covers mixed document shapes)
//                if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(sessionEmail))
//                {
//                    var qById = baseQuery.WhereEqualTo("EmployeeId", sessionId);
//                    var qByEmail = baseQuery.WhereEqualTo("EmployeeEmail", sessionEmail);

//                    var t1 = qById.GetSnapshotAsync();
//                    var t2 = qByEmail.GetSnapshotAsync();
//                    await Task.WhenAll(t1, t2);

//                    snapshots.AddRange(t1.Result.Documents);
//                    snapshots.AddRange(t2.Result.Documents);
//                }
//                else if (!string.IsNullOrEmpty(sessionId))
//                {
//                    var snap = await baseQuery.WhereEqualTo("EmployeeId", sessionId).GetSnapshotAsync();
//                    snapshots.AddRange(snap.Documents);
//                }
//                else if (!string.IsNullOrEmpty(sessionEmail))
//                {
//                    var snap = await baseQuery.WhereEqualTo("EmployeeEmail", sessionEmail).GetSnapshotAsync();
//                    snapshots.AddRange(snap.Documents);
//                }
//                else
//                {
//                    // No session identity -> return empty list (safer than returning everything)
//                    return new List<ReportViewModel>();
//                }
//            }

//            // Deduplicate documents by id (in case both queries returned same docs)
//            var distinctDocs = snapshots
//                .GroupBy(d => d.Id)
//                .Select(g => g.First())
//                .ToList();

//            var reportData = new List<ReportViewModel>();
//            foreach (var doc in distinctDocs)
//            {
//                try
//                {
//                    var leaveRequest = doc.ConvertTo<LeaveRequest>();
//                    var reportItem = await ConvertToReportItem(leaveRequest);
//                    if (reportItem != null) reportData.Add(reportItem);
//                }
//                catch
//                {
//                    // ignore malformed docs but continue processing others
//                }
//            }

//            return reportData
//                .OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue)
//                .ToList();
//        }


//        private async Task<ReportViewModel> ConvertToReportItem(LeaveRequest leaveRequest)
//        {
//            // Safe days calc (handles missing timestamps)
//            double daysOnLeave = CalculateLeaveDays(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay);

//            var reportItem = new ReportViewModel
//            {
//                EmployeeId = leaveRequest.EmployeeId,
//                EmployeeName = leaveRequest.EmployeeName ?? "Unknown",
//                LeaveType = leaveRequest.LeaveType ?? "Unknown",
//                StartDate = leaveRequest.StartDate,
//                EndDate = leaveRequest.EndDate,
//                SubmittedAt = leaveRequest.SubmittedAt,
//                Status = leaveRequest.Status ?? "Unknown",
//                Reason = leaveRequest.Reason ?? leaveRequest.Comment ?? "N/A",
//                DaysOnLeave = daysOnLeave
//            };

//            await SetApproverInfo(reportItem, leaveRequest);

//            return reportItem;
//        }

//        private async Task SetApproverInfo(ReportViewModel reportItem, LeaveRequest leaveRequest)
//        {
//            string? approverId = null;
//            Timestamp? actionDate = leaveRequest.SubmittedAt; // default

//            if (string.Equals(leaveRequest.Status, StatusValues.Approved, StringComparison.OrdinalIgnoreCase)
//                || string.Equals(leaveRequest.Status, StatusValues.Rejected, StringComparison.OrdinalIgnoreCase))
//            {
//                // prefer HOD timestamp if present and valid
//                if (leaveRequest.HodApprovedAt.HasValue && leaveRequest.HodApprovedAt.Value.ToDateTime() > DateTime.MinValue)
//                {
//                    approverId = leaveRequest.HodId;
//                    actionDate = leaveRequest.HodApprovedAt;
//                }
//                else if (leaveRequest.SupervisorApprovedAt.HasValue && leaveRequest.SupervisorApprovedAt.Value.ToDateTime() > DateTime.MinValue)
//                {
//                    approverId = leaveRequest.SupervisorId;
//                    actionDate = leaveRequest.SupervisorApprovedAt;
//                }
//            }

//            if (!string.IsNullOrEmpty(approverId))
//            {
//                var approverDoc = await GetUserDocumentAsync(approverId);
//                if (approverDoc != null && approverDoc.Exists)
//                {
//                    reportItem.ApproverDecliner = GetSafeFieldValue(approverDoc, "FullName", "Name", "DisplayName") ?? approverId;
//                }
//                else
//                {
//                    reportItem.ApproverDecliner = approverId;
//                }
//            }
//            else
//            {
//                reportItem.ApproverDecliner = "Pending";
//            }

//            reportItem.DateActioned = actionDate;
//        }

//        // -----------------------
//        // Leave days calculation (safe for nullable Timestamps)
//        // -----------------------
//        private double CalculateLeaveDays(Timestamp? startTs, Timestamp? endTs, bool isHalfDay)
//        {
//            if (isHalfDay) return 0.5;

//            if (!startTs.HasValue || !endTs.HasValue)
//                return 0.0;

//            var start = startTs.Value.ToDateTime().Date;
//            var end = endTs.Value.ToDateTime().Date;
//            if (end < start) return 0.0;

//            // inclusive days
//            return (end - start).TotalDays + 1;
//        }

//        // -----------------------
//        // Summary / statistics helpers
//        // -----------------------
//        private void CalculateSummaryStatistics(ReportViewModel model)
//        {
//            model.TotalRequests = model.ReportData?.Count ?? 0;
//            model.ApprovedCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Approved, StringComparison.OrdinalIgnoreCase)) ?? 0;
//            model.RejectedCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Rejected, StringComparison.OrdinalIgnoreCase)) ?? 0;
//            model.PendingCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Pending, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Status, StatusValues.PendingHod, StringComparison.OrdinalIgnoreCase)) ?? 0;
//            model.TotalLeaveDays = model.ReportData?.Sum(x => x.DaysOnLeave) ?? 0;
//        }

//        private async Task CalculateHodStatistics(ReportViewModel model, string hodId)
//        {
//            var employeesQuery = _db.Collection("Employees").WhereEqualTo("HodID", hodId);
//            var employeesSnapshot = await employeesQuery.GetSnapshotAsync();

//            ViewBag.TotalTeamMembers = employeesSnapshot.Documents.Count;
//            ViewBag.Department = model.Department ?? "All Departments";

//            // Calculate approval rate for HOD (best-effort)
//            var hodApproved = model.ReportData?.Count(x =>
//                x.Status == StatusValues.Approved &&
//                x.ApproverDecliner != null &&
//                (hodId != null && x.ApproverDecliner.Contains(hodId.Substring(0, Math.Min(5, hodId.Length))))) ?? 0;

//            ViewBag.HodApprovalRate = model.TotalRequests > 0 ? (hodApproved * 100.0 / model.TotalRequests).ToString("0.0") + "%" : "0%";
//        }

//        private async Task CalculateSupervisorStatistics(ReportViewModel model, string supervisorId)
//        {
//            var employeesQuery = _db.Collection("Employees").WhereEqualTo("SupervisorId", supervisorId);
//            var employeesSnapshot = await employeesQuery.GetSnapshotAsync();

//            ViewBag.TotalTeamMembers = employeesSnapshot.Documents.Count;

//            var supervisorApproved = model.ReportData?.Count(x =>
//                x.Status == StatusValues.Approved &&
//                x.ApproverDecliner != null &&
//                (supervisorId != null && x.ApproverDecliner.Contains(supervisorId.Substring(0, Math.Min(5, supervisorId.Length))))) ?? 0;

//            ViewBag.SupervisorApprovalRate = model.TotalRequests > 0 ? (supervisorApproved * 100.0 / model.TotalRequests).ToString("0.0") + "%" : "0%";
//        }

//        // -----------------------
//        // Dropdown population
//        // -----------------------
//        private async Task PopulateDropdownData(ReportViewModel model)
//        {
//            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
//            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

//            var employeesSnapshot = await _db.Collection("Employees").GetSnapshotAsync();
//            model.Employees = employeesSnapshot.Documents
//                .Select(d => new Employee
//                {
//                    EmployeeId = d.Id,
//                    FullName = GetSafeFieldValue(d, "FullName", "Name", "DisplayName", "fullName", "name") ?? "Unknown User",
//                    Email = GetSafeFieldValue(d, "Email", "email", "EmailAddress", "emailAddress") ?? "No Email",
//                    Phone = GetSafeFieldValue(d, "Phone", "phone", "PhoneNumber", "phoneNumber") ?? "No Phone"
//                })
//                .OrderBy(e => e.FullName)
//                .ToList();
//        }

//        private async Task PopulateHodDropdownData(ReportViewModel model, string hodId)
//        {
//            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
//            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

//            var empDocs = await GetEmployeeDocsForManagerAsync(hodId, new[] { "HodID", "HodId", "Hod" });

//            model.Employees = empDocs.Select(d => new Employee
//            {
//                EmployeeId = d.Id,
//                FullName = GetSafeFieldValue(d, "FullName", "Name") ?? "Unknown User",
//                Email = GetSafeFieldValue(d, "Email", "email") ?? "No Email",
//                Phone = GetSafeFieldValue(d, "Phone", "phone") ?? "No Phone"
//            }).OrderBy(e => e.FullName).ToList();
//        }

//        private async Task<List<ReportViewModel>> GetHodReportData(string hodId, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
//        {
//            if (string.IsNullOrEmpty(hodId))
//                return new List<ReportViewModel>();

//            var empDocs = await GetEmployeeDocsForManagerAsync(hodId, new[] { "HodID", "HodId", "Hod" });
//            var employeeIds = empDocs.Select(d => d.Id).ToList();
//            if (!employeeIds.Any()) return new List<ReportViewModel>();

//            // Firestore WhereIn supports up to 10, split into batches if needed
//            var reportData = new List<ReportViewModel>();
//            const int batchSize = 10;
//            for (int i = 0; i < employeeIds.Count; i += batchSize)
//            {
//                var batch = employeeIds.Skip(i).Take(batchSize).ToList();
//                var query = _db.Collection("LeaveRequests").WhereIn("EmployeeId", batch);
//                query = ApplyReportFilters(query, startDate, endDate, leaveType, status, employeeId);

//                var snapshot = await query.GetSnapshotAsync();
//                foreach (var doc in snapshot.Documents)
//                {
//                    try
//                    {
//                        var lr = doc.ConvertTo<LeaveRequest>();
//                        var item = await ConvertToReportItem(lr);
//                        if (item != null) reportData.Add(item);
//                    }
//                    catch { }
//                }
//            }

//            return reportData.OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue).ToList();
//        }

//        private async Task PopulateSupervisorDropdownData(ReportViewModel model, string supervisorId)
//        {
//            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
//            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

//            var empDocs = await GetEmployeeDocsForManagerAsync(supervisorId, new[] { "SupervisorId", "SupID", "Supervisor" });

//            model.Employees = empDocs.Select(d => new Employee
//            {
//                EmployeeId = d.Id,
//                FullName = GetSafeFieldValue(d, "FullName", "Name") ?? "Unknown User",
//                Email = GetSafeFieldValue(d, "Email", "email") ?? "No Email",
//                Phone = GetSafeFieldValue(d, "Phone", "phone") ?? "No Phone"
//            }).OrderBy(e => e.FullName).ToList();
//        }

//        private async Task<List<ReportViewModel>> GetSupervisorReportData(string supervisorId, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
//        {
//            if (string.IsNullOrEmpty(supervisorId))
//                return new List<ReportViewModel>();

//            var empDocs = await GetEmployeeDocsForManagerAsync(supervisorId, new[] { "SupervisorId", "SupID", "Supervisor" });
//            var employeeIds = empDocs.Select(d => d.Id).ToList();
//            if (!employeeIds.Any()) return new List<ReportViewModel>();

//            var reportData = new List<ReportViewModel>();
//            const int batchSize = 10;
//            for (int i = 0; i < employeeIds.Count; i += batchSize)
//            {
//                var batch = employeeIds.Skip(i).Take(batchSize).ToList();
//                var query = _db.Collection("LeaveRequests").WhereIn("EmployeeId", batch);
//                query = ApplyReportFilters(query, startDate, endDate, leaveType, status, employeeId);

//                var snapshot = await query.GetSnapshotAsync();
//                foreach (var doc in snapshot.Documents)
//                {
//                    try
//                    {
//                        var lr = doc.ConvertTo<LeaveRequest>();
//                        var item = await ConvertToReportItem(lr);
//                        if (item != null) reportData.Add(item);
//                    }
//                    catch { }
//                }
//            }

//            return reportData.OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue).ToList();
//        }

//        // Apply filters helper (unchanged except safe Timestamp conversion)
//        private Query ApplyReportFilters(Query query, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
//        {
//            if (startDate.HasValue)
//            {
//                var startTimestamp = Timestamp.FromDateTime(startDate.Value.ToUniversalTime());
//                query = query.WhereGreaterThanOrEqualTo("StartDate", startTimestamp);
//            }

//            if (endDate.HasValue)
//            {
//                var endTimestamp = Timestamp.FromDateTime(endDate.Value.ToUniversalTime());
//                query = query.WhereLessThanOrEqualTo("EndDate", endTimestamp);
//            }

//            if (!string.IsNullOrEmpty(leaveType) && leaveType != "All")
//            {
//                query = query.WhereEqualTo("LeaveType", leaveType);
//            }

//            if (!string.IsNullOrEmpty(status) && status != "All")
//            {
//                query = query.WhereEqualTo("Status", status);
//            }

//            if (!string.IsNullOrEmpty(employeeId) && employeeId != "All")
//            {
//                query = query.WhereEqualTo("EmployeeId", employeeId);
//            }

//            return query;
//        }

//        // Tries Employees, Supervisors, HODs collections for the user id
//        private async Task<DocumentSnapshot?> GetUserDocumentAsync(string userId)
//        {
//            var collections = new[] { "Employees", "Supervisors", "HODs" };
//            foreach (var collection in collections)
//            {
//                var doc = await _db.Collection(collection).Document(userId).GetSnapshotAsync();
//                if (doc.Exists) return doc;
//            }
//            return null;
//        }

//        // tolerant helper to find employee documents referencing a manager id in any of several field name variants
//        // tolerant helper to find employee documents referencing a manager id in any of several field name variants
//        private async Task<List<DocumentSnapshot>> GetEmployeeDocsForManagerAsync(string managerId, string[] managerFieldVariants)
//        {
//            var results = new List<DocumentSnapshot>();

//            if (string.IsNullOrWhiteSpace(managerId) || managerFieldVariants == null || managerFieldVariants.Length == 0)
//                return results;

//            // Read all employees — avoids relying on exact field name indexing. If Employees is huge, consider a different approach.
//            var employeesSnap = await _db.Collection("Employees").GetSnapshotAsync();

//            foreach (var doc in employeesSnap.Documents)
//            {
//                try
//                {
//                    foreach (var field in managerFieldVariants)
//                    {
//                        if (!doc.ContainsField(field)) continue;

//                        var valObj = doc.GetValue<object>(field);
//                        if (valObj == null) continue;

//                        var val = valObj.ToString();
//                        if (string.IsNullOrWhiteSpace(val)) continue;

//                        if (string.Equals(val.Trim(), managerId.Trim(), StringComparison.OrdinalIgnoreCase))
//                        {
//                            results.Add(doc);
//                            break; // matched one variant for this employee, go to next employee doc
//                        }
//                    }
//                }
//                catch
//                {
//                    // ignore malformed doc and continue
//                    continue;
//                }
//            }

//            return results;
//        }

//        // Use this in ReportsController (paste anywhere inside the class)
//        private async Task<DocumentSnapshot?> GetCurrentUserDocumentAsync()
//        {
//            // 1) try session id keys first (fast, direct doc lookup)
//            var possibleIdKeys = new[] { "EmployeeId", "SupID", "SupId", "HodID", "HodId", "UserId", "Uid" };
//            foreach (var key in possibleIdKeys)
//            {
//                var val = GetSessionValue(key);
//                if (string.IsNullOrWhiteSpace(val)) continue;

//                // try direct doc lookup (Employees / Supervisors / HODs)
//                var doc = await GetUserDocumentAsync(val);
//                if (doc != null && doc.Exists) return doc;
//            }

//            // 2) fallback: try email keys and query collections
//            var possibleEmailKeys = new[] { "Email", "UserEmail", "userEmail", "email" };
//            string? email = null;
//            foreach (var ek in possibleEmailKeys)
//            {
//                var v = GetSessionValue(ek);
//                if (!string.IsNullOrWhiteSpace(v))
//                {
//                    email = v.Trim().ToLowerInvariant();
//                    break;
//                }
//            }

//            if (!string.IsNullOrWhiteSpace(email))
//            {
//                // Try direct equality query on common field names
//                var collections = new[] { "Employees", "Supervisors", "HODs" };
//                var commonEmailFields = new[] { "Email", "email", "EmailAddress", "EmailAddressNormalized" };

//                foreach (var col in collections)
//                {
//                    foreach (var field in commonEmailFields)
//                    {
//                        try
//                        {
//                            var q = _db.Collection(col).WhereEqualTo(field, email);
//                            var snap = await q.GetSnapshotAsync();
//                            if (snap.Documents.Count > 0) return snap.Documents.First();
//                        }
//                        catch
//                        {
//                            // if field doesn't exist or query fails, ignore and continue
//                        }
//                    }
//                }

//                // Last resort: fetch small set and compare case-insensitive (safe but expensive)
//                foreach (var col in collections)
//                {
//                    try
//                    {
//                        var all = await _db.Collection(col).GetSnapshotAsync();
//                        var found = all.Documents.FirstOrDefault(d =>
//                        {
//                            try
//                            {
//                                // try multiple field names on the document
//                                foreach (var candidate in commonEmailFields)
//                                {
//                                    if (d.ContainsField(candidate))
//                                    {
//                                        var o = d.GetValue<object>(candidate);
//                                        if (o != null && o.ToString().Trim().Equals(email, StringComparison.OrdinalIgnoreCase))
//                                            return true;
//                                    }
//                                }

//                                // as a final fallback check all string fields
//                                foreach (var kv in d.ToDictionary())
//                                {
//                                    if (kv.Value == null) continue;
//                                    if (kv.Value.ToString().Trim().Equals(email, StringComparison.OrdinalIgnoreCase))
//                                        return true;
//                                }
//                            }
//                            catch { }
//                            return false;
//                        });

//                        if (found != null) return found;
//                    }
//                    catch { /* ignore */ }
//                }
//            }

//            // nothing found
//            return null;
//        }


//        // Debug helper - unchanged except kept safe
//        [HttpGet]
//        public async Task<IActionResult> DebugFields()
//        {
//            try
//            {
//                var result = new List<object>();

//                var employeesSnapshot = await _db.Collection("Employees").Limit(3).GetSnapshotAsync();
//                foreach (var doc in employeesSnapshot.Documents)
//                {
//                    var fields = new Dictionary<string, object>();
//                    foreach (var field in doc.ToDictionary())
//                    {
//                        fields[field.Key] = field.Value;
//                    }
//                    result.Add(new { Collection = "Employees", DocumentId = doc.Id, Fields = fields });
//                }

//                var leaveRequestsSnapshot = await _db.Collection("LeaveRequests").Limit(3).GetSnapshotAsync();
//                foreach (var doc in leaveRequestsSnapshot.Documents)
//                {
//                    var fields = new Dictionary<string, object>();
//                    foreach (var field in doc.ToDictionary())
//                    {
//                        fields[field.Key] = field.Value;
//                    }
//                    result.Add(new { Collection = "LeaveRequests", DocumentId = doc.Id, Fields = fields });
//                }

//                return Json(result);
//            }
//            catch (Exception ex)
//            {
//                return Json(new { error = ex.Message });
//            }
//        }

//        // -----------------------
//        // Utilities
//        // -----------------------
//        private static string FormatTimestamp(Timestamp? t)
//        {
//            return t.HasValue ? t.Value.ToDateTime().ToString("yyyy-MM-dd") : "";
//        }

//        private static string EscapeCsv(string? s)
//        {
//            if (string.IsNullOrEmpty(s)) return "";
//            return s.Replace("\"", "\"\""); // basic CSV escaping
//        }



//    }
//}

using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_system.Controllers
{
    public class ReportsController : Controller
    {
        private readonly FirestoreDb _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportsController(FirestoreDb db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        // Helper method for session access
        private string? GetSessionValue(string key)
        {
            return _httpContextAccessor.HttpContext?.Session?.GetString(key);
        }

        // Safer GetSafeFieldValue that handles non-string fields gracefully
        private string? GetSafeFieldValue(DocumentSnapshot doc, params string[] possibleFieldNames)
        {
            foreach (var field in possibleFieldNames)
            {
                if (doc.ContainsField(field))
                {
                    try
                    {
                        var o = doc.GetValue<object>(field);
                        if (o == null) continue;
                        // If it's a string-like value, return string
                        if (o is string s && !string.IsNullOrEmpty(s)) return s;
                        // If it's a Timestamp and we want a date string, return ISO date
                        if (o is Timestamp ts) return ts.ToDateTime().ToString("yyyy-MM-dd");
                        // Fallback to ToString()
                        var str = o.ToString();
                        if (!string.IsNullOrEmpty(str)) return str;
                    }
                    catch
                    {
                        // ignore parsing errors and try next candidate
                    }
                }
            }
            return null;
        }

        // -----------------------
        // INDEX (reports landing)
        // -----------------------
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
        {
            try
            {
                var model = new ReportViewModel();

                // Set filter values
                model.FilterStartDate = startDate;
                model.FilterEndDate = endDate;
                model.FilterLeaveType = leaveType;
                model.FilterStatus = status;
                model.FilterEmployeeId = employeeId;

                // Get current user info for the header - use tolerant helper
                var userDoc = await GetCurrentUserDocumentAsync();
                if (userDoc != null && userDoc.Exists)
                {
                    model.EmployeeName = GetSafeFieldValue(userDoc, "FullName", "Name", "DisplayName", "fullName", "name") ?? "Unknown";
                    model.Email = GetSafeFieldValue(userDoc, "Email", "email", "EmailAddress", "emailAddress") ?? "Unknown";
                    model.Phone = GetSafeFieldValue(userDoc, "Phone", "phone", "PhoneNumber", "phoneNumber", "ContactNumber") ?? "Unknown";
                }
                else
                {
                    model.EmployeeName = "Unknown";
                    model.Email = "Unknown";
                    model.Phone = "Unknown";
                }

                // Populate dropdown data
                await PopulateDropdownData(model);

                // Get report data based on filters
                model.ReportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);

                // Calculate summary statistics
                CalculateSummaryStatistics(model);

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading reports: {ex.Message}";
                return View(new ReportViewModel());
            }
        }

        // -----------------------
        // HOD Reports
        // -----------------------
        [HttpGet]
        public async Task<IActionResult> HodReports(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
        {
            try
            {
                var model = new ReportViewModel();

                model.FilterStartDate = startDate;
                model.FilterEndDate = endDate;
                model.FilterLeaveType = leaveType;
                model.FilterStatus = status;
                model.FilterEmployeeId = employeeId;

                // Use tolerant helper to find current HOD doc (works if session contains id OR email)
                var hodDoc = await GetCurrentUserDocumentAsync();
                if (hodDoc != null && hodDoc.Exists)
                {
                    model.EmployeeName = GetSafeFieldValue(hodDoc, "FullName", "Name", "DisplayName") ?? "HOD User";
                    model.Email = GetSafeFieldValue(hodDoc, "Email", "email", "EmailAddress") ?? "Unknown";
                    model.Phone = GetSafeFieldValue(hodDoc, "Phone", "phone") ?? "Unknown";
                    model.Department = GetSafeFieldValue(hodDoc, "Department", "department") ?? "All Departments";

                    // determine hodId for dropdowns/reports (prefer doc.Id)
                    var hodId = hodDoc.Id;
                    await PopulateHodDropdownData(model, hodId);
                    model.ReportData = await GetHodReportData(hodId, startDate, endDate, leaveType, status, employeeId);
                    CalculateSummaryStatistics(model);
                    await CalculateHodStatistics(model, hodId);

                    ViewBag.ReportType = "HOD";
                    return View("HodReports", model);
                }
                else
                {
                    // if no hodDoc found, still populate empty view with dropdown defaults
                    await PopulateHodDropdownData(model, null);
                    model.ReportData = new List<ReportViewModel>();
                    CalculateSummaryStatistics(model);
                    ViewBag.ReportType = "HOD";
                    return View("HodReports", model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading HOD reports: {ex.Message}";
                return View("HodReports", new ReportViewModel());
            }
        }

        // -----------------------
        // Supervisor Reports
        // -----------------------
        [HttpGet]
        public async Task<IActionResult> SupervisorReports(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
        {
            try
            {
                var model = new ReportViewModel();

                model.FilterStartDate = startDate;
                model.FilterEndDate = endDate;
                model.FilterLeaveType = leaveType;
                model.FilterStatus = status;
                model.FilterEmployeeId = employeeId;

                // Use tolerant helper to find current Supervisor doc (works if session contains id OR email)
                var supDoc = await GetCurrentUserDocumentAsync();
                if (supDoc != null && supDoc.Exists)
                {
                    model.EmployeeName = GetSafeFieldValue(supDoc, "FullName", "Name", "DisplayName") ?? "Supervisor User";
                    model.Email = GetSafeFieldValue(supDoc, "Email", "email", "EmailAddress") ?? "Unknown";
                    model.Phone = GetSafeFieldValue(supDoc, "Phone", "phone") ?? "Unknown";

                    var supervisorId = supDoc.Id;
                    await PopulateSupervisorDropdownData(model, supervisorId);
                    model.ReportData = await GetSupervisorReportData(supervisorId, startDate, endDate, leaveType, status, employeeId);
                    CalculateSummaryStatistics(model);
                    await CalculateSupervisorStatistics(model, supervisorId);

                    ViewBag.ReportType = "Supervisor";
                    return View("SupervisorReports", model);
                }
                else
                {
                    // no supervisor doc found in session -> show defaults
                    await PopulateSupervisorDropdownData(model, null);
                    model.ReportData = new List<ReportViewModel>();
                    CalculateSummaryStatistics(model);
                    ViewBag.ReportType = "Supervisor";
                    return View("SupervisorReports", model);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading Supervisor reports: {ex.Message}";
                return View("SupervisorReports", new ReportViewModel());
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerateReport(ReportViewModel model)
        {
            // Redirect with filter parameters (use ISO date strings)
            return RedirectToAction("Index", new
            {
                startDate = model.FilterStartDate?.ToString("yyyy-MM-dd"),
                endDate = model.FilterEndDate?.ToString("yyyy-MM-dd"),
                leaveType = model.FilterLeaveType,
                status = model.FilterStatus,
                employeeId = model.FilterEmployeeId
            });
        }

        // -----------------------
        // Export CSV
        // -----------------------
        [HttpGet]
        public async Task<IActionResult> ExportToCsv(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
        {
            try
            {
                var reportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);

                var csv = "Employee Name,Leave Type,Start Date,End Date,Date Submitted,Status,Approver/Decliner,Date Actioned,Days on Leave,Reason for Leave\n";

                foreach (var item in reportData)
                {
                    csv += $"\"{EscapeCsv(item.EmployeeName)}\","
                         + $"\"{EscapeCsv(item.LeaveType)}\","
                         + $"\"{FormatTimestamp(item.StartDate)}\","
                         + $"\"{FormatTimestamp(item.EndDate)}\","
                         + $"\"{FormatTimestamp(item.SubmittedAt)}\","
                         + $"\"{EscapeCsv(item.Status)}\","
                         + $"\"{EscapeCsv(item.ApproverDecliner)}\","
                         + $"\"{FormatTimestamp(item.DateActioned)}\","
                         + $"\"{item.DaysOnLeave}\","
                         + $"\"{EscapeCsv(item.Reason)}\"\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                return File(bytes, "text/csv", $"LeaveReports_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error exporting report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PrintReport(DateTime? startDate, DateTime? endDate, string leaveType = "", string status = "", string employeeId = "")
        {
            try
            {
                var model = new ReportViewModel();

                model.FilterStartDate = startDate;
                model.FilterEndDate = endDate;
                model.FilterLeaveType = leaveType;
                model.FilterStatus = status;
                model.FilterEmployeeId = employeeId;

                var currentUserId = GetSessionValue("EmployeeId") ?? GetSessionValue("SupID") ?? GetSessionValue("HodID");
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var userDoc = await GetUserDocumentAsync(currentUserId);
                    if (userDoc != null && userDoc.Exists)
                    {
                        model.EmployeeName = GetSafeFieldValue(userDoc, "FullName") ?? "Unknown";
                        model.Email = GetSafeFieldValue(userDoc, "Email") ?? "Unknown";
                        model.Phone = GetSafeFieldValue(userDoc, "Phone") ?? "Unknown";
                    }
                }

                model.ReportData = await GetReportData(startDate, endDate, leaveType, status, employeeId);
                CalculateSummaryStatistics(model);

                ViewBag.IsPrintView = true;
                ViewBag.PrintDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");

                return View("Print", model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error generating print report: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        // -----------------------
        // Data retrieval & conversion (session-aware)
        // -----------------------
        private async Task<List<ReportViewModel>> GetReportData(DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
        {
            // Build base query with filters except employeeId (we handle employee filtering separately for session-aware logic)
            Query baseQuery = _db.Collection("LeaveRequests");
            baseQuery = ApplyReportFilters(baseQuery, startDate, endDate, leaveType, status, null);

            var snapshots = new List<DocumentSnapshot>();

            // If an explicit employeeId was provided via UI/params, use that (keeps previous behavior)
            if (!string.IsNullOrEmpty(employeeId) && employeeId != "All")
            {
                var q = baseQuery.WhereEqualTo("EmployeeId", employeeId);
                var snap = await q.GetSnapshotAsync();
                snapshots.AddRange(snap.Documents);
            }
            else
            {
                // No explicit employeeId -> restrict to the currently logged-in user's records
                var sessionId = GetSessionValue("EmployeeId")
                                ?? GetSessionValue("Uid")
                                ?? GetSessionValue("SupID")
                                ?? GetSessionValue("HodID");

                var sessionEmail = GetSessionValue("Email") ?? GetSessionValue("UserEmail");
                sessionEmail = string.IsNullOrWhiteSpace(sessionEmail) ? null : sessionEmail.Trim().ToLower();

                // If we have both an id and an email, query both and merge (covers mixed document shapes)
                if (!string.IsNullOrEmpty(sessionId) && !string.IsNullOrEmpty(sessionEmail))
                {
                    var qById = baseQuery.WhereEqualTo("EmployeeId", sessionId);
                    var qByEmail = baseQuery.WhereEqualTo("EmployeeEmail", sessionEmail);

                    var t1 = qById.GetSnapshotAsync();
                    var t2 = qByEmail.GetSnapshotAsync();
                    await Task.WhenAll(t1, t2);

                    snapshots.AddRange(t1.Result.Documents);
                    snapshots.AddRange(t2.Result.Documents);
                }
                else if (!string.IsNullOrEmpty(sessionId))
                {
                    var snap = await baseQuery.WhereEqualTo("EmployeeId", sessionId).GetSnapshotAsync();
                    snapshots.AddRange(snap.Documents);
                }
                else if (!string.IsNullOrEmpty(sessionEmail))
                {
                    var snap = await baseQuery.WhereEqualTo("EmployeeEmail", sessionEmail).GetSnapshotAsync();
                    snapshots.AddRange(snap.Documents);
                }
                else
                {
                    // No session identity -> return empty list (safer than returning everything)
                    return new List<ReportViewModel>();
                }
            }

            // Deduplicate documents by id (in case both queries returned same docs)
            var distinctDocs = snapshots
                .GroupBy(d => d.Id)
                .Select(g => g.First())
                .ToList();

            var reportData = new List<ReportViewModel>();
            foreach (var doc in distinctDocs)
            {
                try
                {
                    var leaveRequest = doc.ConvertTo<LeaveRequest>();
                    var reportItem = await ConvertToReportItem(leaveRequest);
                    if (reportItem != null) reportData.Add(reportItem);
                }
                catch
                {
                    // ignore malformed docs but continue processing others
                }
            }

            return reportData
                .OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue)
                .ToList();
        }

        private async Task<ReportViewModel> ConvertToReportItem(LeaveRequest leaveRequest)
        {
            // Safe days calc (handles missing timestamps)
            double daysOnLeave = CalculateLeaveDays(leaveRequest.StartDate, leaveRequest.EndDate, leaveRequest.IsHalfDay);

            var reportItem = new ReportViewModel
            {
                EmployeeId = leaveRequest.EmployeeId,
                EmployeeName = leaveRequest.EmployeeName ?? "Unknown",
                LeaveType = leaveRequest.LeaveType ?? "Unknown",
                StartDate = leaveRequest.StartDate,
                EndDate = leaveRequest.EndDate,
                SubmittedAt = leaveRequest.SubmittedAt,
                Status = leaveRequest.Status ?? "Unknown",
                Reason = leaveRequest.Reason ?? leaveRequest.Comment ?? "N/A",
                DaysOnLeave = daysOnLeave
            };

            await SetApproverInfo(reportItem, leaveRequest);

            return reportItem;
        }

        private async Task SetApproverInfo(ReportViewModel reportItem, LeaveRequest leaveRequest)
        {
            string? approverId = null;
            Timestamp? actionDate = leaveRequest.SubmittedAt; // default

            if (string.Equals(leaveRequest.Status, StatusValues.Approved, StringComparison.OrdinalIgnoreCase)
                || string.Equals(leaveRequest.Status, StatusValues.Rejected, StringComparison.OrdinalIgnoreCase))
            {
                // prefer HOD timestamp if present and valid
                if (leaveRequest.HodApprovedAt.HasValue && leaveRequest.HodApprovedAt.Value.ToDateTime() > DateTime.MinValue)
                {
                    approverId = leaveRequest.HodId;
                    actionDate = leaveRequest.HodApprovedAt;
                }
                else if (leaveRequest.SupervisorApprovedAt.HasValue && leaveRequest.SupervisorApprovedAt.Value.ToDateTime() > DateTime.MinValue)
                {
                    approverId = leaveRequest.SupervisorId;
                    actionDate = leaveRequest.SupervisorApprovedAt;
                }
            }

            if (!string.IsNullOrEmpty(approverId))
            {
                var approverDoc = await GetUserDocumentAsync(approverId);
                if (approverDoc != null && approverDoc.Exists)
                {
                    reportItem.ApproverDecliner = GetSafeFieldValue(approverDoc, "FullName", "Name", "DisplayName") ?? approverId;
                }
                else
                {
                    reportItem.ApproverDecliner = approverId;
                }
            }
            else
            {
                reportItem.ApproverDecliner = "Pending";
            }

            reportItem.DateActioned = actionDate;
        }

        // -----------------------
        // Leave days calculation (safe for nullable Timestamps)
        // -----------------------
        private double CalculateLeaveDays(Timestamp? startTs, Timestamp? endTs, bool isHalfDay)
        {
            if (isHalfDay) return 0.5;

            if (!startTs.HasValue || !endTs.HasValue)
                return 0.0;

            var start = startTs.Value.ToDateTime().Date;
            var end = endTs.Value.ToDateTime().Date;
            if (end < start) return 0.0;

            // inclusive days
            return (end - start).TotalDays + 1;
        }

        // -----------------------
        // Summary / statistics helpers
        // -----------------------
        private void CalculateSummaryStatistics(ReportViewModel model)
        {
            model.TotalRequests = model.ReportData?.Count ?? 0;
            model.ApprovedCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Approved, StringComparison.OrdinalIgnoreCase)) ?? 0;
            model.RejectedCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Rejected, StringComparison.OrdinalIgnoreCase)) ?? 0;
            model.PendingCount = model.ReportData?.Count(x => string.Equals(x.Status, StatusValues.Pending, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Status, StatusValues.PendingHod, StringComparison.OrdinalIgnoreCase)) ?? 0;
            model.TotalLeaveDays = model.ReportData?.Sum(x => x.DaysOnLeave) ?? 0;
        }

        private async Task CalculateHodStatistics(ReportViewModel model, string hodId)
        {
            var employeesQuery = _db.Collection("Employees").WhereEqualTo("HodID", hodId);
            var employeesSnapshot = await employeesQuery.GetSnapshotAsync();

            ViewBag.TotalTeamMembers = employeesSnapshot.Documents.Count;
            ViewBag.Department = model.Department ?? "All Departments";

            // Calculate approval rate for HOD (best-effort)
            var hodApproved = model.ReportData?.Count(x =>
                x.Status == StatusValues.Approved &&
                x.ApproverDecliner != null &&
                (hodId != null && x.ApproverDecliner.Contains(hodId.Substring(0, Math.Min(5, hodId.Length))))) ?? 0;

            ViewBag.HodApprovalRate = model.TotalRequests > 0 ? (hodApproved * 100.0 / model.TotalRequests).ToString("0.0") + "%" : "0%";
        }

        private async Task CalculateSupervisorStatistics(ReportViewModel model, string supervisorId)
        {
            var employeesQuery = _db.Collection("Employees").WhereEqualTo("SupervisorId", supervisorId);
            var employeesSnapshot = await employeesQuery.GetSnapshotAsync();

            ViewBag.TotalTeamMembers = employeesSnapshot.Documents.Count;

            var supervisorApproved = model.ReportData?.Count(x =>
                x.Status == StatusValues.Approved &&
                x.ApproverDecliner != null &&
                (supervisorId != null && x.ApproverDecliner.Contains(supervisorId.Substring(0, Math.Min(5, supervisorId.Length))))) ?? 0;

            ViewBag.SupervisorApprovalRate = model.TotalRequests > 0 ? (supervisorApproved * 100.0 / model.TotalRequests).ToString("0.0") + "%" : "0%";
        }

        // -----------------------
        // Dropdown population
        // -----------------------
        private async Task PopulateDropdownData(ReportViewModel model)
        {
            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

            var employeesSnapshot = await _db.Collection("Employees").GetSnapshotAsync();
            model.Employees = employeesSnapshot.Documents
                .Select(d => new Employee
                {
                    EmployeeId = d.Id,
                    FullName = GetSafeFieldValue(d, "FullName", "Name", "DisplayName", "fullName", "name") ?? "Unknown User",
                    Email = GetSafeFieldValue(d, "Email", "email", "EmailAddress", "emailAddress") ?? "No Email",
                    Phone = GetSafeFieldValue(d, "Phone", "phone", "PhoneNumber", "phoneNumber") ?? "No Phone"
                })
                .OrderBy(e => e.FullName)
                .ToList();
        }

        private async Task PopulateHodDropdownData(ReportViewModel model, string hodId)
        {
            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

            var empDocs = await GetEmployeeDocsForManagerAsync(hodId, new[] { "HodID", "HodId", "Hod" });

            model.Employees = empDocs.Select(d => new Employee
            {
                EmployeeId = d.Id,
                FullName = GetSafeFieldValue(d, "FullName", "Name") ?? "Unknown User",
                Email = GetSafeFieldValue(d, "Email", "email") ?? "No Email",
                Phone = GetSafeFieldValue(d, "Phone", "phone") ?? "No Phone"
            }).OrderBy(e => e.FullName).ToList();
        }

        private async Task<List<ReportViewModel>> GetHodReportData(string hodId, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
        {
            if (string.IsNullOrEmpty(hodId))
                return new List<ReportViewModel>();

            var empDocs = await GetEmployeeDocsForManagerAsync(hodId, new[] { "HodID", "HodId", "Hod" });
            var employeeIds = empDocs.Select(d => d.Id).ToList();
            if (!employeeIds.Any()) return new List<ReportViewModel>();

            // Firestore WhereIn supports up to 10, split into batches if needed
            var reportData = new List<ReportViewModel>();
            const int batchSize = 10;
            for (int i = 0; i < employeeIds.Count; i += batchSize)
            {
                var batch = employeeIds.Skip(i).Take(batchSize).ToList();
                var query = _db.Collection("LeaveRequests").WhereIn("EmployeeId", batch);
                query = ApplyReportFilters(query, startDate, endDate, leaveType, status, employeeId);

                var snapshot = await query.GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var lr = doc.ConvertTo<LeaveRequest>();
                        var item = await ConvertToReportItem(lr);
                        if (item != null) reportData.Add(item);
                    }
                    catch { }
                }
            }

            return reportData.OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue).ToList();
        }

        private async Task PopulateSupervisorDropdownData(ReportViewModel model, string supervisorId)
        {
            model.LeaveTypes = new List<string> { "All", "Annual", "Sick", "Study", "Family Responsibility", "Family", "Other" };
            model.Statuses = new List<string> { "All", "Pending", "PendingHod", "Approved", "Rejected" };

            var empDocs = await GetEmployeeDocsForManagerAsync(supervisorId, new[] { "SupervisorId", "SupID", "Supervisor" });

            model.Employees = empDocs.Select(d => new Employee
            {
                EmployeeId = d.Id,
                FullName = GetSafeFieldValue(d, "FullName", "Name") ?? "Unknown User",
                Email = GetSafeFieldValue(d, "Email", "email") ?? "No Email",
                Phone = GetSafeFieldValue(d, "Phone", "phone") ?? "No Phone"
            }).OrderBy(e => e.FullName).ToList();
        }

        private async Task<List<ReportViewModel>> GetSupervisorReportData(string supervisorId, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
        {
            if (string.IsNullOrEmpty(supervisorId))
                return new List<ReportViewModel>();

            var empDocs = await GetEmployeeDocsForManagerAsync(supervisorId, new[] { "SupervisorId", "SupID", "Supervisor" });
            var employeeIds = empDocs.Select(d => d.Id).ToList();
            if (!employeeIds.Any()) return new List<ReportViewModel>();

            var reportData = new List<ReportViewModel>();
            const int batchSize = 10;
            for (int i = 0; i < employeeIds.Count; i += batchSize)
            {
                var batch = employeeIds.Skip(i).Take(batchSize).ToList();
                var query = _db.Collection("LeaveRequests").WhereIn("EmployeeId", batch);
                query = ApplyReportFilters(query, startDate, endDate, leaveType, status, employeeId);

                var snapshot = await query.GetSnapshotAsync();
                foreach (var doc in snapshot.Documents)
                {
                    try
                    {
                        var lr = doc.ConvertTo<LeaveRequest>();
                        var item = await ConvertToReportItem(lr);
                        if (item != null) reportData.Add(item);
                    }
                    catch { }
                }
            }

            return reportData.OrderByDescending(x => x.SubmittedAt?.ToDateTime() ?? DateTime.MinValue).ToList();
        }

        // Apply filters helper (unchanged except safe Timestamp conversion)
        private Query ApplyReportFilters(Query query, DateTime? startDate, DateTime? endDate, string leaveType, string status, string employeeId)
        {
            if (startDate.HasValue)
            {
                var startTimestamp = Timestamp.FromDateTime(startDate.Value.ToUniversalTime());
                query = query.WhereGreaterThanOrEqualTo("StartDate", startTimestamp);
            }

            if (endDate.HasValue)
            {
                var endTimestamp = Timestamp.FromDateTime(endDate.Value.ToUniversalTime());
                query = query.WhereLessThanOrEqualTo("EndDate", endTimestamp);
            }

            if (!string.IsNullOrEmpty(leaveType) && leaveType != "All")
            {
                query = query.WhereEqualTo("LeaveType", leaveType);
            }

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                query = query.WhereEqualTo("Status", status);
            }

            if (!string.IsNullOrEmpty(employeeId) && employeeId != "All")
            {
                query = query.WhereEqualTo("EmployeeId", employeeId);
            }

            return query;
        }

        // Tries Employees, Supervisors, HODs collections for the user id
        // Tries Employees, Supervisors, HODs collections for the user id
        private async Task<DocumentSnapshot?> GetUserDocumentAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;

            var collections = new[] { "Employees", "Supervisors", "HODs" };

            // 1) Try direct document id lookup (fast path)
            foreach (var collection in collections)
            {
                try
                {
                    var doc = await _db.Collection(collection).Document(userId).GetSnapshotAsync();
                    if (doc != null && doc.Exists) return doc;
                }
                catch
                {
                    // ignore and try next collection
                }
            }

            // 2) If direct lookup failed, try querying for common id field variants inside each collection
            // This covers cases where session contains a custom id (e.g. HodID, SupID) that is stored as a field, not as the doc id.
            var idFieldCandidates = new[] { "EmployeeId", "EmployeeID", "EmpId", "SupID", "SupId", "SupervisorId", "SupervisorID", "HodID", "HodId", "Uid", "UID", "UserId" };
            foreach (var collection in collections)
            {
                foreach (var idField in idFieldCandidates)
                {
                    try
                    {
                        var q = _db.Collection(collection).WhereEqualTo(idField, userId);
                        var snap = await q.Limit(1).GetSnapshotAsync();
                        if (snap != null && snap.Documents.Count > 0)
                            return snap.Documents.First();
                    }
                    catch
                    {
                        // query might fail if field doesn't exist or not indexed - ignore and continue
                    }
                }
            }

            // Not found
            return null;
        }


        // tolerant helper to find employee documents referencing a manager id in any of several field name variants
        private async Task<List<DocumentSnapshot>> GetEmployeeDocsForManagerAsync(string managerId, string[] managerFieldVariants)
        {
            var results = new List<DocumentSnapshot>();

            if (string.IsNullOrWhiteSpace(managerId) || managerFieldVariants == null || managerFieldVariants.Length == 0)
                return results;

            // Read all employees — avoids relying on exact field name indexing. If Employees is huge, consider a different approach.
            var employeesSnap = await _db.Collection("Employees").GetSnapshotAsync();

            foreach (var doc in employeesSnap.Documents)
            {
                try
                {
                    foreach (var field in managerFieldVariants)
                    {
                        if (!doc.ContainsField(field)) continue;

                        var valObj = doc.GetValue<object>(field);
                        if (valObj == null) continue;

                        var val = valObj.ToString();
                        if (string.IsNullOrWhiteSpace(val)) continue;

                        if (string.Equals(val.Trim(), managerId.Trim(), StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(doc);
                            break; // matched one variant for this employee, go to next employee doc
                        }
                    }
                }
                catch
                {
                    // ignore malformed doc and continue
                    continue;
                }
            }

            return results;
        }

        // Use this in ReportsController (paste anywhere inside the class)
        // Use this in ReportsController (replace existing GetCurrentUserDocumentAsync)
        private async Task<DocumentSnapshot?> GetCurrentUserDocumentAsync()
        {
            // 1) try session id keys first (fast, direct doc lookup or field-based lookup via GetUserDocumentAsync)
            var possibleIdKeys = new[] { "EmployeeId", "UserId", "SupID", "SupId", "HodID", "HodId", "Uid" };
            foreach (var key in possibleIdKeys)
            {
                var val = GetSessionValue(key);
                if (string.IsNullOrWhiteSpace(val)) continue;

                var doc = await GetUserDocumentAsync(val);
                if (doc != null && doc.Exists) return doc;
            }

            // 2) fallback: try email keys and query collections
            var possibleEmailKeys = new[] { "Email", "UserEmail", "userEmail", "email" };
            string? email = null;
            foreach (var ek in possibleEmailKeys)
            {
                var v = GetSessionValue(ek);
                if (!string.IsNullOrWhiteSpace(v))
                {
                    email = v.Trim(); // preserve casing — Firestore equality is case-sensitive
                    break;
                }
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                // Try direct equality query on common field names using the email exactly as stored in session.
                var collections = new[] { "Employees", "Supervisors", "HODs" };
                var commonEmailFields = new[] { "Email", "email", "EmailAddress", "EmailAddressNormalized" };

                foreach (var col in collections)
                {
                    foreach (var field in commonEmailFields)
                    {
                        try
                        {
                            var q = _db.Collection(col).WhereEqualTo(field, email);
                            var snap = await q.Limit(1).GetSnapshotAsync();
                            if (snap != null && snap.Documents.Count > 0) return snap.Documents.First();
                        }
                        catch
                        {
                            // if field doesn't exist or query fails, ignore and continue
                        }
                    }
                }

                // Last resort: fetch a small set and compare case-insensitive (safe but expensive)
                foreach (var col in collections)
                {
                    try
                    {
                        var all = await _db.Collection(col).GetSnapshotAsync();
                        var found = all.Documents.FirstOrDefault(d =>
                        {
                            try
                            {
                                var candidates = new[] { "Email", "email", "EmailAddress", "EmailAddressNormalized" };
                                foreach (var candidate in candidates)
                                {
                                    if (d.ContainsField(candidate))
                                    {
                                        var o = d.GetValue<object>(candidate);
                                        if (o != null && o.ToString().Trim().Equals(email, StringComparison.OrdinalIgnoreCase))
                                            return true;
                                    }
                                }

                                // as a final fallback check all string fields
                                foreach (var kv in d.ToDictionary())
                                {
                                    if (kv.Value == null) continue;
                                    if (kv.Value.ToString().Trim().Equals(email, StringComparison.OrdinalIgnoreCase))
                                        return true;
                                }
                            }
                            catch { }
                            return false;
                        });

                        if (found != null) return found;
                    }
                    catch { /* ignore */ }
                }
            }

            // nothing found
            return null;
        }


        // Debug helper - unchanged except kept safe
        [HttpGet]
        public async Task<IActionResult> DebugFields()
        {
            try
            {
                var result = new List<object>();

                var employeesSnapshot = await _db.Collection("Employees").Limit(3).GetSnapshotAsync();
                foreach (var doc in employeesSnapshot.Documents)
                {
                    var fields = new Dictionary<string, object>();
                    foreach (var field in doc.ToDictionary())
                    {
                        fields[field.Key] = field.Value;
                    }
                    result.Add(new { Collection = "Employees", DocumentId = doc.Id, Fields = fields });
                }

                var leaveRequestsSnapshot = await _db.Collection("LeaveRequests").Limit(3).GetSnapshotAsync();
                foreach (var doc in leaveRequestsSnapshot.Documents)
                {
                    var fields = new Dictionary<string, object>();
                    foreach (var field in doc.ToDictionary())
                    {
                        fields[field.Key] = field.Value;
                    }
                    result.Add(new { Collection = "LeaveRequests", DocumentId = doc.Id, Fields = fields });
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // -----------------------
        // Utilities
        // -----------------------
        private static string FormatTimestamp(Timestamp? t)
        {
            return t.HasValue ? t.Value.ToDateTime().ToString("yyyy-MM-dd") : "";
        }

        private static string EscapeCsv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\"", "\"\""); // basic CSV escaping
        }
    }
}

