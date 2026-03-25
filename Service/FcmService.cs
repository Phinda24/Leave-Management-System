//using Microsoft.AspNetCore.Mvc;
//using FirebaseAdmin.Messaging;
//using Google.Cloud.Firestore;
//using Leave_Management_system.Models;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Leave_Management_system.Service
//{

//    public class FcmService
//    {
//        private readonly FirestoreDb _db;

//        public FcmService(FirestoreDb db)
//        {
//            _db = db;
//        }

//        // send notification to tokens, and persist per-user notification
//        public async Task SendAndPersistNotificationAsync(
//            IEnumerable<string> userIds,
//            string title,
//            string body,
//            IDictionary<string, string>? data = null)
//        {
//            data ??= new Dictionary<string, string>();

//            // Collect tokens for all recipients
//            var tokens = new List<string>();
//            var users = new List<(string userId, List<string> userTokens)>();

//            foreach (var uid in userIds.Distinct())
//            {
//                var doc = await _db.Collection("Employees").Document(uid).GetSnapshotAsync();
//                if (!doc.Exists)
//                    continue;

//                List<string> userTokens = new List<string>();

//                if (doc.ContainsField("FcmTokens"))
//                {
//                    var o = doc.GetValue<object>("FcmTokens");
//                    if (o is IEnumerable<object> arr)
//                    {
//                        userTokens = arr.Select(x => x?.ToString()).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToList()!;
//                    }
//                }

//                // fallback: maybe single token field "FcmToken"
//                if (userTokens.Count == 0 && doc.ContainsField("FcmToken"))
//                {
//                    var t = doc.GetValue<object>("FcmToken")?.ToString();
//                    if (!string.IsNullOrEmpty(t)) userTokens.Add(t);
//                }

//                if (userTokens.Count > 0)
//                {
//                    tokens.AddRange(userTokens);
//                    users.Add((uid, userTokens));
//                }
//                else
//                {
//                    // still create a persisted notification record, but no tokens
//                    users.Add((uid, new List<string>()));
//                }
//            }

//            tokens = tokens.Distinct().ToList();

//            // 1) Send via FCM
//            if (tokens.Count > 0)
//            {
//                var message = new MulticastMessage
//                {
//                    Tokens = tokens,
//                    Notification = new Notification { Title = title, Body = body },
//                    Data = data.ToDictionary(kv => kv.Key, kv => kv.Value)
//                };

//                var result = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);

//                // Optional: log result.FailureCount and result.Responses to handle invalid tokens
//                // For broken tokens: result.Responses[i].Exception contains details
//            }

//            // 2) Persist notifications per-user into Firestore (so UI can show notification list)
//            foreach (var (userId, userTokens) in users)
//            {
//                var note = new NotificationItem
//                {
//                    Id = Guid.NewGuid().ToString(),
//                    UserId = userId,
//                    Title = title,
//                    Body = body,
//                    Read = false,
//                    CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
//                    Data = data
//                };

//                await _db.Collection("Notifications").Document(note.Id).SetAsync(note);
//            }
//        }

//        // convenience: get supervisor/hod ids for an employee doc
//        public async Task<(string? supervisorId, string? hodId)> GetSupervisorAndHodForEmployee(string employeeId)
//        {
//            var doc = await _db.Collection("Employees").Document(employeeId).GetSnapshotAsync();
//            if (!doc.Exists) return (null, null);

//            string? sup = null, hod = null;
//            if (doc.ContainsField("SupervisorId")) sup = doc.GetValue<string>("SupervisorId");
//            if (doc.ContainsField("HodID")) hod = doc.ContainsField("HodID") ? doc.GetValue<string>("HodID") : (doc.ContainsField("HodId") ? doc.GetValue<string>("HodId") : null);
//            // try other field names if your data uses different names
//            return (sup, hod);
//        }
//    }

//}
using FirebaseAdmin.Messaging;
using Google.Cloud.Firestore;
using Leave_Management_system.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_system.Service
{
    public class FcmService
    {
        private readonly FirestoreDb _db;
        private readonly ILogger<FcmService> _logger;

        public FcmService(FirestoreDb db, ILogger<FcmService> logger)
        {
            _db = db;
            _logger = logger;
        }


        /// <summary>
        /// Send FCM notification to a list of userIds (these may be employees, supervisors or hods),
        /// persist a NotificationItem to Firestore for each recipient, and prune invalid tokens.
        /// </summary>
        public async Task SendAndPersistNotificationAsync(
            IEnumerable<string> userIds,
            string title,
            string body,
            IDictionary<string, string>? data = null)
        {
            _logger.LogInformation("Sending FCM notification to {UserCount} users: {UserIds}",
        userIds.Count(), string.Join(", ", userIds));


            data ??= new Dictionary<string, string>();


            // collects token -> users mapping so we can prune per-user tokens if FCM reports them as invalid
            var tokenToUsers = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            var users = new List<(string userId, List<string> tokens)>();

            // Resolve tokens for each userId (search multiple likely collections)
            foreach (var uid in userIds.Where(u => !string.IsNullOrWhiteSpace(u)).Distinct())
            {
                var doc = await GetUserDocumentAsync(uid);
                if (doc == null || !doc.Exists)
                {
                    // Still keep user entry so we persist notification record even if no tokens exist
                    users.Add((uid, new List<string>()));
                    continue;
                }

                var userTokens = new List<string>();

                if (doc.ContainsField("FcmTokens"))
                {
                    var raw = doc.GetValue<object>("FcmTokens");
                    if (raw is IEnumerable<object> arr)
                    {
                        userTokens = arr
                            .Select(x => x?.ToString())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Distinct()
                            .ToList()!;
                    }
                }

                // fallback single token
                if (userTokens.Count == 0 && doc.ContainsField("FcmToken"))
                {
                    var t = doc.GetValue<object>("FcmToken")?.ToString();
                    if (!string.IsNullOrEmpty(t)) userTokens.Add(t);
                }

                // PASTE THIS RIGHT HERE - after token resolution
                _logger.LogDebug("Found {TokenCount} FCM tokens for user {UserId}", userTokens.Count, uid);

                foreach (var t in userTokens)
                {
                    if (!tokenToUsers.TryGetValue(t, out var list))
                    {
                        list = new List<string>();
                        tokenToUsers[t] = list;
                    }
                    list.Add(uid);
                }

                users.Add((uid, userTokens));
            }

            var allTokens = tokenToUsers.Keys.Distinct().ToList();

            // 1) Send message via FCM
            BatchResponse? batchResult = null;
            if (allTokens.Count > 0)
            {
                var multicast = new MulticastMessage
                {
                    Tokens = allTokens,
                    Notification = new Notification { Title = title, Body = body },
                    Data = data.ToDictionary(kv => kv.Key, kv => kv.Value)
                };

                try
                {
                    batchResult = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(multicast);

                    if (batchResult?.SuccessCount > 0)
                    {
                        _logger.LogInformation("Successfully sent FCM notification to {SuccessCount} devices",
                            batchResult.SuccessCount);
                    }
                }
                catch (Exception ex)
                {

                    _logger.LogError(ex, "Failed to send FCM notification to {TokenCount} tokens for users: {UserIds}",
                        allTokens.Count, string.Join(", ", userIds));
                    // If sending fails entirely, we still persist notifications (no tokens delivered)
                    batchResult = null;
                }
            }

            // 2) If some tokens failed, prune them from their user documents
            if (batchResult != null && batchResult.FailureCount > 0)
            {
                // iterate responses and map failures to tokens by index
                var responses = batchResult.Responses;
                for (int i = 0; i < responses.Count && i < allTokens.Count; i++)
                {
                    var resp = responses[i];
                    bool isFailure = false;

                    // Common pattern: Check for an exception on the response. Different SDK versions
                    // may expose different properties; usually response.Exception != null indicates a failure.
                    try
                    {
                        // Prefer response.Exception if available
                        var exceptionProp = resp.GetType().GetProperty("Exception");
                        if (exceptionProp != null)
                        {
                            var exVal = exceptionProp.GetValue(resp);
                            if (exVal != null) isFailure = true;
                        }
                        else
                        {
                            // Fallback: check Success/IsSuccess properties if present
                            var successProp = resp.GetType().GetProperty("Success") ?? resp.GetType().GetProperty("IsSuccess");
                            if (successProp != null)
                            {
                                var ok = successProp.GetValue(resp);
                                if (ok is bool b && !b) isFailure = true;
                            }
                        }
                    }
                    catch
                    {
                        // If reflection fails, don't crash the loop — assume it might be success
                        isFailure = false;
                    }

                    if (!isFailure) continue;

                    var badToken = allTokens[i];
                    if (!tokenToUsers.TryGetValue(badToken, out var affectedUsers)) continue;

                    // For each affected user remove the bad token
                    foreach (var u in affectedUsers)
                    {
                        try
                        {
                            // Determine which collection the user lives in and remove the token
                            var userDoc = await GetUserDocumentAsync(u);
                            if (userDoc != null && userDoc.Exists)
                            {
                                // Build a DocumentReference for the doc and remove token
                                // We need the doc's reference path: it's available via userDoc.Reference
                                await userDoc.Reference.UpdateAsync(new Dictionary<string, object>
                                {
                                    { "FcmTokens", FieldValue.ArrayRemove(badToken) }
                                });
                            }
                        }
                        catch (Exception pruneEx)
                        {
                            _logger.LogWarning(pruneEx, "Failed to prune invalid FCM token {BadToken} for user {UserId}", badToken, u);
                        }
                    }
                }
            }

            // 3) Persist notification per user
            foreach (var (userId, tokens) in users)
            {
                var note = new NotificationItem
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = title,
                    Body = body,
                    Read = false,
                    CreatedAt = Timestamp.FromDateTime(DateTime.UtcNow),
                    Data = data
                };

                try
                {
                    await _db.Collection("Notifications").Document(note.Id).SetAsync(note);
                }
                catch (Exception persistEx)
                {
                    _logger.LogError(persistEx, "Failed to persist notification for user {UserId}", userId);
                }
            }
        }

        /// <summary>
        /// Try to load a user document from the known collections in order.
        /// Returns DocumentSnapshot or null if not found.
        /// </summary>
        private async Task<DocumentSnapshot?> GetUserDocumentAsync(string userId)
        {
            // Try collections in this order: Employees, Supervisors, Hods
            var colNames = new[] { "Employees", "Supervisors", "HODs" };
            foreach (var c in colNames)
            {
                var docRef = _db.Collection(c).Document(userId);
                try
                {
                    var snap = await docRef.GetSnapshotAsync();
                    if (snap.Exists) return snap;
                }
                catch (Exception lookupEx)
                {
                    _logger.LogDebug(lookupEx, "Failed to lookup user {UserId} in collection {Collection}", userId, c);
                }
            }
            return null;
        }

        /// <summary>
        /// Convenience: if you ever need the supervisor/hod for an employee.
        /// </summary>
        public async Task<(string? supervisorId, string? hodId)> GetSupervisorAndHodForEmployee(string employeeId)
        {
            var doc = await _db.Collection("Employees").Document(employeeId).GetSnapshotAsync();
            if (!doc.Exists) return (null, null);

            string? sup = null, hod = null;
            if (doc.ContainsField("SupervisorId")) sup = doc.GetValue<string>("SupervisorId");
            if (doc.ContainsField("HodID")) hod = doc.GetValue<string>("HodID");
            else if (doc.ContainsField("HodId")) hod = doc.GetValue<string>("HodId");
            return (sup, hod);
        }
    }
}
