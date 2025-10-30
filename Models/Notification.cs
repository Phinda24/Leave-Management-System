using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;

namespace Leave_Management_system.Models
{

     [FirestoreData]
    public class NotificationItem
    {
        [FirestoreProperty] public string Id { get; set; }
        [FirestoreProperty] public string UserId { get; set; }          // recipient user id
        [FirestoreProperty] public string Title { get; set; }
        [FirestoreProperty] public string Body { get; set; }
        [FirestoreProperty] public bool Read { get; set; } = false;
        [FirestoreProperty] public Timestamp CreatedAt { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
        [FirestoreProperty] public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

}

