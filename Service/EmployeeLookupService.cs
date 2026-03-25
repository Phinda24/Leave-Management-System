using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Leave_Management_system.Services
{
    public class EmployeeLookupService
    {
        private readonly FirestoreDb _db;

        public EmployeeLookupService(FirestoreDb db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        // tolerant helper to find employee documents referencing a manager id in any of several field name variants
        public async Task<List<DocumentSnapshot>> GetEmployeeDocsForManagerAsync(string managerId, string[] managerFieldVariants)
        {
            var results = new List<DocumentSnapshot>();
            if (string.IsNullOrWhiteSpace(managerId) || managerFieldVariants == null || managerFieldVariants.Length == 0)
                return results;

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
                            break;
                        }
                    }
                }
                catch
                {
                    // ignore malformed doc and continue
                }
            }

            return results;
        }

        // Try Employees, Supervisors, HODs collections for the user id
        public async Task<DocumentSnapshot?> GetUserDocumentAsync(string userId)
        {
            var collections = new[] { "Employees", "Supervisors", "HODs" };
            foreach (var collection in collections)
            {
                var doc = await _db.Collection(collection).Document(userId).GetSnapshotAsync();
                if (doc.Exists) return doc;
            }
            return null;
        }
    }
}
