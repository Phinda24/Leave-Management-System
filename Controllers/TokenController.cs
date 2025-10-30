//using Microsoft.AspNetCore.Mvc;
//using Google.Cloud.Firestore;
//using Microsoft.AspNetCore.Mvc;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Leave_Management_system.Controllers
//{

//    [Route("api/[controller]")]
//    public class TokenController : Controller
//    {
//        private readonly FirestoreDb _db;

//        public TokenController(FirestoreDb db)
//        {
//            _db = db;
//        }

//        // POST api/token/register
//        [HttpPost("register")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Register([FromForm] string token)
//        {
//            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token missing");

//            // Read session to determine who this is
//            var role = (HttpContext.Session.GetString("UserRole") ?? "").Trim();
//            var employeeId = HttpContext.Session.GetString("EmployeeId");
//            var supId = HttpContext.Session.GetString("SupID");
//            var hodId = HttpContext.Session.GetString("HodID");
//            var uid = HttpContext.Session.GetString("Uid"); // fallback

//            string userId = employeeId ?? supId ?? hodId ?? uid;
//            if (string.IsNullOrEmpty(userId)) return Unauthorized("no user session");

//            // Choose collection based on role (adjust these names to match your Firestore layout)
//            string collection;
//            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase)) collection = "Supervisors";
//            else if (role.Equals("HOD", StringComparison.OrdinalIgnoreCase) || role.Equals("HeadOfDepartment", StringComparison.OrdinalIgnoreCase)) collection = "Hods";
//            else collection = "Employees";

//            var docRef = _db.Collection(collection).Document(userId);

//            try
//            {
//                await docRef.UpdateAsync(new Dictionary<string, object>
//            {
//                { "FcmTokens", FieldValue.ArrayUnion(token) }
//            });
//                return Ok(new { success = true });
//            }
//            //catch (Google.Cloud.Firestore.FirestoreException fex) when (fex.Status?.StatusCode == Grpc.Core.StatusCode.NotFound)
//            //{
//            //    // Document missing — create it (merge) with token
//            //    var payload = new Dictionary<string, object>
//            //    {
//            //catch (Google.Cloud.Firestore.FirestoreException fex) when (fex.Status?.StatusCode == Grpc.Core.StatusCode.NotFound)
//            //{
//            //    // Document missing — create it (merge) with token
//            //    var payload = new Dictionary<string, object>
//            //{

//            //    { "FcmTokens", new[] { token } }
//            //};
//            catch (Grpc.Core.RpcException rex) when (rex.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
//            {
//                // Document missing — create it (merge) with token
//                    var payload = new Dictionary<string, object>
//                    {

//                    { "FcmTokens", new[] { token } }
//            };
//                await docRef.SetAsync(payload, SetOptions.MergeAll);
//                return Ok(new { success = true, created = true });
//            }
//            catch (Exception ex)
//            {
//                // In production log this
//                return StatusCode(500, ex.Message);
//            }
//        }

//        // POST api/token/unregister
//        [HttpPost("unregister")]
//        [ValidateAntiForgeryToken]
//        public async Task<IActionResult> Unregister([FromForm] string token)
//        {
//            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token missing");

//            var role = (HttpContext.Session.GetString("UserRole") ?? "").Trim();
//            var employeeId = HttpContext.Session.GetString("EmployeeId");
//            var supId = HttpContext.Session.GetString("SupID");
//            var hodId = HttpContext.Session.GetString("HodID");
//            var uid = HttpContext.Session.GetString("Uid");

//            string userId = employeeId ?? supId ?? hodId ?? uid;
//            if (string.IsNullOrEmpty(userId)) return Unauthorized("no user session");

//            string collection;
//            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase)) collection = "Supervisors";
//            else if (role.Equals("HOD", StringComparison.OrdinalIgnoreCase) || role.Equals("HeadOfDepartment", StringComparison.OrdinalIgnoreCase)) collection = "Hods";
//            else collection = "Employees";

//            var docRef = _db.Collection(collection).Document(userId);

//            try
//            {
//                await docRef.UpdateAsync(new Dictionary<string, object>
//            {
//                { "FcmTokens", FieldValue.ArrayRemove(token) }
//            });
//                return Ok(new { success = true });
//            }
//            //catch (Google.Cloud.Firestore.FirestoreException fex) when (fex.Status?.StatusCode == Grpc.Core.StatusCode.NotFound)
//            //{
//            //    // nothing to remove
//            //    return Ok(new { success = true, note = "doc_not_found" });
//            //}
//            catch (Grpc.Core.RpcException rex) when (rex.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
//            {
//                // nothing to remove
//                return Ok(new { success = true, note = "doc_not_found" });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, ex.Message);
//            }
//        }
//    }

//}
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Leave_Management_system.Controllers
{
    [Route("api/[controller]")]
    public class TokenController : Controller
    {
        private readonly FirestoreDb _db;

        public TokenController(FirestoreDb db)
        {
            _db = db;
        }

        // POST api/token/register
        [HttpPost("register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token missing");

            var role = (HttpContext.Session.GetString("UserRole") ?? "").Trim();
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var supId = HttpContext.Session.GetString("SupID");
            var hodId = HttpContext.Session.GetString("HodID");
            var uid = HttpContext.Session.GetString("Uid");

            string userId = employeeId ?? supId ?? hodId ?? uid;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("no user session");

            string collection = "Employees";
            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase)) collection = "Supervisors";
            else if (role.Equals("HODs", StringComparison.OrdinalIgnoreCase) || role.Equals("HeadOfDepartment", StringComparison.OrdinalIgnoreCase)) collection = "Hods";

            var docRef = _db.Collection(collection).Document(userId);

            try
            {
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "FcmTokens", FieldValue.ArrayUnion(token) }
                });
                return Ok(new { success = true });
            }
            catch (Grpc.Core.RpcException rex) when (rex.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                var payload = new Dictionary<string, object>
                {
                    { "FcmTokens", new[] { token } }
                };
                await docRef.SetAsync(payload, SetOptions.MergeAll);
                return Ok(new { success = true, created = true });
            }
            catch (Exception ex)
            {
                // log
                return StatusCode(500, ex.Message);
            }
        }

        // POST api/token/unregister
        [HttpPost("unregister")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unregister([FromForm] string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return BadRequest("token missing");

            var role = (HttpContext.Session.GetString("UserRole") ?? "").Trim();
            var employeeId = HttpContext.Session.GetString("EmployeeId");
            var supId = HttpContext.Session.GetString("SupID");
            var hodId = HttpContext.Session.GetString("HodID");
            var uid = HttpContext.Session.GetString("Uid");

            string userId = employeeId ?? supId ?? hodId ?? uid;
            if (string.IsNullOrEmpty(userId)) return Unauthorized("no user session");

            string collection = "Employees";
            if (role.Equals("Supervisor", StringComparison.OrdinalIgnoreCase)) collection = "Supervisors";
            else if (role.Equals("HODs", StringComparison.OrdinalIgnoreCase) || role.Equals("HeadOfDepartment", StringComparison.OrdinalIgnoreCase)) collection = "Hods";

            var docRef = _db.Collection(collection).Document(userId);

            try
            {
                await docRef.UpdateAsync(new Dictionary<string, object>
                {
                    { "FcmTokens", FieldValue.ArrayRemove(token) }
                });
                return Ok(new { success = true });
            }
            catch (Grpc.Core.RpcException rex) when (rex.Status.StatusCode == Grpc.Core.StatusCode.NotFound)
            {
                // nothing to remove
                return Ok(new { success = true, note = "doc_not_found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
