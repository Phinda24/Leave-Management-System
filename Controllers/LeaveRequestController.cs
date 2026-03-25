
using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Leave_Management_system.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_system.Controllers
{
    public class LeaveRequestController : Controller
    {
        private readonly FirestoreDb _db;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly FcmService _fcmService;

        public LeaveRequestController(FirestoreDb db, IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor, FcmService fcmService)
        {
            _db = db;
            _env = env;
            _httpContextAccessor = httpContextAccessor;
            _fcmService = fcmService;
        }

        // ✅ Helper method for session access (updated)
        private string? GetSessionValue(string key)
        {
            // Try the requested key first
            var value = _httpContextAccessor.HttpContext?.Session?.GetString(key);
            if (!string.IsNullOrEmpty(value)) return value;

            // Fallback keys commonly used for different login flows (HOD, Supervisor, Employee, Firebase)
            var fallbackKeys = new[] { "EmployeeId", "HodID", "UserId", "Uid", "UserEmail", "Email" };

            foreach (var k in fallbackKeys)
            {
                var v = _httpContextAccessor.HttpContext?.Session?.GetString(k);
                if (!string.IsNullOrEmpty(v)) return v;
            }

            return null;
        }

        private (string? id, string? email) GetSessionIdAndEmail()
        {
            var id = GetSessionValue("EmployeeId");
            var email = GetSessionValue("Email");
            return (id, email);
        }

        public async Task<IActionResult> Dashboard(string? employeeId)
        {
            var pair = GetSessionIdAndEmail();
            employeeId ??= pair.id;
            var employeeEmail = pair.email;

            if (string.IsNullOrEmpty(employeeId) && string.IsNullOrEmpty(employeeEmail))
                return RedirectToAction("Login", "Auth");

            // Try by id first
            QuerySnapshot snapshots;
            if (!string.IsNullOrEmpty(employeeId))
            {
                snapshots = await _db.Collection("LeaveRequests")
                                     .WhereEqualTo("EmployeeId", employeeId)
                                     .OrderByDescending("StartDate")
                                     .Limit(5)
                                     .GetSnapshotAsync();
            }
            else
            {
                // fallback by email
                snapshots = await _db.Collection("LeaveRequests")
                                     .WhereEqualTo("EmployeeEmail", employeeEmail)
                                     .OrderByDescending("StartDate")
                                     .Limit(5)
                                     .GetSnapshotAsync();
            }

            var recentLeaves = snapshots.Documents.Select(d => d.ConvertTo<LeaveRequest>()).ToList();
            ViewBag.RecentLeaves = recentLeaves;

            // Employee info (try finding employee doc either by id or email)
            DocumentSnapshot employeeDoc = null;
            if (!string.IsNullOrEmpty(employeeId))
                employeeDoc = await _db.Collection("Employees").Document(employeeId).GetSnapshotAsync();

            if ((employeeDoc == null || !employeeDoc.Exists) && !string.IsNullOrEmpty(employeeEmail))
            {
                // try lookup by email
                var q = await _db.Collection("Employees").WhereEqualTo("Email", employeeEmail).Limit(1).GetSnapshotAsync();
                if (q.Documents.Count > 0) employeeDoc = q.Documents.First();
            }

            if (employeeDoc == null || !employeeDoc.Exists)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToAction("Login", "Auth");
            }

            var employee = employeeDoc.ConvertTo<Employee>();
            return View(employee);
        }

        [HttpGet]
        public async Task<IActionResult> MyApplications(string? employeeId)
        {
            try
            {
                var pair = GetSessionIdAndEmail();
                employeeId ??= pair.id;
                var employeeEmail = pair.email;

                if (string.IsNullOrEmpty(employeeId) && string.IsNullOrEmpty(employeeEmail))
                    return RedirectToAction("Login", "Auth");

                // Which statuses we consider "pending" for an employee
                var pendingStatuses = new[] { StatusValues.Pending, StatusValues.PendingHod };

                Query query;
                QuerySnapshot snapshot;

                if (!string.IsNullOrEmpty(employeeId))
                {
                    // Prefer EmployeeId; filter to only pending statuses
                    query = _db.Collection("LeaveRequests")
                               .WhereEqualTo("EmployeeId", employeeId)
                               .WhereIn("Status", pendingStatuses);
                    snapshot = await query.GetSnapshotAsync();

                    // If nothing found by id, try fallback by email
                    if (snapshot.Documents.Count == 0 && !string.IsNullOrEmpty(employeeEmail))
                    {
                        query = _db.Collection("LeaveRequests")
                                   .WhereEqualTo("EmployeeEmail", employeeEmail)
                                   .WhereIn("Status", pendingStatuses);
                        snapshot = await query.GetSnapshotAsync();
                    }
                }
                else
                {
                    // No id -> query by email
                    query = _db.Collection("LeaveRequests")
                               .WhereEqualTo("EmployeeEmail", employeeEmail)
                               .WhereIn("Status", pendingStatuses);
                    snapshot = await query.GetSnapshotAsync();
                }

                var leaveRequests = snapshot.Documents
                    .Select(d => d.ConvertTo<LeaveRequest>())
                    .OrderByDescending(l => l.StartDate?.ToDateTime())
                    .ToList();

                return View(leaveRequests);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading leave applications: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        // ✅ GET: Apply for Leave
        [HttpGet]
        public IActionResult Apply()
        {

            var leave = new LeaveRequest
            {
                EmployeeId = GetSessionValue("EmployeeId"),
                EmployeeEmail = GetSessionValue("Email"),
                EmployeeName = GetSessionValue("FullName")
            };

            return View(leave);
        }

        // ✅ POST: Apply for Leave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(IFormFile SupportingDocument, string LeaveType, DateTime StartDate, DateTime EndDate, bool HalfDay, string Reason, string Comment)
        {

            try
            {
                string employeeEmail = GetSessionValue("Email");
                string employeeName = GetSessionValue("FullName");
                string employeeId = GetSessionValue("EmployeeId");

                if (string.IsNullOrEmpty(employeeId))
                {
                    TempData["ErrorMessage"] = "Session expired. Please log in again.";
                    return RedirectToAction("Login", "Auth");
                }

                string fileUrl = null;
                if (SupportingDocument != null && SupportingDocument.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                        Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = $"{Guid.NewGuid()}_{SupportingDocument.FileName}";
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await SupportingDocument.CopyToAsync(stream);
                    }

                    fileUrl = "/uploads/" + uniqueFileName;
                }

                // Treat StartDate/EndDate as date-only (no time) and store as UTC midnight for that date
                var startUtcDate = new DateTime(StartDate.Year, StartDate.Month, StartDate.Day, 0, 0, 0, DateTimeKind.Utc);
                var endUtcDate = new DateTime(EndDate.Year, EndDate.Month, EndDate.Day, 0, 0, 0, DateTimeKind.Utc);


                var leaveRequest = new LeaveRequest
                {
                    Id = Guid.NewGuid().ToString(),
                    EmployeeId = employeeId,
                    EmployeeEmail = employeeEmail,
                    EmployeeName = employeeName,
                    LeaveType = LeaveType,
                    Reason = Reason,
                    Comment = Comment,
                    //StartDate = Timestamp.FromDateTime(StartDate.ToUniversalTime()),
                    //EndDate = Timestamp.FromDateTime(EndDate.ToUniversalTime()),
                    StartDate = Timestamp.FromDateTime(startUtcDate),
                    EndDate = Timestamp.FromDateTime(endUtcDate),
                    IsHalfDay = HalfDay,
                    Status = "Pending",
                    DocumentUrl = fileUrl,
                    SubmittedAt = Timestamp.FromDateTime(DateTime.UtcNow)
                };

                await _db.Collection("LeaveRequests").Document(leaveRequest.Id).SetAsync(leaveRequest);

                // Attempt to get supervisor & hod ids for this employee
                try
                {
                    var (supervisorId, hodId) = await _fcmService.GetSupervisorAndHodForEmployee(employeeId);
                    var recipients = new List<string>();
                    if (!string.IsNullOrEmpty(supervisorId)) recipients.Add(supervisorId);
                    if (!string.IsNullOrEmpty(hodId) && hodId != supervisorId) recipients.Add(hodId);

                    // If none available, you may optionally send to a topic (e.g. "supervisors") - left as optional.

                    if (recipients.Any())
                    {
                        var title = "New Leave Request";
                        var body = $"{employeeName} submitted a leave request ({LeaveType}) from {StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}.";
                        var data = new Dictionary<string, string>
                        {
                            { "leaveId", leaveRequest.Id },
                            { "employeeId", leaveRequest.EmployeeId },
                            { "action", "new_leave" }
                        };

                        // run fire-and-forget (but await ensures operation happens before redirect; you can remove await if you prefer background)
                        await _fcmService.SendAndPersistNotificationAsync(recipients, title, body, data);
                    }
                }
                catch
                {
                    // swallowing exceptions here so we still return success if notifications fail;
                    // in production log this error
                    TempData["SuccessMessage"] = "✅ Leave request submitted successfully!";
                    return RedirectToAction("MyApplications", new { employeeId = employeeId });
                }


                TempData["SuccessMessage"] = "✅ Leave request submitted successfully!";
                return RedirectToAction("MyApplications", new { employeeId = employeeId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error submitting leave request: {ex.Message}";
                return View();
            }
        }

        public async Task<IActionResult> History(string? employeeId)
        {
            try
            {
                var pair = GetSessionIdAndEmail();
                employeeId ??= pair.id;
                var employeeEmail = pair.email;

                if (string.IsNullOrEmpty(employeeId) && string.IsNullOrEmpty(employeeEmail))
                    return RedirectToAction("Login", "Auth");

                // Show completed/cancelled statuses in history
                var historyStatuses = new[] { StatusValues.Approved, StatusValues.Rejected, "Cancelled" };

                Query query;
                QuerySnapshot snapshot;

                if (!string.IsNullOrEmpty(employeeId))
                {
                    query = _db.Collection("LeaveRequests")
                               .WhereEqualTo("EmployeeId", employeeId)
                               .WhereIn("Status", historyStatuses);
                    snapshot = await query.GetSnapshotAsync();

                    // fallback to email if nothing found by id
                    if (snapshot.Documents.Count == 0 && !string.IsNullOrEmpty(employeeEmail))
                    {
                        query = _db.Collection("LeaveRequests")
                                   .WhereEqualTo("EmployeeEmail", employeeEmail)
                                   .WhereIn("Status", historyStatuses);
                        snapshot = await query.GetSnapshotAsync();
                    }
                }
                else
                {
                    query = _db.Collection("LeaveRequests")
                               .WhereEqualTo("EmployeeEmail", employeeEmail)
                               .WhereIn("Status", historyStatuses);
                    snapshot = await query.GetSnapshotAsync();
                }

                var leaveRequests = snapshot.Documents
                    .Select(d => d.ConvertTo<LeaveRequest>())
                    .OrderByDescending(l => l.StartDate?.ToDateTime())
                    .ToList();

                return View(leaveRequests);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading leave history: {ex.Message}";
                return RedirectToAction("Dashboard");
            }
        }

        public async Task<IActionResult> Approvals()
        {
            var role = (HttpContext.Session.GetString("UserRole") ?? string.Empty).Trim();

            Query query;
            if (string.Equals(role, "HOD", StringComparison.OrdinalIgnoreCase))
            {
                query = _db.Collection("LeaveRequests").WhereEqualTo("Status", StatusValues.PendingHod);
            }
            else // Supervisor or default
            {
                query = _db.Collection("LeaveRequests").WhereEqualTo("Status", StatusValues.Pending);
            }

            QuerySnapshot docs = await query.GetSnapshotAsync();
            var requests = docs.Documents.Select(d => d.ConvertTo<LeaveRequest>()).ToList();
            return View(requests);
        }
        [HttpGet]
        public async Task<IActionResult> DebugRequest(string id)
        {
            if (string.IsNullOrEmpty(id)) return BadRequest("Missing id");

            var doc = await _db.Collection("LeaveRequests").Document(id).GetSnapshotAsync();
            if (!doc.Exists) return NotFound();

            var lr = doc.ConvertTo<LeaveRequest>();

            // Safe formatting for nullable timestamps
            string SafeFormat(Timestamp? ts) => ts.HasValue ? ts.Value.ToDateTime().ToString("o") : null;

            return Json(new
            {
                lr.Id,
                lr.Status,
                lr.SupervisorApproved,
                lr.SupervisorId,
                SupervisorApprovedAt = SafeFormat(lr.SupervisorApprovedAt),
                lr.SupervisorReason,
                lr.HodApproved,
                lr.HodId,
                HodApprovedAt = SafeFormat(lr.HodApprovedAt),
                lr.HodReason,
                ApprovalHistory = lr.ApprovalHistory?.Select(h => new { h.Level, h.Action, h.By, At = (h.At.HasValue ? h.At.Value.ToDateTime().ToString("o") : null), h.Reason })
            });
        }


        public async Task<IActionResult> Pending()
        {
            var role = (HttpContext.Session.GetString("UserRole") ?? string.Empty).Trim();

            if (string.Equals(role, "HOD", StringComparison.OrdinalIgnoreCase))
            {
                // HOD wants the items that are approved by Supervisor and waiting for HOD
                var query = _db.Collection("LeaveRequests").WhereEqualTo("Status", StatusValues.PendingHod);
                var snapshot = await query.GetSnapshotAsync();
                var list = snapshot.Documents.Select(d => d.ConvertTo<Leave_Management_system.Models.LeaveRequest>()).ToList();

                ViewData["Layout"] = "~/Views/Shared/_HodLayout.cshtml";
                return View("PendingHod", list);
            }

            if (string.Equals(role, "Supervisor", StringComparison.OrdinalIgnoreCase))
            {
                // Supervisor wants the requests that are still at supervisor stage
                var query = _db.Collection("LeaveRequests").WhereEqualTo("Status", StatusValues.Pending);
                var snapshot = await query.GetSnapshotAsync();
                var list = snapshot.Documents.Select(d => d.ConvertTo<Leave_Management_system.Models.LeaveRequest>()).ToList();

                ViewData["Layout"] = "~/Views/Shared/_SupLayout.cshtml";
                return View("PendingSupervisor", list);
            }

            // fallback: no role
            return Content($"UserRole was '{role}'. Couldn't determine whether to show HOD or Supervisor view.");
        }

        //Put this inside your controller class (outside UpdateStatus) — a private helper method
        private double CalculateBusinessDays(DateTime start, DateTime end, bool isHalfDay, HashSet<DateTime>? holidaySet)
        {
            if (isHalfDay) return 0.5;

            var s = start.Date;
            var e = end.Date;
            if (e < s) return 0;

            double businessDays = 0;
            for (var d = s; d <= e; d = d.AddDays(1))
            {
                var dow = d.DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) continue;
                if (holidaySet != null && holidaySet.Contains(d)) continue;
                businessDays += 1;
            }
            return businessDays;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, string operation, string reason)
        {
            operation = (operation ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(id))
                return BadRequest("Missing request id.");

            if (operation != "approve" && operation != "reject")
                return BadRequest("Unknown operation. Expected 'approve' or 'reject'.");

            var role = (HttpContext.Session.GetString("UserRole") ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(role))
                return Unauthorized("Missing user role in session.");

            var docRef = _db.Collection("LeaveRequests").Document(id);

            try
            {
                // --- Read public holidays (unchanged) ---
                var publicHolidays = new HashSet<DateTime>();
                try
                {
                    var holSnap = await _db.Collection("PublicHolidays").GetSnapshotAsync();
                    foreach (var hdoc in holSnap.Documents)
                    {
                        DateTime? hd = null;
                        foreach (var fname in new[] { "Date", "HolidayDate", "Holiday", "Day", "ObservedDate" })
                        {
                            if (!hdoc.ContainsField(fname)) continue;
                            try
                            {
                                var o = hdoc.GetValue<object>(fname);
                                if (o is Google.Cloud.Firestore.Timestamp ts) hd = ts.ToDateTime().Date;
                                else if (DateTime.TryParse(o?.ToString(), out var parsed)) hd = parsed.Date;
                            }
                            catch { }
                            if (hd.HasValue) break;
                        }
                        if (hd.HasValue) publicHolidays.Add(hd.Value);
                    }
                }
                catch { }

                await _db.RunTransactionAsync(async transaction =>
                {
                    var snap = await transaction.GetSnapshotAsync(docRef);
                    if (!snap.Exists)
                        throw new Exception("Leave request not found.");

                    var lr = snap.ConvertTo<LeaveRequest>();

                    if (lr.ApprovalHistory == null)
                        lr.ApprovalHistory = new List<ApprovalRecord>();

                    var now = Timestamp.FromDateTime(DateTime.UtcNow);

                    var userId = HttpContext.Session.GetString("SupID")
                                 ?? HttpContext.Session.GetString("HodID")
                                 ?? HttpContext.Session.GetString("EmployeeId")
                                 ?? HttpContext.Session.GetString("Uid")
                                 ?? HttpContext.Session.GetString("FullName")
                                 ?? "unknown";

                    var level = role.Equals("HOD", StringComparison.OrdinalIgnoreCase) ? "HOD" : "Supervisor";
                    var recordAction = operation == "approve" ? "Approved" : "Rejected";

                    var newRecord = new ApprovalRecord
                    {
                        Level = level,
                        Action = recordAction,
                        By = userId,
                        Reason = reason ?? string.Empty,
                        At = now
                    };
                    lr.ApprovalHistory.Add(newRecord);

                    var updates = new Dictionary<string, object>
                    {
                        { "ApprovalHistory", lr.ApprovalHistory }
                    };

                    if (operation == "approve")
                    {
                        if (level == "Supervisor")
                        {
                            if (!string.Equals(lr.Status, StatusValues.Pending, StringComparison.OrdinalIgnoreCase))
                                throw new InvalidOperationException("Request is not in a state that Supervisor can approve.");

                            updates["SupervisorApproved"] = true;
                            updates["SupervisorId"] = userId;
                            updates["SupervisorApprovedAt"] = now;
                            updates["SupervisorReason"] = reason ?? string.Empty;
                            updates["Status"] = StatusValues.PendingHod;
                        }
                        else // HOD final approval
                        {
                            if (!(string.Equals(lr.Status, StatusValues.PendingHod, StringComparison.OrdinalIgnoreCase)
                                  || string.Equals(lr.Status, StatusValues.Pending, StringComparison.OrdinalIgnoreCase)))
                            {
                                throw new InvalidOperationException("Request is not in a state that HOD can approve.");
                            }

                            if (lr.HodApproved)
                                throw new InvalidOperationException("This request has already been approved by HOD.");

                            // --- SAFE: ensure StartDate and EndDate exist ---
                            var start = lr.StartDate?.ToDateTime();
                            var end = lr.EndDate?.ToDateTime();
                            if (!start.HasValue || !end.HasValue)
                                throw new InvalidOperationException("Leave request is missing StartDate or EndDate.");

                            DateTime startDt = start.Value;
                            DateTime endDt = end.Value;

                            double daysToDeduct = CalculateBusinessDays(startDt, endDt, lr.IsHalfDay, publicHolidays);

                            var lt = (lr.LeaveType ?? "").Trim().ToLowerInvariant();
                            string takenField = "AnnualLeaveTaken";
                            string balanceField = "AnnualLeaveBalance";

                            if (lt.Contains("sick"))
                            {
                                takenField = "SickLeaveTaken";
                                balanceField = "SickLeaveBalance";
                            }
                            else if (lt.Contains("study"))
                            {
                                takenField = "StudyLeaveTaken";
                                balanceField = "StudyLeaveBalance";
                            }
                            else if (lt.Contains("family") || lt.Contains("familyresponsibility") || lt.Contains("family responsibility"))
                            {
                                takenField = "FamilyLeaveTaken";
                                balanceField = "FamilyLeaveBalance";
                            }

                            var balancesRef = _db.Collection("LeaveBalances").Document(lr.EmployeeId);
                            var balSnap = await transaction.GetSnapshotAsync(balancesRef);

                            if (!balSnap.Exists)
                            {
                                var newFields = new Dictionary<string, object>
                                {
                                    { "EmployeeId", lr.EmployeeId },
                                    { takenField, daysToDeduct },
                                    { balanceField, FieldValue.Increment(-daysToDeduct) }
                                };
                                transaction.Set(balancesRef, newFields, SetOptions.MergeAll);
                            }
                            else
                            {
                                transaction.Update(balancesRef, new Dictionary<string, object>
                                {
                                    { takenField, FieldValue.Increment(daysToDeduct) },
                                    { balanceField, FieldValue.Increment(-daysToDeduct) }
                                });
                            }

                            updates["HodApproved"] = true;
                            updates["HodId"] = userId;
                            updates["HodApprovedAt"] = now;
                            updates["HodReason"] = reason ?? string.Empty;
                            updates["Status"] = StatusValues.Approved;
                        }
                    }
                    else // reject
                    {
                        updates["Status"] = StatusValues.Rejected;

                        if (level == "Supervisor")
                        {
                            updates["SupervisorApproved"] = false;
                            updates["SupervisorId"] = userId;
                            updates["SupervisorApprovedAt"] = now;
                            updates["SupervisorReason"] = reason ?? string.Empty;
                        }
                        else
                        {
                            updates["HodApproved"] = false;
                            updates["HodId"] = userId;
                            updates["HodApprovedAt"] = now;
                            updates["HodReason"] = reason ?? string.Empty;
                        }
                    }

                    transaction.Update(docRef, updates);
                });

                // Notify employee (safe formatting)
                try
                {
                    var doc = await _db.Collection("LeaveRequests").Document(id).GetSnapshotAsync();
                    if (doc.Exists)
                    {
                        var lr = doc.ConvertTo<LeaveRequest>();

                        string FormatDate(Timestamp? t) => t.HasValue ? t.Value.ToDateTime().ToString("yyyy-MM-dd") : "N/A";

                        var employeeToNotify = lr.EmployeeId;
                        var title = operation == "approve" ? "Leave Approved" : "Leave Rejected";
                        var body = operation == "approve"
                            ? $"Your leave request ({lr.LeaveType}) from {FormatDate(lr.StartDate)} to {FormatDate(lr.EndDate)} was approved."
                            : $"Your leave request ({lr.LeaveType}) from {FormatDate(lr.StartDate)} to {FormatDate(lr.EndDate)} was rejected. Reason: {reason}";

                        var data = new Dictionary<string, string>
                        {
                            { "leaveId", lr.Id },
                            { "action", operation == "approve" ? "approved" : "rejected" }
                        };

                        await _fcmService.SendAndPersistNotificationAsync(new[] { employeeToNotify }, title, body, data);
                    }
                }
                catch
                {
                    // log in production
                }

                TempData["Success"] = "Request updated.";
                return RedirectToAction("Pending");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Could not update status: {ex.Message}";
                return RedirectToAction("Pending");
            }
        }


        [HttpGet]
        public async Task<IActionResult> LeaveBalances(string? employeeId)
        {
            try
            {
                employeeId ??= GetSessionValue("EmployeeId");
                if (string.IsNullOrEmpty(employeeId))
                    return RedirectToAction("Login", "Auth");

                DocumentSnapshot doc = await _db.Collection("LeaveBalances").Document(employeeId).GetSnapshotAsync();
                if (!doc.Exists)
                {
                    TempData["ErrorMessage"] = "No leave balance data found for this employee in Firestore.";
                    return RedirectToAction("Dashboard");
                }

                // Helper to read numeric fields from many possible names, falling back to 0
                double GetDoubleFromDoc(DocumentSnapshot snapshot, params string[] candidateNames)
                {
                    foreach (var name in candidateNames)
                    {
                        if (!snapshot.ContainsField(name)) continue;
                        try
                        {
                            var obj = snapshot.GetValue<object>(name);
                            if (obj == null) continue;

                            // Firestore may return long/int/double or string — handle common cases
                            if (obj is double d) return d;
                            if (obj is float f) return Convert.ToDouble(f);
                            if (obj is long l) return Convert.ToDouble(l);
                            if (obj is int i) return Convert.ToDouble(i);
                            if (double.TryParse(obj.ToString(), out var parsed)) return parsed;
                        }
                        catch
                        {
                            // ignore and try next
                        }
                    }
                    return 0.0;
                }

                // Read both "Balance" and "Entitlement" and "Taken" possibilities
                var annualEntitlement = GetDoubleFromDoc(doc, "AnnualLeaveEntitlement", "AnnualLeaveBalance", "AnnualLeaveAvailable");
                var annualTaken = GetDoubleFromDoc(doc, "AnnualLeaveTaken", "AnnualLeaveToken", "AnnualLeaveUsed", "AnnualLeaveTokens");
                var annualBalance = GetDoubleFromDoc(doc, "AnnualLeaveBalance", "AnnualLeaveAvailable");

                var sickEntitlement = GetDoubleFromDoc(doc, "SickLeaveEntitlement", "SickLeaveBalance", "SickLeaveAvailable");
                var sickTaken = GetDoubleFromDoc(doc, "SickLeaveTaken", "SickLeaveUsed");
                var sickBalance = GetDoubleFromDoc(doc, "SickLeaveBalance", "SickLeaveAvailable");

                var familyEntitlement = GetDoubleFromDoc(doc, "FamilyLeaveEntitlement", "FamilyLeaveBalance", "FamilyLeaveAvailable");
                var familyTaken = GetDoubleFromDoc(doc, "FamilyLeaveTaken", "FamilyLeaveUsed");
                var familyBalance = GetDoubleFromDoc(doc, "FamilyLeaveBalance", "FamilyLeaveAvailable");

                var studyEntitlement = GetDoubleFromDoc(doc, "StudyLeaveEntitlement", "StudyLeaveBalance", "StudyLeaveAvailable");
                var studyTaken = GetDoubleFromDoc(doc, "StudyLeaveTaken", "StudyLeaveUsed");
                var studyBalance = GetDoubleFromDoc(doc, "StudyLeaveBalance", "StudyLeaveAvailable");

                // If entitlement is missing but balance is present, compute entitlement = balance + taken
                if (annualEntitlement <= 0 && annualBalance > 0) annualEntitlement = annualBalance + annualTaken;
                if (sickEntitlement <= 0 && sickBalance > 0) sickEntitlement = sickBalance + sickTaken;
                if (familyEntitlement <= 0 && familyBalance > 0) familyEntitlement = familyBalance + familyTaken;
                if (studyEntitlement <= 0 && studyBalance > 0) studyEntitlement = studyBalance + studyTaken;

                // Final clamps (no negatives)
                annualEntitlement = Math.Max(0, annualEntitlement);
                annualTaken = Math.Max(0, annualTaken);

                sickEntitlement = Math.Max(0, sickEntitlement);
                sickTaken = Math.Max(0, sickTaken);

                familyEntitlement = Math.Max(0, familyEntitlement);
                familyTaken = Math.Max(0, familyTaken);

                studyEntitlement = Math.Max(0, studyEntitlement);
                studyTaken = Math.Max(0, studyTaken);

                // Build view model
                var model = new LeaveBalanceViewModel
                {
                    EmployeeId = GetStringSafely(doc, "EmployeeId"),
                    FullName = GetStringSafely(doc, "FullName", "Fullname", "EmployeeName"),
                    Email = GetStringSafely(doc, "Email"),
                    Phone = GetStringSafely(doc, "Phone"),
                    ProfileImageUrl = GetStringSafely(doc, "ProfileImageUrl"),

                    AnnualLeaveEntitlement = annualEntitlement,
                    AnnualLeaveTaken = annualTaken,

                    SickLeaveEntitlement = sickEntitlement,
                    SickLeaveTaken = sickTaken,

                    FamilyLeaveEntitlement = familyEntitlement,
                    FamilyLeaveTaken = familyTaken,

                    StudyLeaveEntitlement = studyEntitlement,
                    StudyLeaveTaken = studyTaken,
                };


                if (doc.ContainsField("StartDate"))
                {
                    var o = doc.GetValue<object>("StartDate");
                    if (o is Timestamp ts) model.StartDate = ts.ToDateTime().Date;
                    else if (DateTime.TryParse(o?.ToString(), out var sd)) model.StartDate = sd.Date;
                }
                if (doc.ContainsField("EndDate"))
                {
                    var o = doc.GetValue<object>("EndDate");
                    if (o is Timestamp ts) model.EndDate = ts.ToDateTime().Date;
                    else if (DateTime.TryParse(o?.ToString(), out var ed)) model.EndDate = ed.Date;
                }

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error loading leave balances: {ex.Message}";
                return RedirectToAction("Dashboard");
            }

            // small local helper to get strings
            string GetStringSafely(DocumentSnapshot snap, params string[] names)
            {
                foreach (var n in names)
                {
                    if (!snap.ContainsField(n)) continue;
                    try
                    {
                        var v = snap.GetValue<object>(n);
                        if (v != null) return v.ToString();
                    }
                    catch { /* ignore */ }
                }
                return "";
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(string id, string? reason)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Missing id.");

            // determine current user (employee) from session
            var employeeId = GetSessionValue("EmployeeId") ?? HttpContext.Session.GetString("EmployeeId");
            if (string.IsNullOrEmpty(employeeId))
                return Unauthorized("Session expired. Please log in.");

            var docRef = _db.Collection("LeaveRequests").Document(id);

            try
            {
                // run transaction to validate & update atomically
                await _db.RunTransactionAsync(async transaction =>
                {
                    var snap = await transaction.GetSnapshotAsync(docRef);
                    if (!snap.Exists)
                        throw new InvalidOperationException("Leave request not found.");

                    var lr = snap.ConvertTo<LeaveRequest>();

                    // ensure only owner can cancel
                    if (!string.Equals(lr.EmployeeId, employeeId, StringComparison.OrdinalIgnoreCase))
                        throw new UnauthorizedAccessException("You can only cancel your own leave requests.");

                    var currentStatus = (lr.Status ?? "").Trim();

                    // Prevent cancelling if already finalised
                    if (string.Equals(currentStatus, StatusValues.Approved, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(currentStatus, StatusValues.Rejected, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(currentStatus, "Cancelled", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(currentStatus, "Canceled", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("This request cannot be cancelled in its current state.");
                    }

                    // ensure ApprovalHistory exists
                    if (lr.ApprovalHistory == null)
                        lr.ApprovalHistory = new List<ApprovalRecord>();

                    var now = Timestamp.FromDateTime(DateTime.UtcNow);

                    var userId = employeeId; // the canceller is the employee
                    var cancelRecord = new ApprovalRecord
                    {
                        Level = "Employee",
                        Action = "Cancelled",
                        By = userId,
                        Reason = reason ?? string.Empty,
                        At = now
                    };
                    lr.ApprovalHistory.Add(cancelRecord);

                    // prepare updates
                    var updates = new Dictionary<string, object>
            {
                { "Status", "Cancelled" },
                { "CancelledAt", now },
                { "CancelReason", reason ?? string.Empty },
                { "ApprovalHistory", lr.ApprovalHistory }
            };

                    transaction.Update(docRef, updates);
                });

                // Notify supervisor/HOD (best-effort; don't block on notification failures)
                try
                {
                    var (supervisorId, hodId) = await _fcmService.GetSupervisorAndHodForEmployee(employeeId);
                    var recipients = new List<string>();
                    if (!string.IsNullOrEmpty(supervisorId)) recipients.Add(supervisorId);
                    if (!string.IsNullOrEmpty(hodId) && hodId != supervisorId) recipients.Add(hodId);

                    if (recipients.Any())
                    {
                        var employeeName = HttpContext.Session.GetString("FullName") ?? "An employee";
                        var title = "Leave Cancelled";
                        var body = $"{employeeName} cancelled a leave request.";
                        var data = new Dictionary<string, string>
                {
                    { "leaveId", id },
                    { "action", "cancelled" }
                };

                        await _fcmService.SendAndPersistNotificationAsync(recipients, title, body, data);
                    }
                }
                catch
                {
                    // in production: log the notification exception
                }

                TempData["SuccessMessage"] = "Leave request cancelled successfully.";
                return RedirectToAction("MyApplications", new { employeeId = employeeId });
            }
            catch (Exception ex)
            {
                // surface a friendly error
                TempData["ErrorMessage"] = $"Could not cancel leave request: {ex.Message}";
                return RedirectToAction("MyApplications", new { employeeId = employeeId });
            }
        }


      

    }
}
