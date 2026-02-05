using Google.Cloud.Firestore;

namespace FamilyBudgetApi.Models
{


    public class EditEvent
    {
        public string? UserId { get; set; }
        public string? UserEmail { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Action { get; set; }
    }
}
